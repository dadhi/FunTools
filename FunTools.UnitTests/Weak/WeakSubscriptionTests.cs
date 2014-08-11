using System;
using System.ComponentModel;
using DryTools.UnitTests;
using FunTools.Weak;
using NUnit.Framework;

namespace FunTools.UnitTests.Weak
{
	[TestFixture]
	public class WeakSubscriptionTests
	{
		[Test]
		public void Delegate_event_handler_should_be_called_when_subscriber_not_GC_collected()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeViewModel();

			SubscribeWeakly.OnCustomDelegate<PropertyChangedEventHandler, PropertyChangedEventArgs, SomeView>(
				a => a.Invoke,
				h => model.PropertyChanged += h,
				h => model.PropertyChanged -= h,
				view, (v, sender, args) => v.OnPropertyChanged());

			// Act
			model.NotifyPropertyChanged();

			// Assert
			view.Observer.IsPropertyChangedHandled.Should().BeTrue();
		}

		[Test]
		public void Delegate_event_handler_should_be_called_when_GC_collected_and_subscriber_is_used_aftrewards()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeViewModel();

			SubscribeWeakly.OnCustomDelegate<PropertyChangedEventHandler, PropertyChangedEventArgs, SomeView>(
				a => a.Invoke,
				h => model.PropertyChanged += h,
				h => model.PropertyChanged -= h,
				view, (v, sender, args) => v.OnPropertyChanged());

			// Act
			GC.Collect();
			model.NotifyPropertyChanged();

