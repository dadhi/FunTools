using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace PresentationTools.UnitTests.POC
{
	[TestClass]
	public class MoqApiTests
	{
		[TestMethod]
		public void Can_verify_raised_events_sequence()
		{
			// Arrange
			var server = new Server();

			var observer = new Mock<IServerEventsObserver>();

			int[] eventSequence = { 0 };

			observer.Setup(x => x.OnReportStarted(server, It.IsAny<EventArgs>()))
				.Callback(() => (++eventSequence[0]).Should().Be(1));

			observer.Setup(x => x.OnReportEnded(server, It.IsAny<EventArgs>()))
				.Callback(() => (++eventSequence[0]).Should().Be(2));

			server.ReportStarted += observer.Object.OnReportStarted;
			server.ReportEnded += observer.Object.OnReportEnded;

			// Act
			server.Start();

			// Assert
			observer.VerifyAll();
		}

		[TestMethod]
		public void Can_simulate_sequence_of_events_from_external_service()
		{
			// Arrange
			var consumer = new ProxyConsumer();

			var proxyMock = new Mock<IProxy>();
			proxyMock.Setup(x => x.Start())
				.Callback(
				() =>
				{
					consumer.OnReportReady("a");
					consumer.OnReportReady("b");
					consumer.OnReportReady("c");
					consumer.OnSessionCompleted();
				});

			// Act
			consumer.Execute(proxyMock.Object);

			// Assert
			consumer.Total.Should().Be("abc!");
		}

		#region SUT

		public class Server
		{
			public event EventHandler ReportStarted;
			public event EventHandler ReportEnded;

			public void Start()
			{
				ReportStarted(this, EventArgs.Empty);
				ReportEnded(this, EventArgs.Empty);
			}
		}

		public interface IServerEventsObserver
		{
			void OnReportStarted(object sender, EventArgs e);
			void OnReportEnded(object sender, EventArgs e);
		}

		public interface IProxy
		{
			event Action<string> ReportReady;

			event Action SessionCompleted;

			void Start();
		}

		public class ProxyConsumer
		{
			public string Total = string.Empty;

			public void OnReportReady(string report)
			{
				Total += report;
			}

			public void OnSessionCompleted()
			{
				Total += "!";
			}

			public void Execute(IProxy proxy)
			{
				proxy.ReportReady += OnReportReady;
				proxy.SessionCompleted += OnSessionCompleted;
				proxy.Start();
			}
		}

		#endregion
	}
}