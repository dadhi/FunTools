using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools.Events.Aggregation;
using PresentationTools.Events.Aggregation.Selection;
using FluentAssertions;
using I = FluentAssertions.AssertionExtensions;

namespace PresentationTools.UnitTests.Events.Aggregation.Selection
{
	[TestClass]
	public class SelectionTests
	{
		[TestMethod]
		public void I_should_be_able_to_subscribe_to_selected_event()
		{
			// Arrange
			var aggregator = new EventHub();
			var observer = new Observer();
			var client = new EveryEventClient(observer);

			// Act
			client.Select().From<SomeEvent>().Of(aggregator);

			aggregator.Publish(new SomeEvent());
			aggregator.Publish(new object());

			// Assert
			observer.HandledEvent.Should().BeOfType<SomeEvent>();
		}

		[TestMethod]
		public void I_should_be_able_to_subscribe_to_selected_and_filtered_event()
		{
			// Arrange
			var aggregator = new EventHub();
			var observer = new Observer();
			var client = new DataClient(observer);

			// Act
			client.Select<string>().From<SomeEvent>(e => e.Data).Of(aggregator);

			aggregator.Publish(new SomeEvent { Data = "hi" });

			// Assert
			observer.HandledEvent.Should().BeOfType<string>();
		}

		[TestMethod]
		public void I_should_be_able_to_subscribe_to_selected_and_filtered_event_and_handle_it_only_if_condition_is_true()
		{
			// Arrange
			var aggregator = new EventHub();
			var observer = new Observer();
			var client = new DataClient(observer);

			// Act
			client.Select<string>().From<SomeEvent>(e => e.Data, e => !string.IsNullOrEmpty(e.Data)).Of(aggregator);

			// Assert
			aggregator.Publish(new SomeEvent { Data = "" });
			observer.HandledEvent.Should().BeNull();

			aggregator.Publish(new SomeEvent { Data = "hi" });
			string.Equals(observer.HandledEvent, "hi").Should().BeTrue();
		}

		[TestMethod]
		public void I_should_be_able_to_unsubscribe_client_subscribed_to_selected_and_filtered_event()
		{
			// Arrange
			var aggregator = new EventHub();
			var observer = new Observer();
			var client = new DataClient(observer);
			var handler = client.Select<string>().From<SomeEvent>(e => e.Data).Of(aggregator);

			// Act
			aggregator.Unsubscribe(handler);
			aggregator.Publish(new SomeEvent { Data = "hi" });

			// Assert
			observer.HandledEvent.Should().BeNull();
		}

		[TestMethod]
		public void When_handler_is_garbage_collected_then_event_should_not_be_handled()
		{
			// Arrange
			var aggregator = new EventHub();
			var observer = new Observer();
			var client = new DataClient(observer);
			client.Select<string>().From<SomeEvent>(e => e.Data).Of(aggregator);

			// Act
			var weakClient = new WeakReference(client);
			// ReSharper disable RedundantAssignment
			client = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			aggregator.Publish(new SomeEvent { Data = "hey" });

			// Assert
			observer.HandledEvent.Should().BeNull();
			weakClient.IsAlive.Should().BeFalse();
		}

		[TestMethod]
		public void When_handler_is_garbage_collected_then_any_subsequent_Publish_calls_should_not_throw()
		{
			// Arrange
			var aggregator = new EventHub();
			var observer = new Observer();
			var client = new DataClient(observer);
			client.Select<string>().From<SomeEvent>(e => e.Data).Of(aggregator);

			// Act
			// ReSharper disable RedundantAssignment
			client = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			aggregator.Publish(new SomeEvent { Data = "hey" });

			// Assert
			I.ShouldNotThrow(
				() => aggregator.Publish(new SomeEvent { Data = "there" }));
		}

		[TestMethod]
		public void When_handler_is_NOT_garbage_collected_then_event_should_be_handled()
		{
			// Arrange
			var aggregator = new EventHub();
			var observer = new Observer();
			var client = new DataClient(observer);

			client.Select<string>().From<SomeEvent>(e => e.Data).Of(aggregator);

			// Act
			GC.Collect();
			aggregator.Publish(new SomeEvent { Data = "hey" });

			// Assert
			client.ClientObserver.HandledEvent.Should().NotBeNull();
		}

		#region CUT

		public class SomeEvent
		{
			public string Data { get; set; }
		}

		public class EveryEventClient : IListen<object>
		{
			private readonly Observer _observer;

			public EveryEventClient(Observer observer)
			{
				_observer = observer;
			}

			public void Listen(object e)
			{
				_observer.HandledEvent = e;
			}
		}

		public class DataClient : IListen<string>, IListen<OtherEvent>
		{
			public Observer ClientObserver { get; private set; }

			public DataClient(Observer observer)
			{
				ClientObserver = observer;
			}

			public void Listen(string e)
			{
				ClientObserver.HandledEvent = e;
			}

			public void Listen(OtherEvent e)
			{
				ClientObserver.HandledEvent = e;
			}
		}

		public class Observer
		{
			public object HandledEvent;
		}

		public class OtherEvent
		{
		}


		#endregion
	}
}