			// Assert
			view.Observer.IsPropertyChangedHandled.Should().BeTrue();
		}

		[Test]
		public void Delegate_event_handler_should_not_be_called_when_GC_collected_and_subscriber_is_not_used_aftrewards()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeViewModel();

			SubscribeWeakly.OnCustomDelegate<PropertyChangedEventHandler, PropertyChangedEventArgs, SomeView>(
				h => h.Invoke,
				h => model.PropertyChanged += h,
				h => model.PropertyChanged -= h,
				view, (v, sender, args) => v.OnPropertyChanged());

			// Act
			var observer = view.Observer;
			// ReSharper disable RedundantAssignment
			view = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			model.NotifyPropertyChanged();

			// Assert
			observer.IsPropertyChangedHandled.Should().BeFalse();
		}

		[Test]
		public void Delegate_event_method_handler_should_be_called_when_GC_collected_and_subscriber_is_used_aftrewards()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeViewModel();

			SubscribeWeakly.OnCustomDelegate<PropertyChangedEventHandler, PropertyChangedEventArgs>(
				a => a.Invoke,
				h => model.PropertyChanged += h,
				h => model.PropertyChanged -= h,
				view.OnPropertyChanged);

			// Act
			GC.Collect();
			model.NotifyPropertyChanged();

			// Assert
			view.Observer.IsPropertyChangedHandled.Should().BeTrue();
		}

		[Test]
		public void Delegate_event_method_handler_should_not_be_called_when_GC_collected_and_subscriber_is_not_used_aftrewards()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeViewModel();

			SubscribeWeakly.OnCustomDelegate<PropertyChangedEventHandler, PropertyChangedEventArgs>(
				a => a.Invoke,
				h => model.PropertyChanged += h,
				h => model.PropertyChanged -= h,
				view.OnPropertyChanged);

			// Act
			var observer = view.Observer;
			// ReSharper disable RedundantAssignment
			view = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			model.NotifyPropertyChanged();

			// Assert
			observer.IsPropertyChangedHandled.Should().BeFalse();
		}

		[Test]
		public void Delegate_event_method_handler_should_not_be_called_when_subscription_disposed()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeViewModel();

			var subscription = SubscribeWeakly.OnCustomDelegate<PropertyChangedEventHandler, PropertyChangedEventArgs>(
				a => a.Invoke,
				h => model.PropertyChanged += h,
				h => model.PropertyChanged -= h,
				view.OnPropertyChanged);

			// Act
			subscription.Dispose();
			model.NotifyPropertyChanged();

			// Assert
			view.Observer.IsPropertyChangedHandled.Should().BeFalse();
		}

		[Test]
		public void Generic_EventHandler_event_handler_should_be_called_on_event()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeModel();

			SubscribeWeakly.OnGenericEventHandler<OtherDataChangedEvent, SomeView>(
				h => model.OtherDataChanged += h,
				h => model.OtherDataChanged -= h,
				view, (v, sender, e) => v.OnOtherDataChanged());

			// Act
			model.NotifyOtherDataChanged(new OtherDataChangedEvent());

			// Assert
			view.Observer.IsOtherDataChangedHandled.Should().BeTrue();
		}

		[Test]
		public void EventHandler_event_handler_should_be_called_on_event()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeModel();

			SubscribeWeakly.OnEventHandler(
				h => model.Updated += h,
				h => model.Updated -= h,
				view, (v, sender, e) => v.OnOtherDataChanged());

			// Act
			model.Update();

			// Assert
			view.Observer.IsOtherDataChangedHandled.Should().BeTrue();
		}

		[Test]
		public void EventHandler_event_handler_should_not_be_called_when_subscriber_is_garbage_collected()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeModel();

			SubscribeWeakly.OnEventHandler(
				h => model.Updated += h,
				h => model.Updated -= h,
				view, (v, sender, e) => v.OnOtherDataChanged());

			// Act
			var observer = view.Observer;
			// ReSharper disable RedundantAssignment
			view = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();

			model.Update();

			// Assert
			observer.IsOtherDataChangedHandled.Should().BeFalse();
		}

		[Test]
		public void Action_event_handler_should_be_called_on_event()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeModel();

			SubscribeWeakly.OnGenericAction<SomeDataChangedEvent, SomeView>(
				h => model.SomeDataChanged += h,
				h => model.SomeDataChanged -= h,
				view, (v, e) => v.OnSomeDataChanged());

			// Act
			model.NotifySomeDataChanged(new SomeDataChangedEvent());

			// Assert
			view.Observer.IsSomeDataChangedHandled.Should().BeTrue();
		}

		[Test]
		public void Action_event_handler_should_be_called_when_GC_collected_and_subscriber_is_used_aftrewards()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeModel();

			SubscribeWeakly.OnGenericAction<SomeDataChangedEvent, SomeView>(
				h => model.SomeDataChanged += h,
				h => model.SomeDataChanged -= h,
				view, (v, e) => v.OnSomeDataChanged());

			// Act
			GC.Collect();
			model.NotifySomeDataChanged(new SomeDataChangedEvent());

			// Assert
			view.Observer.IsSomeDataChangedHandled.Should().BeTrue();
		}

		[Test]
		public void Action_event_handler_should_not_be_called_when_GC_collected_and_subscriber_is_not_used_aftrewards()
		{
			// Arrange			
			var view = new SomeView();
			var model = new SomeModel();

			SubscribeWeakly.OnGenericAction<SomeDataChangedEvent, SomeView>(
				h => model.SomeDataChanged += h,
				h => model.SomeDataChanged -= h,
				view, (v, e) => v.OnSomeDataChanged());

			// Act
			var observer = view.Observer;
			// ReSharper disable RedundantAssignment
			view = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			model.NotifySomeDataChanged(new SomeDataChangedEvent());

			// Assert
			observer.IsSomeDataChangedHandled.Should().BeFalse();
		}

		[Test]
		public void PropertyChanged_event_handler_should_be_called_when_GC_collected_and_subscriber_is_used_aftrewards()
		{
			// Arrange
			var view = new SomeView();
			var viewModel = new SomeViewModel();

			viewModel.SubscribeWeakly(view, (x, _, e) => x.OnPropertyChanged());

			// Act
			GC.Collect();
			viewModel.NotifyPropertyChanged();

			// Assert
			view.Observer.IsPropertyChangedHandled.Should().BeTrue();
		}

		[Test]
		public void PropertyChanged_event_handler_should_not_be_called_when_GC_collected_and_subscriber_is_not_used_aftrewards()
		{
			// Arrange
			var view = new SomeView();
			var viewModel = new SomeViewModel();

			viewModel.SubscribeWeakly(view, (x, _, e) => x.OnPropertyChanged());

			// Act
			var observer = view.Observer;
			// ReSharper disable RedundantAssignment
			view = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			viewModel.NotifyPropertyChanged();

			// Assert
			observer.IsPropertyChangedHandled.Should().BeFalse();
		}

		[Test]
		public void Action_event_subscription_can_be_safely_disposed_multiple_times()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeModel();

			var subscription = SubscribeWeakly.OnGenericAction<SomeDataChangedEvent, SomeView>(
				h => model.SomeDataChanged += h,
				h => model.SomeDataChanged -= h,
				view, (x, e) => { });

			// Act
			subscription.Dispose();
			subscription.Dispose();

			// Assert
			// No exception should be raised
		}

		[Test]
		public void Action_event_handler_should_not_be_called_when_subscription_is_disposed()
		{
			// Arrange
			var view = new SomeView();
			var model = new SomeModel();

			var subscription = SubscribeWeakly.OnGenericAction<SomeDataChangedEvent, SomeView>(
				h => model.SomeDataChanged += h,
				h => model.SomeDataChanged -= h,
				view,
				(x, e) => x.OnSomeDataChanged());

			// Act
			subscription.Dispose();
			model.NotifySomeDataChanged(new SomeDataChangedEvent());

			// Assert
			view.Observer.IsSomeDataChangedHandled.Should().BeFalse();
		}

		#region CUT

		private class EventObserver
		{
			public bool IsPropertyChangedHandled { get; set; }

			public bool IsSomeDataChangedHandled { get; set; }

			public bool IsOtherDataChangedHandled { get; set; }
		}

		private class SomeViewModel : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			public void NotifyPropertyChanged()
			{
				var handler = PropertyChanged;
				if (handler != null) handler(this, null);
			}
		}

		private class SomeView
		{
			public readonly EventObserver Observer = new EventObserver();

			public void OnPropertyChanged()
			{
				Observer.IsPropertyChangedHandled = true;
			}

			public void OnPropertyChanged(object s, PropertyChangedEventArgs e)
			{
				Observer.IsPropertyChangedHandled = true;
			}

			public void OnSomeDataChanged()
			{
				Observer.IsSomeDataChangedHandled = true;
			}

			public void OnOtherDataChanged()
			{
				Observer.IsOtherDataChangedHandled = true;
			}
		}

		private class SomeDataChangedEvent
		{
		}

		private class OtherDataChangedEvent : EventArgs
		{
		}

		private class SomeModel
		{
			public event Action<SomeDataChangedEvent> SomeDataChanged;

			public event EventHandler<OtherDataChangedEvent> OtherDataChanged;

			public event EventHandler Updated;

			public void NotifySomeDataChanged(SomeDataChangedEvent someDataChangedEvent)
			{
				var handler = SomeDataChanged;
				if (handler != null)
					handler(someDataChangedEvent);
			}

			public void NotifyOtherDataChanged(OtherDataChangedEvent e)
			{
				var handler = OtherDataChanged;
				if (handler != null)
					handler(this, e);
			}

			public void Update()
			{
				var handler = Updated;
				if (handler != null)
					handler(this, EventArgs.Empty);
			}
		}

		#endregion
	}
}