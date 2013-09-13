using System;
using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools.Reactives;
using FluentAssertions.EventMonitoring;
using I = FluentAssertions.AssertionExtensions;

namespace PresentationTools.UnitTests.Reactives
{
	[TestClass]
	public class ReactiveTests
	{
		[TestMethod]
		public void When_reactive_value_changed_then_PropertyChanged_event_should_be_raised()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var eventRaised = false;
			counter.PropertyChanged += (s, e) => eventRaised = true;

			// Act
			++counter.Value;

			// Assert
			eventRaised.Should().BeTrue();
		}

		[TestMethod]
		public void When_assigned_to_its_value_type_variable_the_reactive_should_be_cast_to_its_value_type()
		{
			// Arrange
			var message = Reactive.Of("Hey");

			// Act
			string text = message;

			// Assert
			text.Should().Be(message.Value);
		}

		[TestMethod]
		public void The_results_of_implicit_cast_and_ToString_on_reactives_should_be_same_as_for_their_values()
		{
			// Arrange
			var counter = Reactive.Of<int>();
			var message = Reactive.Of<string>();
			var someObject = Reactive.Of<object>();

			// Assert
			("x" + counter).Should().Be("x" + counter.Value);
			(counter + "x").Should().Be(counter.Value + "x");

			("x" + message).Should().Be("x" + message.Value);
			(message + "x").Should().Be(message.Value + "x");

			("x" + someObject).Should().Be("x" + someObject.Value);
			(someObject + "x").Should().Be(someObject.Value + "x");

			(counter + message).Should().Be(counter.Value + message.Value);
			(message + counter).Should().Be(message.Value + counter.Value);

			(someObject + message).Should().Be(someObject.Value + message.Value);
			(message + someObject).Should().Be(message.Value + someObject.Value);
		}

		[TestMethod]
		public void Can_create_reactive_with_no_initial_value_provided()
		{
			// Arrange
			var counter = Reactive.Of<int>();

			// Assert
			counter.Value.Should().Be(default(int));
		}

		[TestMethod]
		public void Can_create_reactive_proxy_from_multiple_reactives()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var message = Reactive.Of("hey");

			var messageWithCounter = Reactive.Of(() => message.Value + ":" + counter.Value, message, counter);

