using System;
using System.ComponentModel;
using FluentAssertions;
using FunTools.Changed;
using NUnit.Framework;

namespace FunTools.UnitTests.Changed
{
	[TestFixture]
	public class NotifyChangeTests
	{
		[Test]
		public void Given_backing_field_When_its_wrapper_set_to_new_value_Then_PC_event_should_be_raised()
		{
			// Arrange
			var model = new SomeModel();

			var propertyChangedRaised = false;
			model.Count.PropertyChanged += (sender, args) => propertyChangedRaised = true;

			// Act
			model.Count.Value = 3;

			// Assert
			propertyChangedRaised.Should().BeTrue();
		}

		[Test]
		public void Given_no_backing_field_When_I_set_wrapper_value_Then_I_should_get_the_same_value()
		{
			// Arrange
			var model = new SomeModel();

			// Act
			model.Message.Value = "welcome";

			// Assert
			model.Message.Value.Should().Be("welcome");
		}

		[Test]
		public void Given_NC_with_no_setter_specified_When_I_assing_any_value_Then_exception_should_be_thrown()
		{
			// Arrange
			var number = new NotifyChange<int>(() => 1);

			// Act
			// Assert
			AssertionExtensions.ShouldThrow<NotSupportedException>(() => number.Value = 2)
				.Where(ex => ex.Message.Contains("Setter is not provided, therefore value assignment is not supported"));
		}

		[Test]
		public void Given_I_have_selected_from_two_sources_When_one_of_them_changed_Then_PC_event_should_be_raised_for_selected_wrapper()
		{
			// Arrange
			var model = new SomeModel();
			var raised = false;
			model.DetailedMessage.PropertyChanged += (sender, args) => raised = true;

			// Act
			model.Count.Value += 1;

			// Assert
			raised.Should().BeTrue();
		}

		[Test]
		public void Given_I_have_selected_from_two_sources_When_any_of_them_changed_Then_selected_value_should_be_changed_accordingly()
		{
			// Arrange
			var model = new SomeModel();

			// Act
			model.Message.Value = "hey";
			model.Count.Value = 1;

			// Assert
			model.DetailedMessage.Value.Should().Be(model.Message.Value + "-" + model.Count.Value);
		}

		[Test]
		public void Given_I_have_selected_from_two_sources_When_selected_result_not_in_use_anymore_and_GC_Then_selected_result_should_be_collected()
		{
			// Arrange
			var model = new SomeModel();
			var doubleCount = NotifyChange.Select(() => model.Count.Value * 2, model.Count);
			var doubleCountWeakRef = new WeakReference(doubleCount);

			// Act
			// ReSharper disable RedundantAssignment
			doubleCount = null;
			// ReSharper restore RedundantAssignment
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			// Assert
			doubleCountWeakRef.IsAlive.Should().BeFalse();
			model.Count.Value.Should().Be(0);
		}

		[Test]
		public void Given_I_have_selected_from_two_sources_When_selected_result_not_in_use_And_sources_are_changed_Then_no_exception_should_be_raised()
		{
			// Arrange
			var model = new SomeModel();
			var detailedMessage = NotifyChange.Select(() => model.Message.Value + model.Count.Value, model.Count, model.Message);
			var detailedMessageWeakRef = new WeakReference(detailedMessage);

			// Act
			// ReSharper disable RedundantAssignment
			detailedMessage = null;
			// ReSharper restore RedundantAssignment

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			// Assert
			AssertionExtensions.ShouldNotThrow(() =>
			{
				model.Count.Value += 1;
				model.Message.Value = "hey";
			});
			detailedMessageWeakRef.IsAlive.Should().BeFalse();
		}

		[Test]
		public void Given_I_have_selected_from_one_wrapper_When_I_change_source_Then_selected_value_should_change_accordingly()
		{
			// Arrange
			var model = new SomeModel();
			var adjustedCount = model.Count.Select(x => x + 3);

			// Act
			model.Count.Value = 2;

			// Assert
			adjustedCount.Value.Should().Be(2 + 3);
		}

		[Test]
		public void Given_I_have_selected_from_one_wrapper_When_I_change_source_Then_PC_event_should_be_raised_for_selected_wrapper()
		{
			// Arrange
			var model = new SomeModel();
			var adjustedCount = model.Count.Select(x => x + 3);
			var raised = false;
			adjustedCount.PropertyChanged += (sender, args) => raised = true;

			// Act
			model.Count.Value = 2;

			// Assert
			raised.Should().BeTrue();
		}

