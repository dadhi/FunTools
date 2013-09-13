using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools.Events.Aggregation;
using PresentationTools.Events.Aggregation.Extensions;
using PresentationTools.Reactives;

namespace PresentationTools.UnitTests
{
	[TestClass]
	public class ReactiveWithEventHubTests
	{
		[TestMethod]
		public void I_should_be_able_to_subscribe_reactive_to_hub()
		{
			// Arrange		
			var eventHub = new EventHub();
			var counter = Reactive.Of(0).Use(eventHub, to => to.Handle<CounterEvent>(e => e.Count));

			// Act	
			eventHub.Publish(new CounterEvent { Count = 3 });

			// Assert
			counter.Value.Should().Be(3);
		}

		[TestMethod]
		public void When_reactive_is_garbage_collected_then_value_should_not_be_updated()
		{
			// Arrange
			var eventHub = new EventHub();
			var updated = 0;
			Reactive.Of(0).Use(eventHub, to => to.Handle<CounterEvent>(e => e.Count)).OnChange(i => updated = i);

			// Act			
			GC.Collect();
			eventHub.Publish(new CounterEvent { Count = 3 });

			// Assert
			updated.Should().Be(0);
		}

		[TestMethod]
		public void When_reactive_is_not_garbage_collected_then_value_should_be_updated()
		{
			// Arrange
			var eventHub = new EventHub();
			var updated = 0;
			var counter = Reactive.Of(0).Use(eventHub, to => to.Handle<CounterEvent>(e => e.Count)).OnChange(i => updated = i);

			// Act			
			GC.Collect();
			eventHub.Publish(new CounterEvent { Count = 3 });

			// Assert
			counter.Value.Should().Be(3);
			updated.Should().Be(3);
		}

		[TestMethod]
		public void I_should_be_able_to_handle_and_publich_multiple_event_from_one_event_aggregator_in_one_statement()
		{
			// Arrange
			var eventHub = new EventHub();

			const string deleted = "deleted";
			const string usersSays = "users says";
			const string hi = "hi";

			var message = Reactive.Of<string>().Use(eventHub, to => to
				.Handle<MessageChangedEvent>(e => e.Message, e => !string.IsNullOrEmpty(e.Message))
				.Handle<MessageDeletedEvent>(_ => deleted)
				.Publish(_ => new PingEvent())
				.Publish(x => new MessageUpdatedByUserEvent(x), x => x.Contains(usersSays)));

			var pingCount = 0;
			eventHub.Subscribe<PingEvent>(_ => ++pingCount);

			string updatedMessage = null;
			eventHub.Subscribe<MessageUpdatedByUserEvent>(e => updatedMessage = e.Message);

			// Act, Assert
			eventHub.Publish(new MessageChangedEvent { Message = hi });
			message.Value.Should().Be(hi);
			pingCount.Should().Be(1);
			updatedMessage.Should().BeNull();

			// Act, Assert
			eventHub.Publish(new MessageDeletedEvent());
			message.Value.Should().Be(deleted);
			pingCount.Should().Be(2);
			updatedMessage.Should().BeNull();

			// Act, Assert
			message.Value = usersSays + hi;
			pingCount.Should().Be(3);
			updatedMessage.Should().Be(usersSays + hi);
		}

		#region CUT

		private class CounterEvent
		{
			public int Count { get; set; }
		}

		public class PingEvent
		{
		}

		public class MessageUpdatedByUserEvent
		{
			public string Message { get; set; }

			public MessageUpdatedByUserEvent(string message)
			{
				Message = message;
			}
		}

		public class MessageDeletedEvent
		{
		}

		public class MessageChangedEvent
		{
			public string Message { get; set; }
		}

		#endregion
	}
}