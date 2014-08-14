using System;
using FunTools.Reactives;
using NUnit.Framework;

namespace FunTools.UnitTests.Reactives
{
	[TestFixture]
	public class ReactiveCommandTests
	{
		[Test]
		public void Command_should_execute_provided_action()
		{
			// Arrange
			var executed = false;
			var command = Reactive.Command(() => executed = true);

			// Act
			command.Execute(null);

			// Assert
			executed.Should().BeTrue();
		}

		[Test]
		public void For_command_created_without_CanExecute_parameter_CanExecute_should_be_true()
		{
			// Arrange
			var counter = 0;
			Action execute = () => counter++;

			// Act
			var command = Reactive.Command(execute);

			// Assert
			command.CanExecute(null).Should().BeTrue();
		}

		[Test]
		public void Сommand_can_be_created_with_name()
		{
			// Arrange
			var counter = 0;
			Action execute = () => counter++;

			// Act
			const string commandName = "Increase Counter";
			var command = Reactive.Command(execute).Named(commandName);

			// Assert
			commandName.Should().Be(command.Name);
		}

		[Test]
		public void When_CanExecute_reactive_condition_changed_then_command_CanExecuteChanged_event_should_be_raised()
		{
			// Arrange
			var canExecute = Reactive.Of(false);
			var command = Reactive.Command(() => { }, canExecute);
			var eventRaised = false;
			command.CanExecuteChanged += (sender, args) => eventRaised = true;

			// Act
			canExecute.Value = !canExecute.Value;

			// Assert
			eventRaised.Should().BeTrue();
		}

		[Test]
		public void When_CanExecute_reactive_condition_changed_then_command_CanExecute_should_changed_accordignly()
		{
			// Arrange			
			var result = false;
			var condition = Reactive.Of(false);
			var command = Reactive.Command(() => result = true, condition);

			// Act
			condition.Value = true;
			command.TryExecute(null);

			// Assert
			result.Should().BeTrue();
		}

		[Test]
		public void CanExecuteChanged_wont_be_raised_if_condition_is_not_reactive()
		{
			// Arrange
			bool[] nonReactiveConditionSource = { false };
			var canExecute = Reactive.Of(() => nonReactiveConditionSource[0]);

			var command = Reactive.Command(() => { }, canExecute);

			var raised = false;
			command.CanExecuteChanged += (sender, e) => raised = true;

			// Act
			nonReactiveConditionSource[0] = true;

			// Assert
			raised.Should().BeFalse();
		}

		[Test]
		public void CanExecuteChanged_is_weak_event_so_when_handler_collected_then_event_should_not_be_handled()
		{
			// Arrange
			var canExecute = Reactive.Of(false);
			var command = Reactive.Command(() => { }, canExecute);

			var handled = false;
			command.CanExecuteChanged += (sender, args) => handled = true;

			// Act
			GC.Collect();
			canExecute.Value = true;

			// Assert
			handled.Should().BeFalse();
		}

		[Test]
		public void Framework_should_be_able_to_unsubscribe_from_CanExecuteChanged()
		{
			// Arrange
			var condition = Reactive.Of(false);
			var command = Reactive.Command(() => { }, condition);

			var handled = false;
			EventHandler onCanExecuteChanged = (sender, args) => handled = true;
			command.CanExecuteChanged += onCanExecuteChanged;

			// Act
			command.CanExecuteChanged -= onCanExecuteChanged;
			condition.Value = true;

			// Assert
			handled.Should().BeFalse();
		}

		[Test]
		public void Can_create_command_with_parameter()
		{
			// Arrange
			var result = 0;
			var command = Reactive.Command<int>(x => result = x);

			// Act
			const int parameter = 1;
			command.TryExecute(parameter);

			// Assert
			result.Should().Be(parameter);
		}

        //[Test]
        //public void Can_create_command_with_parameterized_CanExecute()
        //{
        //    // Arrange
        //    var canExecute = Reactive.Of(false);
        //    var result = 0;
        //    var command = Reactive.Command<int>(x => result = x, x => canExecute && x > 0, canExecute);
        //    command.MonitorEvents();

        //    // Act
        //    canExecute.Value = true;
        //    command.ShouldRaise("CanExecuteChanged");

        //    // Assert
        //    const int parameter = 1;
        //    command.TryExecute(parameter);
        //    result.Should().Be(parameter);
        //}
	}

	public static class CommandExtensions
	{
		public static void TryExecute(this ICommand command, object parameter)
		{
			if (command.CanExecute(parameter))
				command.Execute(parameter);
		}
	}
}