		[Test]
		public void Given_any_INPC_implementation_converted_to_NC_When_I_change_converted_property_Then_converted_value_should_change()
		{
			// Arrange
			var model = new CustomModel();
			var count = model.SelectNotifyChange(x => x.Count);

			// Act
			model.Count = 1;

			// Assert
			count.Value.Should().Be(1);
		}

		[Test]
        [Ignore]
		public void Given_any_INPC_implementation_converted_to_NC_When_I_change_converted_property_Then_NC_should_raise_PC()
		{
			// Arrange
			var model = new CustomModel();
			var count = model.SelectNotifyChange(x => x.Count);
			var raised = false;
			count.PropertyChanged += (sender, args) => raised = true;

			// Act
			model.Count = 1;

			// Assert
			raised.Should().BeTrue();
		}

		[Test]
		public void Given_any_INPC_implementation_converted_to_NC_When_I_change_not_converted_property_Then_NC_should_not_raise_PC()
		{
			// Arrange
			var model = new CustomModel();
			var count = model.SelectNotifyChange(x => x.Count);
			var raised = false;
			count.PropertyChanged += (sender, args) => raised = true;

			// Act
			model.Message = "hey";

			// Assert
			raised.Should().BeFalse();
		}

		[Test]
		public void Given_null_value_When_I_convert_wrapper_to_string_Then_it_should_not_raise_exception()
		{
			// Arrange
			var obj = NotifyChange.Of<object>(null);

			// Act
			// Assert
			var x = string.Empty;
			AssertionExtensions.ShouldNotThrow(() => x = obj.ToString());
			x.Should().Be("Value: Null");
		}

		[Test]
		public void Given_some_invalid_NC_When_I_validate_it_with_error_message_Then_it_should_set_NC_Error_property_to_specified_message()
		{
			// Arrange
			var counter = NotifyChange.Of(-1);

			// Act
			counter = counter.ValidateThat(i => i > 0, "Should be positive");

			// Assert
			counter.Error.Should().Be("Should be positive");
		}

		[Test]
		public void Given_some_invalid_NC_When_I_validate_it_with_error_message_Then_it_should_set_NC_indexer_should_return_the_same_message_as_Error()
		{
			// Arrange
			var counter = NotifyChange.Of(-1);

			// Act
			counter = counter.ValidateThat(i => i > 0, "Should be positive");

			// Assert
			counter["hey"].Should().Be(counter.Error);
		}

		[Test]
		public void Given_some_invalid_NC_When_I_validate_it_without_error_message_Then_it_should_set_NC_Error_property_to_default_error_message()
		{
			// Arrange
			var counter = NotifyChange.Of(-1);

			// Act
			counter = counter.ValidateThat(i => i > 0);

			// Assert
			counter.Error.Should().Be(NotifyChange.Defaults.ValidationErrorMessage);
		}

		[Test]
		public void Given_valid_source_NC_and_another_NC_with_source_Error_property_When_I_assign_invalid_value_to_source_Then_Error_PC_should_be_raised()
		{
			// Arrange
			var counter = NotifyChange.Of(1).ValidateThat(x => x > 0);
			var validationError = counter.SelectNotifyChange(x => x.Error);
			var raised = false;
			validationError.PropertyChanged += (sender, args) => raised = true;

			// Act
			counter.Value = -1;

			// Assert
			raised.Should().BeTrue();
		}

		[Test]
		public void Given_that_no_validation_condition_was_specified_Then_Error_propetry_should_return_null()
		{
			// Arrange
			var count = NotifyChange.Of(0);

			// Act
			// Assert
			count.Error.Should().BeNull();
		}

		#region CUT

		public class SomeModel
		{
			public NotifyChange<int> Count { get; private set; }

			public NotifyChange<string> Message { get; set; }

			public NotifyChange<string> DetailedMessage { get; set; }

			public SomeModel()
			{
				Count = NotifyChange.Of(() => _count, x => _count = x);

				Message = NotifyChange.Of<string>();

				DetailedMessage = NotifyChange.Select(() => Message + "-" + Count, Message, Count);
			}

			private int _count;
		}

		public class CustomModel : INotifyPropertyChanged
		{
			private int _count;
			private string _message;

			public int Count
			{
				get { return _count; }
				set
				{
					_count = value;
					OnPropertyChanged("Count");
				}
			}

			public string Message
			{
				get { return _message; }
				set
				{
					_message = value;
					OnPropertyChanged("Message");
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			private void OnPropertyChanged(string name)
			{
				var handler = PropertyChanged;
				if (handler != null)
					handler(this, new PropertyChangedEventArgs(name));
			}
		}

		#endregion
	}
}