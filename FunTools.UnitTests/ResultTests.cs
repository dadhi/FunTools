using System;
using NUnit.Framework;

namespace FunTools.UnitTests
{
	[TestFixture]
	public class ResultTests
	{
		[Test]
		public void Some_of_success_could_be_readly_composed()
		{
			var result = Some.Of(Success.Of(1));

			Assert.AreEqual(true, result.IsSome);
			Assert.AreEqual(true, result.Some.IsSuccess);
		}

		[Test]
		public void None_of_success_could_be_readly_composed()
		{
			var result = None.Of<Result<int>>();

			Assert.AreEqual(true, result.IsNone);
		}

		[Test]
		public void Display_success_should_be_predictable()
		{
			var displaySuccess = Success.Of(1).ToString();
			Assert.AreEqual("Success<Int32>(1)", displaySuccess);
		}

		[Test]
		public void Display_failure_should_be_predictable()
		{
			var error = new Exception("ex");
			var displaySuccess = Failure.Of<int>(error).ToString();
			Assert.AreEqual("Failure<Int32>(\n" + error + "\n)", displaySuccess);
		}

		[Test]
		public void Test_nested_map()
		{
			var result = Some.Of(Success.Of("hello, world"));
			var words = result.Map(x => x.Map(s => s.Split(',')));

			CollectionAssert.AreEqual(new[] { "hello", " world" }, words.SomeOrDefault().SuccessOrDefault());
		}

		[Test]
		public void Test_nested_match()
		{
			var result = Some.Of(Success.Of("hello, world"));

			var words = 
				result.ConvertTo(
					x => x.ConvertTo(
						s => s.Split(','), 
						_ => new string[0]),
					() => new string[0]);

			CollectionAssert.AreEqual(new[] { "hello", " world" }, words);
		}

		[Test]
		public void Test_convert_Result_to_some_other_type()
		{
			var result = Success.Of(1).ConvertTo(Some.Of, None.Of<int>());
			Assert.AreEqual(1, result.Some);
		}
	}
}