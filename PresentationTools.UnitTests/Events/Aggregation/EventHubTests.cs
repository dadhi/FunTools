using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools.Events.Aggregation;

namespace PresentationTools.UnitTests.Events.Aggregation
{
	[TestClass]
	public class EventHubTests
	{
		[TestMethod]
		public void I_should_be_able_to_subscribe_client_and_then_notify_it_on_event()
		{
			// Arrange
			var eventHub = new EventHub();

			var observer = new Observer();
			var client = new SomeClient(observer);

			// Act
			eventHub.Subscribe(client);
			var someEvent = new SomeEvent();
			eventHub.Publish(someEvent);

			// Assert
			observer.HandledEvent.Should().BeSameAs(someEvent);
		}

		[TestMethod]
		public void I_should_be_able_to_subscribe_client_and_then_notify_it_on_subtype_event()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);

			// Act
			eventHub.Subscribe(client);
			var someEvent = new SomeEventSubtype();
			eventHub.Publish(someEvent);

			// Assert
			observer.HandledEvent.Should().BeSameAs(someEvent);
		}

		[TestMethod]
		public void Client_should_be_able_to_subscribe_to_all_type_of_events_by_implementing_IListener_of_object()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new AllClient(observer);

			// Act
			eventHub.Subscribe(client);

			eventHub.Publish(new SomeEventSubtype());
			eventHub.Publish(new SomeEvent());
			eventHub.Publish(new OtherEvent());
			eventHub.Publish(new object());

			// Assert
			observer.EventCount.Should().Be(4);
		}

		[TestMethod]
		public void When_subscribed_for_one_event_I_should_not_be_notified_on_other_event()
		{
			// Arrange
			var eventHub = new EventHub();

			var observer = new Observer();
			var client = new SomeClient(observer);

			// Act
			eventHub.Subscribe(client);
			var otherEvent = new OtherEvent();
			eventHub.Publish(otherEvent);

			// Assert
			observer.HandledEvent.Should().BeNull();
		}

		[TestMethod]
		public void I_should_be_able_to_unsubscribe()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);
			eventHub.Subscribe(client);

			// Act
			eventHub.Unsubscribe(client);
			eventHub.Publish(new SomeEvent());

			// Assert
			observer.HandledEvent.Should().BeNull();
		}

		[TestMethod]
		public void Second_subscription_of_the_same_listener_should_be_ignored()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);

			// Act
			eventHub.Subscribe(client);
			eventHub.Subscribe(client);
			eventHub.Publish(new SomeEvent());

			// Assert
			observer.EventCount.Should().Be(1);
		}

		[TestMethod]
		public void When_listener_is_garbage_collected_then_event_should_not_be_handled()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			// ReSharper disable NotAccessedVariable
			var client = new SomeClient(observer);
			// ReSharper restore NotAccessedVariable

			// Act
			// ReSharper disable RedundantAssignment
			client = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			eventHub.Publish(new SomeEvent());

			// Assert
			observer.HandledEvent.Should().BeNull();
		}

		// Removal take place on because it is the only place to for EventAggregator to grow in size and therefore to drop performance
		[TestMethod]
		public void When_listener_is_garbage_collected_then_it_should_be_removed_on_next_subscribe()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);
			eventHub.Subscribe(client);

			// Act
			// ReSharper disable RedundantAssignment
			client = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			eventHub.Subscribe(new OtherClient());

			// Assert
			eventHub.Size.Should().Be(1);
		}

		[TestMethod]
		public void When_listener_is_garbage_collected_then_it_should_be_removed_on_next_unsubscribe()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);
			eventHub.Subscribe(client);

			// Act
			// ReSharper disable RedundantAssignment
			client = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			eventHub.Unsubscribe(new OtherClient());

			// Assert
			eventHub.Size.Should().Be(0);
		}

		[TestMethod]
		public void When_listener_is_garbage_collected_then_publishing_event_on_it_will_not_raise_any_exception()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);
			eventHub.Subscribe(client);

			// Act
			// ReSharper disable RedundantAssignment
			client = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			eventHub.Publish(new SomeEvent());

			// Assert
			observer.HandledEvent.Should().BeNull();
		}

		[TestMethod]
		public void Long_Raise_execution_should_not_block_Subscribe_made_from_other_thread()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer { HandlerExecutionMilliseconds = 500 };
			var client = new SomeClient(observer);
			eventHub.Subscribe(client);

			// Act
			var stopwatch = Stopwatch.StartNew();
			new Thread(() => eventHub.Publish(new SomeEvent())) { IsBackground = true }.Start();

			eventHub.Subscribe(new OtherEvent());

			// Assert
			stopwatch.Stop();
			observer.HandlerExecutionMilliseconds.Should().BeGreaterThan((int)stopwatch.ElapsedMilliseconds);
		}

		[TestMethod]
		public void Listener_should_be_able_to_specify_its_handler_execution_strategy()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);

			// Act
			var listenerHandled = false;
			eventHub.Subscribe(client,
				a =>
				{
					a();
					listenerHandled = true;
				});
			var someEvent = new SomeEvent();
			eventHub.Publish(someEvent);

			// Assert
			observer.HandledEvent.Should().BeSameAs(someEvent);
			listenerHandled.Should().BeTrue();
		}

		[TestMethod]
		public void Event_hub_will_use_default_handler_startegy_if_specified()
		{
			// Arrange
			var eventHub = new EventHub();

			var defaultHandled = false;
			eventHub.DefaultStrategy =
				a =>
				{
					a();
					defaultHandled = true;
				};

			var observer = new Observer();
			var client = new SomeClient(observer);

			// Act
			eventHub.Subscribe(client);
			var someEvent = new SomeEvent();
			eventHub.Publish(someEvent);

			// Assert
			observer.HandledEvent.Should().BeSameAs(someEvent);
			defaultHandled.Should().BeTrue();
		}

		[TestMethod]
		public void If_both_default_and_listener_handler_startegy_are_specified_Then_should_be_used_listener_startegy()
		{
			// Arrange
			var eventHub = new EventHub();

			var defaultHandled = false;
			eventHub.DefaultStrategy =
				a =>
				{
					a();
					defaultHandled = true;
				};

			var observer = new Observer();
			var client = new SomeClient(observer);

			// Act
			var listenerHandled = false;
			eventHub.Subscribe(client,
				a =>
				{
					a();
					listenerHandled = true;
				});
			var someEvent = new SomeEvent();
			eventHub.Publish(someEvent);

			// Assert
			observer.HandledEvent.Should().BeSameAs(someEvent);
			listenerHandled.Should().BeTrue();
			defaultHandled.Should().BeFalse();
		}

		[TestMethod]
		public void I_should_be_able_to_specify_event_handling_startegy_when_raising_event()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);
			eventHub.Subscribe(client);

			// Act			
			var someEvent = new SomeEvent();
			var raiseHandled = false;
			eventHub.Publish(someEvent,
				a =>
				{
					a();
					raiseHandled = true;
				});

			// Assert
			observer.HandledEvent.Should().BeSameAs(someEvent);
			raiseHandled.Should().BeTrue();
		}

		[TestMethod]
		public void If_both_listener_and_event_handler_startegy_are_specified_Then_should_be_used_listener_startegy()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);

			// Act
			var listenerHandled = false;
			eventHub.Subscribe(client,
				a =>
				{
					a();
					listenerHandled = true;
				});

			var someEvent = new SomeEvent();
			var raiseHandled = false;
			eventHub.Publish(someEvent,
				a =>
				{
					a();
					raiseHandled = true;
				});

			// Assert
			observer.HandledEvent.Should().BeSameAs(someEvent);
			listenerHandled.Should().BeTrue();
			raiseHandled.Should().BeFalse();
		}

		[TestMethod]
		public void If_all_strategies_are_specified_then_listener_strategy_should_be_preferred()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);

			var defaultHandled = false;
			eventHub.DefaultStrategy =
				a =>
				{
					a();
					defaultHandled = true;
				};

			// Act
			var listenerHandled = false;
			eventHub.Subscribe(client,
				a =>
				{
					a();
					listenerHandled = true;
				});

			var someEvent = new SomeEvent();
			var raiseHandled = false;
			eventHub.Publish(someEvent,
				a =>
				{
					a();
					raiseHandled = true;
				});

			// Assert
			observer.HandledEvent.Should().BeSameAs(someEvent);
			listenerHandled.Should().BeTrue();
			raiseHandled.Should().BeFalse();
			defaultHandled.Should().BeFalse();
		}

		[TestMethod]
		public void When_I_set_strategy_formula_to_null_then_handler_will_be_invoked_sync_on_publishing_thread()
		{
			// Arrange
			var eventHub = new EventHub();
			var observer = new Observer();
			var client = new SomeClient(observer);
			eventHub.Subscribe(client);

			// Act
			eventHub.StrategyFormula = null;
			var e = new SomeEvent();
			eventHub.Publish(e);

			// Assert
			observer.HandledEvent.Should().BeSameAs(e);
		}

		#region CUT

		public class Observer
		{
			public object HandledEvent;

			public int EventCount;

			public int HandlerExecutionMilliseconds;
		}

		internal class SomeClient : IListen<SomeEvent>
		{
			private readonly Observer _observer;

			public SomeClient(Observer observer)
			{
				_observer = observer;
			}

			public void Listen(SomeEvent e)
			{
				if (_observer.HandlerExecutionMilliseconds != 0)
					Thread.Sleep(_observer.HandlerExecutionMilliseconds);

				_observer.HandledEvent = e;
				++_observer.EventCount;
			}
		}

		public class OtherClient : IListen<OtherEvent>
		{
			public void Listen(OtherEvent e)
			{
			}
		}

		public class AllClient : IListen<object>
		{
			private readonly Observer _observer;

			public AllClient(Observer observer)
			{
				_observer = observer;
			}

			public void Listen(object e)
			{
				++_observer.EventCount;
			}
		}

		internal class SomeEvent
		{
		}

		internal class SomeEventSubtype : SomeEvent
		{
		}

		public class OtherEvent
		{
		}

		#endregion
	}
}
