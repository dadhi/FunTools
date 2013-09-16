using System;
using System.Diagnostics;
using System.Linq.Expressions;
using FluentAssertions;
using FunTools.Changed;
using NUnit.Framework;

namespace FunTools.UnitTests.Changed
{
	[TestFixture]
	public class ExtractNamePerformanceTests
	{
		[Test]
		[Ignore]
		public void Extracting_name_should_be_1_5_factor_faster_than_lambda_expression_approach()
		{
			// Arrange
			const int times = 500000;
			var propertyName = "Property";
			var providedName = "PropertyA";

			// Act
			var stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < times; i++)
				if (string.Equals(propertyName, providedName))
					break;
			stopwatch.Stop();
			var compareStrings = stopwatch.ElapsedMilliseconds;

			stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < times; i++)
			{
				try
				{
					if (string.Equals(propertyName, providedName))
						break;
				}
				catch (Exception)
				{
					break;
				}
			}
			stopwatch.Stop();
			var compareStringsInTry = stopwatch.ElapsedMilliseconds;

			var model = new SomeModel();
			stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < times; i++)
				if (string.Equals(ExtractName.From(() => model.Property), providedName))
					break;
			stopwatch.Stop();
			var funcToName = stopwatch.ElapsedMilliseconds;

			stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < times; i++)
				if (string.Equals(GetMemberName(() => model.Property), providedName))
					break;
			stopwatch.Stop();
			var expressionToName = stopwatch.ElapsedMilliseconds;

			// Assert
			(compareStrings * 100).Should().BeGreaterThan(compareStringsInTry);
			(compareStrings * 1000).Should().BeGreaterThan(funcToName);
			(compareStrings * 2000).Should().BeGreaterThan(expressionToName);
		}

		[Test]
		[Ignore]
		public void Extracting_name_should_take_less_then_evaluated_time()
		{
			// Arrange
			const int times = 500000;
			var providedName = "PropertyA";

			// Act
			var model = new SomeModel();

			var stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < times; i++)
				if (string.Equals(ExtractName.From(() => model.Property), providedName))
					break;
			stopwatch.Stop();
			var funcToName = stopwatch.ElapsedMilliseconds;

			// Assert
			funcToName.Should().BeLessThan(3000);
		}

		#region CUT

		internal class SomeModel
		{
			public string Property { get; set; }
		}

		#endregion

		#region Implementation

		internal static string GetMemberName<T>(Expression<Func<T>> getMemberExpression)
		{
			if (getMemberExpression == null) throw new ArgumentNullException("getMemberExpression");

			var unaryExpression = getMemberExpression.Body as UnaryExpression;
			var memberExpression = unaryExpression != null ? unaryExpression.Operand : getMemberExpression.Body;

			return ((MemberExpression)memberExpression).Member.Name;
		}

		#endregion
	}
}