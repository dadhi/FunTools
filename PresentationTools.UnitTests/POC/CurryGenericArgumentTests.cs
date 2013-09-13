using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PresentationTools.UnitTests.POC
{
	[TestClass]
	public class CurryGenericArgumentTests
	{
		[TestMethod]
		public void Not_carrying_but_removing_one_generic_arg_by_specifying_explicit_type_in_lambda()
		{
			// Arrange
			const int expected = 1;
			var service = new SomeService<int>();

			// Act
			var result = service.Accept((string source) => expected);

			// Assert
			result.Should().Be(expected);
		}


		[TestMethod]
		public void Currying_with_acceptor()
		{
			// Arrange
			const int expected = 1;
			var service = new SomeService<int>();

			// Act
			var result = service.Acceptor().Accept<string>(source => expected);

			// Assert
			result.Should().Be(expected);
		}

		[TestMethod]
		public void Currying_with_nested_lambda()
		{
			// Arrange
			const int expected = 1;
			var service = new SomeService<int>();

			// Act
			var result = service.With(x => x.Accept<string>(source => expected));

			// Assert
			result.Should().Be(expected);
		}
	}

	internal static class SomeModel
	{
		public static TResult Accept<TSource, TResult>(this SomeService<TResult> service, Func<TSource, TResult> sourceToResult)
		{
			return service.Accept(sourceToResult(default(TSource)));
		}

		public static IAcceptor<TResult> Acceptor<TResult>(this SomeService<TResult> service)
		{
			return new Acceptor<TResult>(service);
		}

		public static TResult With<TResult>(this SomeService<TResult> service, Func<IAcceptor<TResult>, TResult> getResult)
		{
			return getResult(new Acceptor<TResult>(service));
		}
	}

	internal interface IAcceptor<TResult>
	{
		TResult Accept<TSource>(Func<TSource, TResult> getResult);
	}

	internal class Acceptor<TResult> : IAcceptor<TResult>
	{
		public Acceptor(SomeService<TResult> service)
		{
			_service = service;
		}

		public TResult Accept<TSource>(Func<TSource, TResult> getResult)
		{
			return _service.Accept(getResult(default(TSource)));
		}

		private readonly SomeService<TResult> _service;
	}

	internal class SomeService<T>
	{
		public T Accept(T data)
		{
			return data;
		}
	}
}