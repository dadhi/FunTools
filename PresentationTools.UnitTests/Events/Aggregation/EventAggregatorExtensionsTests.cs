using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools;
using PresentationTools.Events.Aggregation;
using TestTools.MsUnit;
using TestTools;

namespace PresentationTools.UnitTests.Events.Aggregation
{
	[TestClass]
	public class EventAggregatorExtensionsTests
	{
		[TestMethod]
		public void Can_raise_event_of_specified_type_to_notify_event_type_subscribers_only()
		{
			// Arrange
			var eventAggregator = new EventAggregator();
			var viewModel = new SomeViewModel();
			eventAggregator.Subscribe(viewModel);

			// Act
			eventAggregator.Raise(new SomeEvent());

			// Assert
			viewModel.Observer.IsEventHandled.ShouldBeTrue();
		}

		[TestMethod]
		public void When_subscribing_for_specific_event_the_subscriber_will_also_be_notified_about_other_supported_events()
		{
			// Arrange
			var eventAggregator = new EventAggregator();
			ISubscriber<SomeEvent> viewModel = new SomeViewModel();
			eventAggregator.Subscribe(viewModel);

			// Act
			eventAggregator.RaiseOn<SomeViewModel>(x => x.DataChanged());

			// Assert
			((SomeViewModel)viewModel).Observer.IsDataChanged.ShouldBeTrue();
		}

		[TestMethod]
		public void Can_subscribe_action_handler_on_specific_event_type()
		{
			// Arrange
			var eventAggregator = new EventAggregator();
			SomeEvent actual = null;
			eventAggregator.SubscribeTo<SomeEvent>(x => actual = x);

			// Act
			var expected = new SomeEvent();
			eventAggregator.Raise(expected);

			// Assert
			actual.ShouldBeSameAs(expected);
		}

		[TestMethod]
		public void Can_unsubscribe_action_handler_on_specific_event_type()
		{
			// Arrange
			var eventAggregator = new EventAggregator();
			var raised = false;
			Action<SomeEvent> handler = e => raised = true;
			eventAggregator.SubscribeTo(handler);

			// Act
			eventAggregator.Unsubscribe(handler);
			eventAggregator.Raise(new SomeEvent());

			// Assert
			raised.ShouldBeFalse();
		}

		[TestMethod]
		public void Should_be_able_to_handle_event_by_calling_handler_on_specified_executor()
		{
			// Arrange
			var eventAggregator = new EventAggregator(ActionExecutor.Directly());
			var handled = false;

			// Act
			var onUI = ActionExecutor.OnUI();
			eventAggregator.SubscribeTo<SomeEvent>(e => onUI(() => handled = true));
			eventAggregator.Raise(new SomeEvent());

			// Assert
			DispatcherTools.GetCurrentDispatcher().RunPendingOperations();
			handled.ShouldBeTrue();
		}

		[TestMethod]
		public void Should_be_able_to_handle_event_providing_executor()
		{
			// Arrange
			var eventAggregator = new EventAggregator(ActionExecutor.Directly());
			var handled = false;

			// Act
			eventAggregator.SubscribeTo<SomeEvent>(ActionExecutor.OnUI(), e => handled = true);
			eventAggregator.Raise(new SomeEvent());

			// Assert
			DispatcherTools.GetCurrentDispatcher().RunPendingOperations();
			handled.ShouldBeTrue();
		}

		#region CUT

		private class SomeViewModel : ISubscriber<SomeEvent>
		{
			public readonly EventObserver Observer = new EventObserver();

			public void HandleEvent(SomeEvent e)
			{
				Observer.IsEventHandled = true;
			}

			public void DataChanged()
			{
				Observer.IsDataChanged = true;
			}
		}

		private class EventObserver
		{
			public bool IsEventHandled;

			public bool IsDataChanged;
		}

		private class SomeEvent
		{
		}

		#endregion
	}
}