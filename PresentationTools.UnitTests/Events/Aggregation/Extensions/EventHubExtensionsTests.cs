using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools.Events.Aggregation;
using FluentAssertions;
using PresentationTools.Events.Aggregation.Extensions;

namespace PresentationTools.UnitTests.Events.Aggregation.Extensions
{
	[TestClass]
	public class EventHubExtensionsTests
	{
		[TestMethod]
		public void I_should_be_able_subscribe_lambda()
		{
			// Arrange
			var eventHub = new EventHub();

			// Act
			AnEvent result = null;
			eventHub.Subscribe<AnEvent>(e => result = e);
			var actual = new AnEvent();
			eventHub.Publish(actual);

			// Assert
			result.Should().BeSameAs(actual);
		}

		[TestMethod]
		public void I_should_be_able_to_use_result_lambda_handler_to_unsubscribe()
		{
			// Arrange
			var eventHub = new EventHub();
			AnEvent result = null;
			var handle = eventHub.Subscribe<AnEvent>(e => result = e);

			// Act
			eventHub.Unsubscribe(handle);
			var actual = new AnEvent();
			eventHub.Publish(actual);

			// Assert
			result.Should().BeNull();
		}

		[TestMethod]
		public void Given_I_did_not_save_lambda_handler_when_GC_collected_and_event_published_then_event_should_not_be_handled()
		{
			// Arrange
			var eventHub = new EventHub();
			AnEvent result = null;
			eventHub.Subscribe<AnEvent>(e => result = e);

			// Act
			GC.Collect();
			var actual = new AnEvent();
			eventHub.Publish(actual);

			// Assert
			result.Should().BeNull();
		}

		[TestMethod]
		public void Given_I_did_save_lambda_handler_when_GC_collected_and_event_published_then_event_should_be_handled()
		{
			// Arrange
			var eventHub = new EventHub();
			AnEvent result = null;
			var handle = eventHub.Subscribe<AnEvent>(e => result = e);

			// Act
			GC.Collect();
			var actual = new AnEvent();
			eventHub.Publish(actual);

			// Assert
			result.Should().BeSameAs(actual);
			handle.Should().NotBeNull();
		}

		#region CUT

		internal class AnEvent
		{
		}

		#endregion
	}
}