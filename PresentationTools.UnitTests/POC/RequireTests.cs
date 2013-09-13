using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTools.MsUnit;

namespace PresentationTools.UnitTests.POC
{
	[TestClass]
	public class RequireTests
	{
		[TestMethod]
		[Ignore]
		public void Expression_tree_property_extraction_is_20_slower_than_when_not_using_it()
		{
			// Arrange
			const int iterations = 500000;

			// Act
			var stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < iterations; i++)
			{
				TestModel.MethodWithUsualCheck("hi");
			}
			stopwatch.Stop();
			var usual = stopwatch.ElapsedMilliseconds;

			stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < iterations; i++)
			{
				TestModel.MethodWithLambdaCheck("hi");
			}
			stopwatch.Stop();
			var lambda = stopwatch.ElapsedMilliseconds;

			// Assert
			(usual * 20).ShouldBeGreaterThan(lambda);
		}

		#region SUT

		private static class TestModel
		{
			public static void MethodWithUsualCheck(string parameter)
			{
				if (parameter == null) throw new ArgumentNullException("parameter");
			}

			public static void MethodWithLambdaCheck(string parameter)
			{
				Require.NotNull(parameter, () => () => parameter);
			}
		}

		private static class Require
		{
			public static void NotNull(object argument, Func<Expression<Func<object>>> getArgument)
			{
				if (argument == null)
					throw new ArgumentException(((MemberExpression)getArgument().Body).Member.Name);
			}
		}

		#endregion
	}
}