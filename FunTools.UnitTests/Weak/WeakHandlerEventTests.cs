using System;
using System.ComponentModel;
using FunTools.Weak;
using NUnit.Framework;

namespace FunTools.UnitTests.Weak
{
	[TestFixture]
	public class WeakHandlerEventTests
	{
		[Test]
		public void When_event_raised_the_Method_handler_should_be_called()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			var view = new SomeView();
			viewModel.PropertyChanged += view.OnPropertyChanged;

			// Act
			viewModel.NotifyPropertyChanged(new PropertyChangedEventArgs("some"));

			// Assert
			view.Observer.IsPropertyChangedHandled.Should().BeTrue();
		}

		[Test]
		public void When_event_raised_the_Lambda_handler_should_be_called()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			var handled = false;
			viewModel.PropertyChanged += (sender, args) => handled = true;

			// Act
			viewModel.NotifyPropertyChanged(new PropertyChangedEventArgs("some"));

			// Assert
			handled.Should().BeTrue();
		}

		[Test]
		public void When_event_raised_and_Lambda_handler_is_collected_then_the_handler_should_not_be_called()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			var handled = false;
			viewModel.PropertyChanged += (sender, args) => handled = true;

			// Act
			GC.Collect();
			viewModel.NotifyPropertyChanged(new PropertyChangedEventArgs("some"));

			// Assert
			handled.Should().BeFalse();
		}

		[Test]
		public void When_event_raised_and_GC_collected_and_despite_that_subscriber_is_still_used_then_the_Method_handler_should_not_be_called()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			var view = new SomeView();
			viewModel.PropertyChanged += view.OnPropertyChanged;

			// Act
			GC.Collect();
			viewModel.NotifyPropertyChanged(new PropertyChangedEventArgs("some"));

			// Assert
			view.Observer.IsPropertyChangedHandled.Should().BeFalse();
		}

		[Test]
		public void When_Lambda_handler_is_unsubscribed_and_event_raised_then_the_handler_should_not_be_called()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			var handled = false;
			PropertyChangedEventHandler onPropertyChanged = (sender, args) => handled = true;
			viewModel.PropertyChanged += onPropertyChanged;

			// Act
			viewModel.PropertyChanged -= onPropertyChanged;
			viewModel.NotifyPropertyChanged(new PropertyChangedEventArgs("some"));

			// Assert
			handled.Should().BeFalse();
		}

		[Test]
		public void With_no_subscriptions_should_unsibscribe_without_exception()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			PropertyChangedEventHandler propertyChanged = (sender, args) => { };

			// Act
			viewModel.PropertyChanged -= propertyChanged;
			viewModel.NotifyPropertyChanged(new PropertyChangedEventArgs("some"));

			// Assert
			// No exceptions expected
		}

		[Test]
		public void When_GC_collected_and_then_I_subscribe_then_already_dead_subscribers_should_be_removed()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			var x = 0;
			viewModel.PropertyChanged += (sender, args) => ++x;
			var y = "1";
			viewModel.PropertyChanged += (sender, args) => y += 1;
			viewModel.InternalPropertyChanged.HandlerCount.Should().Be(2);

			// Act
			GC.Collect();
			viewModel.PropertyChanged += (sender, args) => { };

			// Assert
			viewModel.InternalPropertyChanged.HandlerCount.Should().Be(1);
		}

		[Test]
		public void Can_safely_raise_event_even_when_no_subsriptions_were_made()
		{
			// Arrange
			var viewModel = new SomeViewModel();

			// Act
		   Assert.DoesNotThrow(() => 
               viewModel.NotifyPropertyChanged(new PropertyChangedEventArgs("some")));
		}

		#region CUT

		private class SomeViewModel : INotifyPropertyChanged
		{
			private readonly WeakHandlerEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>
				_propertyChanged = new WeakHandlerEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => h.Invoke);

			public WeakHandlerEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>
				InternalPropertyChanged { get { return _propertyChanged; } }

			public event PropertyChangedEventHandler PropertyChanged
			{
				add { _propertyChanged.Subscribe(value); }
				remove { _propertyChanged.Unsubscribe(value); }
			}

			public void NotifyPropertyChanged(PropertyChangedEventArgs e)
			{
				_propertyChanged.Raise(this, e);
			}
		}

		private class SomeView
		{
			public EventObserver Observer { get; private set; }

			public SomeView()
			{
				Observer = new EventObserver();
			}

			public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				Observer.IsPropertyChangedHandled = true;
			}
		}

		private class EventObserver
		{
			public bool IsPropertyChangedHandled { get; set; }
		}

		#endregion
	}
}