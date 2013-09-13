using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools.Reactives;
using FluentAssertions.EventMonitoring;

namespace PresentationTools.UnitTests.Reactives
{
	[TestClass]
	public class LazyReactiveTests
	{
		[TestMethod]
		public void Reactive_can_be_lazy_initialized()
		{
			// Arrange
			const string expected = "hey";
			var message = Reactive.OfLazy(() => expected);

			// Act
			// Assert
			message.Value.Should().Be(expected);
		}

		[TestMethod]
		public void Can_assign_value()
		{
			// Arrange
			var message = Reactive.OfLazy(() => "hey");

			// Act
			const string newValue = "wow";
			message.Value = newValue;

			// Assert
			message.Value.Should().Be(newValue);
		}

		[TestMethod]
		public void Can_silently_assign_value()
		{
			// Arrange
			var message = Reactive.OfLazy(() => "hey");

			// Act
			const string newValue = "wow";
			message.SetValueSilently(newValue);

			// Assert
			message.Value.Should().Be(newValue);
		}

		[TestMethod]
		public void When_I_get_and_set_value_after_that_then_set_should_not_be_locked()
		{
			// Arrange
			var counter = Reactive.OfLazy(
				() =>
				{
					Thread.Sleep(200);
					return 1;
				});

			// Act
			var first = counter.Value;
			counter.Value = 2;

			// Assert
			first.Should().Be(1);
			counter.Value.Should().Be(2);
		}

		[TestMethod]
		public void When_value_assigned_then_reactive_should_raise_PropertyChanged_event()
		{
			// Arrange
			var message = Reactive.OfLazy(() => "hey");
			var raised = false;
			message.PropertyChanged += (sender, args) => raised = true;

			// Act
			message.Value = "wow";

			// Assert
			raised.Should().BeTrue();
		}

		[TestMethod]
		public void When_value_silently_assigned_then_reactive_should_not_raise_PropertyChanged_event()
		{
			// Arrange
			var message = Reactive.OfLazy(() => "hey");
			var raised = false;
			message.PropertyChanged += (sender, args) => raised = true;

			// Act
			message.SetValueSilently("wow");

			// Assert
			raised.Should().BeFalse();
		}

		[TestMethod]
		public void When_same_value_assigned_first_time_then_no_PropertyChanged_should_be_raised()
		{
			// Arrange
			var counter = Reactive.OfLazy(() => 1);
			counter.MonitorEvents();

			// Act
			counter.Value = 1;

			// Assert
			counter.ShouldNotRaise("PropertyChanged");
		}

		[TestMethod]
		public void When_same_value_assigned_second_time_then_no_PropertyChanged_should_be_raised()
		{
			// Arrange
			var counter = Reactive.OfLazy(() => 1);
			counter.Value = 1;
			counter.MonitorEvents();

			// Act
			counter.Value = 1;

			// Assert
			counter.ShouldNotRaise("PropertyChanged");
		}

		[TestMethod]
		public void I_should_be_able_to_always_notify_value_change_despite_that_new_value_is_equal()
		{
			// Arrange
			var message = Reactive.OfLazy(() => "blah!", notifyAlways: true);

			var notified = false;
			message.PropertyChanged += (sender, args) => notified = true;

			// Act
			message.Value = "blah!";

			// Assert
			notified.Should().Be(true);
		}
	}
}