			// Assert
			messageWithCounter.Value.Should().Be(message.Value + ":" + counter.Value);
		}

		[TestMethod]
		public void Can_create_reactive_proxy_from_multiple_reactives_without_change_sources()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var message = Reactive.Of("hey");

			// Act
			var counterMessage = Reactive.Of(() => message.Value + ":" + counter.Value);

			// Assert
			counterMessage.Value.Should().Be(message.Value + ":" + counter.Value);
		}

		[TestMethod]
		public void For_reactive_proxy_PropertyChanged_should_be_raised_on_each_change_of_source()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var message = Reactive.Of("Hey");

			var messageWithCounter = Reactive.Of(() => message.Value + ":" + counter.Value, message, counter);

			var eventCount = 0;
			messageWithCounter.PropertyChanged += (s, e) => eventCount++;

			// Act
			counter.Value = 1;
			message.Value = "Wow";

			// Assert
			eventCount.Should().Be(2);
		}

		[TestMethod]
		public void Can_produce_reactive_of_one_type_from_another()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var message = counter.To(
				x => "" + x.Value,
				(x, value) => x.Value = Int32.Parse(value));

			// Act
			message.Value = "10";

			// Assert
			counter.Value.Should().Be(10);
		}

		[TestMethod]
		public void Can_produce_reactive_of_one_type_from_another_based_on_another_reactive_value()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var message = counter.To(
				value => "" + value,
				int.Parse);

			// Act
			message.Value = "10";

			// Assert
			counter.Value.Should().Be(10);
		}

		[TestMethod]
		public void Can_produce_reactive_of_one_type_from_another_based_on_another_reactive_value_and_using_additional_change_sources()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var flag = Reactive.Of(false);

			var message = counter.To(
				value => "" + value,
				int.Parse,
				flag);

			var changed = false;
			message.PropertyChanged += (sender, args) => changed = true;

			// Act
			flag.Value = true;

			// Assert
			changed.Should().Be(true);
		}

		[TestMethod]
		public void Can_add_change_sources_to_reactive()
		{
			// Arrange
			var flag = Reactive.Of(false);
			const int initial = 0;
			var counter = Reactive.Of(initial).NotifiedBy(flag);
			counter.MonitorEvents();

			// Act
			flag.Value = true;

			// Assert			
			counter.ShouldRaise("PropertyChanged");
			(++counter.Value).Should().Be(initial + 1);
		}

		[TestMethod]
		public void Can_produce_readonly_reactive_of_one_type_from_another()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var greaterThanZero = counter.To(i => i > 0);

			// Act
			counter.Value = 1;

			// Assert
			greaterThanZero.Value.Should().BeTrue();
		}

		[TestMethod]
		public void When_set_readonly_reactive_then_NotSupportedException_should_be_thrown()
		{
			// Arrange
			var counter = Reactive.Of(() => 0);

			// Act, Assert
			I.ShouldThrow<NotSupportedException>(() => counter.Value = 10);
		}

		[TestMethod]
		public void When_set_readonly_reactive_value_then_NotSupportedException_should_be_thrown()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var message = counter.To(value => "" + value);

			// Act, Assert
			I.ShouldThrow<NotSupportedException>(() => message.Value = "10");
		}

		[TestMethod]
		public void Can_decorate_reactive_value_getter()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var twiceCounter = counter.Get(i => i * 2);

			// Act
			twiceCounter.Value++;

			// Assert
			twiceCounter.Value.Should().Be(counter.Value * 2);
		}

		[TestMethod]
		public void Can_decorate_reactive_value_setter()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var twiceCounter = counter.Set(i => i * 2);

			// Act
			twiceCounter.Value = 1;

			// Assert
			counter.Value.Should().Be(2);
		}

		[TestMethod]
		public void Should_be_able_to_execute_provided_action_after_value_is_set()
		{
			// Arrange
			var counterValue = 0;
			var counter = Reactive.Of(0).OnChange(x => counterValue = x);

			// Act
			counter.Value = 1;

			// Assert
			counterValue.Should().Be(1);
		}

		[TestMethod]
		public void Can_set_reactive_value_while_getting_it()
		{
			// Arrange
			var source = Reactive.Of<object>();

			// Act
			var ensureNotNull = source.Get(x => x.Value ?? (x.Value = new object()));

			// Assert
			ensureNotNull.Should().NotBeNull();
		}

		[TestMethod]
		public void Getters_can_be_chained()
		{
			// Arrange
			var counter = Reactive.Of(1);

			// Act
			var adjustedCounter = counter.Get(i => i * 2).Get(x => x.Value + 1);

			// Assert
			adjustedCounter.Value.Should().Be(3);
		}

		[TestMethod]
		public void Getters_and_setters_can_be_chained()
		{
			// Arrange
			var counter = Reactive.Of(1);

			// Act
			var adjustedCounter = counter
				.Set((x, value) => x.Value = value < 1 ? 1 : value)
				.Get(x => x.Value == 1 ? 10 : x);

			adjustedCounter.Value = 0;

			// Assert
			adjustedCounter.Value.Should().Be(10);
		}

		[TestMethod]
		public void Setters_can_be_chained()
		{
			// Arrange
			var counter = Reactive.Of(0);

			// Act
			var adjustedCounter = counter
				.Set((x, value) => x.Value = value < 0 ? 0 : value)
				.Set(i => -i);

			adjustedCounter.Value = 1;

			// Assert
			adjustedCounter.Value.Should().Be(0);
		}

		[TestMethod]
		public void For_Value_reactive_when_value_is_set_silently_then_PropertyChanged_should_not_be_raised()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var eventRaised = false;
			counter.PropertyChanged += (sender, args) => eventRaised = true;

			// Act
			counter.SetValueSilently(1);

			// Assert
			eventRaised.Should().BeFalse();
			counter.Value.Should().Be(1);
		}

		[TestMethod]
		public void For_Proxy_reactive_when_value_is_set_silently_then_PropertyChanged_should_not_be_raised()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var twiceCounter = counter.To(
				x => x.Value * 2,
				(x, value) => x.Value = value == 0 ? 0 : value / 2);

			var eventRaised = false;
			twiceCounter.PropertyChanged += delegate { eventRaised = true; };

			// Act
			twiceCounter.SetValueSilently(2);

			// Assert
			eventRaised.Should().BeFalse();
			counter.Value.Should().Be(1);
		}

		[TestMethod]
		public void Reactive_HashCode_should_equal_to_underlying_Value_HashCode()
		{
			// Arrange
			const string value = "wow";
			var reactive = Reactive.Of(value);

			// Assert
			reactive.GetHashCode().Should().Be(value.GetHashCode());
		}

		[TestMethod]
		public void INotifiedPropertyChanged_implementation_can_be_converted_to_reactive()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			var someData = viewModel.ToReactive(x => x.SomeData);

			// Act
			viewModel.SomeData += 1;

			// Assert
			someData.Value.Should().Be(viewModel.SomeData);
		}

		[TestMethod]
		public void Reactive_created_from_INotifiedPropertyChanged_should_notify_on_wrapped_property_change()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			var someData = viewModel.ToReactive(v => v.SomeData);
			var notified = false;
			someData.PropertyChanged += (sender, args) => notified = true;

			// Act
			viewModel.SomeData += 1;

			// Assert
			notified.Should().BeTrue();
		}

		[TestMethod]
		public void Reactive_created_from_INotifiedPropertyChanged_should_not_notify_on_non_wrapped_property_change()
		{
			// Arrange
			var viewModel = new SomeViewModel();
			var someData = viewModel.ToReactive(v => v.SomeData);
			var notified = false;
			someData.PropertyChanged += (sender, args) => notified = true;

			// Act
			viewModel.OtherData += 1;

			// Assert
			notified.Should().BeFalse();
		}

		[TestMethod]
		public void After_GC_collecting_proxy_and_notifying_change_source_the_Proxy_should_be_unsubscribed_from_change_source()
		{
			// Arrange
			var counter = Reactive.Of(0);
			var twiceCounter = Reactive.Of(() => counter.Value * 2, counter);
			var handled = false;
			twiceCounter.PropertyChanged += ((sender, args) => handled = true);

			// Act
			// ReSharper disable RedundantAssignment
			twiceCounter = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			counter.Value = 1;

			// Assert
			handled.Should().BeFalse();
		}

		[TestMethod]
		public void When_Where_condition_is_true_Then_value_should_change_and_PropertyChanged_event_raised()
		{
			// Arrange
			const string someName = "Samantha";
			var name = Reactive.Of(someName);
			var notEmptyName = name.Where(x => !string.IsNullOrEmpty(x));
			notEmptyName.MonitorEvents();

			// Act
			const string newName = "Garry";
			notEmptyName.Value = newName;

			// Assert
			notEmptyName.ShouldRaise("PropertyChanged");
			notEmptyName.Value.Should().Be(newName);
		}

		[TestMethod]
		public void When_Where_condition_is_not_true_Then_value_should_not_change_and_PropertyChanged_event_raised()
		{
			// Arrange
			const string someName = "Samantha";
			var name = Reactive.Of(someName);
			var notEmptyName = name.Where(x => !string.IsNullOrEmpty(x));
			notEmptyName.MonitorEvents();

			// Act
			notEmptyName.Value = null;

			// Assert
			notEmptyName.ShouldNotRaise("PropertyChanged");
			notEmptyName.Value.Should().Be(someName);
		}

		[TestMethod]
		public void Non_reactive_object_should_not_be_equal_to_any_reactive()
		{
			// Arrange
			var index = Reactive.Of(1);

			// Act
			// Assert
			index.Equals(1).Should().BeFalse();
		}

		[TestMethod]
		public void I_should_be_able_to_always_notify_value_change_despite_that_new_value_is_equal()
		{
			// Arrange
			var signal = Reactive.Of(0, notifyAlways: true);
			
			var notified = false;
			signal.PropertyChanged += (sender, args) => notified = true;

			// Act
			signal.Value = 0;

			// Assert
			notified.Should().Be(true);
		}

		#region SUT

		private class SomeViewModel : INotifyPropertyChanged
		{
			public int SomeData
			{
				get { return _someData; }
				set
				{
					_someData = value;
					NotifyPropertyChanged("SomeData");
				}
			}

			public int OtherData
			{
				get { return _otherData; }
				set
				{
					_otherData = value;
					NotifyPropertyChanged("OtherData");
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			private int _someData;

			private int _otherData;

			private void NotifyPropertyChanged(string propertyName)
			{
				var handler = PropertyChanged;
				if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public class SomeEvent
		{
		}

		#endregion
	}
}