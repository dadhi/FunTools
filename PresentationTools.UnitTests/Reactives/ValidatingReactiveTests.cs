using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools.Reactives;

namespace PresentationTools.UnitTests.Reactives
{
	[TestClass]
	public class ValidatingReactiveTests
	{
		[TestMethod]
		public void Validation_result_should_be_available_from_IDataErrorInfo_Indexer_ignoring_indexer_argument()
		{
			// Arrange
			var counter = Reactive.Of(0);
			const string invalidValueResultFormat = "Invalid value provided: {0}";
			var validatingCounter = counter.Validate(x => false, x => string.Format(invalidValueResultFormat, x));

			// Act
			validatingCounter.Value = 1;

			// Assert
			const string ignored = null;
			validatingCounter[ignored].Should().Be(string.Format(invalidValueResultFormat, 1));
		}

		[TestMethod]
		public void The_same_validation_result_should_be_obtained_both_from_IDataErrorInfo_Indexer_and_Error_property()
		{
			// Arrange
			var counter = Reactive.Of(0);
			const string invalidValueResultFormat = "Invalid value provided: {0}";
			var validatingCounter = counter.Validate(x => false, x => string.Format(invalidValueResultFormat, x));

			// Act
			validatingCounter.Value = 1;

			// Assert
			const string ignored = null;
			var validationInfo = (IDataErrorInfo)validatingCounter;
			validationInfo.Error.Should().Be(validationInfo[ignored]);
		}

		[TestMethod]
		public void IsValid_method_should_return_False_if_Validate_method_was_called_and_result_error_is_Not_Null()
		{
			// Arrange
			var counter = Reactive.Of(0).Validate(x => x != 1, "error");

			// Act
			counter.Value = 1;

			// Assert
			counter.IsValid.Value.Should().BeFalse();
		}

		[TestMethod]
		public void IsValid_method_should_return_True_if_Validate_method_was_called_and_result_error_is_Null()
		{
			// Arrange
			var counter = Reactive.Of(0).Validate(x => x != 1, "error");

			// Act
			counter.Value = 2;

			// Assert
			counter.IsValid.Value.Should().BeTrue();
		}

		[TestMethod]
		public void Can_create_reactive_with_validation_error()
		{
			// Arrange
			const string message = "Name is too long";
			var name = Reactive.Of<string>().Validate(x => x.Length <= 10, message);

			// Act
			name.Value = "America-America";

			// Assert
			name.ValidationError.Value.Should().Be(message);
		}

		[TestMethod]
		public void For_Validating_reactive_when_value_set_silently_then_PropertyChanged_should_not_be_raised()
		{
			// Arrange
			var counter = Reactive.Of(0).Validate(i => "invalid");
			var raised = false;
			counter.PropertyChanged += (sender, args) => raised = true;

			// Act
			counter.SetValueSilently(1);

			// Assert
			raised.Should().BeFalse();
			counter.Value.Should().Be(1);
		}

		[TestMethod]
		public void When_more_than_one_validation_is_failing_then_Error_should_contain_message_from_the_first()
		{
			// Arrange
			const string a = "a";
			const string b = "b";

			var message = Reactive.Of<string>()
				.Validate(x => !x.Contains(a), a)
				.Validate(x => !x.Contains(b), b);

			// Act
			message.Value = a + b;

			// Assert
			message.Error.Should().Be(a);
		}

		[TestMethod]
		public void When_more_than_one_validation_applied_and_first_is_failed_then_the_second_should_not_be_called()
		{
			// Arrange
			const string a = "a";
			var secondValidationCalled = false;
			
			var message = Reactive.Of<string>()
				.Validate(x => !x.Contains(a), string.Empty)
				.Validate(x => secondValidationCalled = true, string.Empty);

			// Act
			message.Value = a;

			// Assert
			secondValidationCalled.Should().BeFalse();
		}

		[TestMethod]
		public void Should_be_able_to_get_reactive_validation_info_for_validating_reactive()
		{
			// Arrange
			const string counterShouldbeGreaterThanZero = "Should be greater than 0";

			Reactive<int> counter = Reactive.Of(10)
				.Validate(x => x > 0, counterShouldbeGreaterThanZero);

			// Act
			counter.Value = 0;
			var isValid = counter.IsValid();
			var validationError = counter.ValidationError();

			// Assert
			isValid.Value.Should().BeFalse();
			validationError.Value.Should().Be(counterShouldbeGreaterThanZero);
		}

		[TestMethod]
		public void Non_validating_reactive_should_always_be_valid()
		{
			// Arrange
			var counter = Reactive.Of(10);

			// Act
			counter.Value = 0;
			var isValid = counter.IsValid();
			var validationError = counter.ValidationError();

			// Assert
			isValid.Value.Should().BeTrue();
			validationError.Value.Should().BeNull();
		}
	}
}
