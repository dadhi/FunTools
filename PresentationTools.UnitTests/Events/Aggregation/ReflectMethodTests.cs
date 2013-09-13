using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools.Events.Aggregation;
using I = FluentAssertions.AssertionExtensions;

namespace PresentationTools.UnitTests.Events.Aggregation
{
	[TestClass]
	public class ReflectMethodTests
	{
		[TestMethod]
		public void Should_throw_exception_if_provided_method_info_is_null()
		{
			// Arrange
			// Act
			// Assert
			I.ShouldThrow<ArgumentException>(() =>
				typeof(SomeService).GetMethod("DoesNotExist").ToOpenHandlerOfOneArg<ReflectMethodTests>());
		}
		[TestMethod]
		public void Should_throw_exception_if_provided_method_info_is_for_parameterless_method()
		{
			// Arrange
			// Act
			// Assert
			I.ShouldThrow<ArgumentException>(() =>
				typeof(SomeService).GetMethod("MethodWithZeroArg").ToOpenHandlerOfOneArg<ReflectMethodTests>());
		}

		#region CUT

		class SomeService
		{
			public void MethodWithZeroArg()
			{
			}
		}

		#endregion
	}
}
