using System;

namespace FunTools
{
	public static class Some
	{
		public static Option<T> Of<T>(T value)
		{
			return new Option<T>(value);
		}
	}

	public static class None
	{
		public static Option<T> Of<T>()
		{
			return Option<T>.None;
		}

		public static Option<T> Of<T>(object ignored)
		{
			return Option<T>.None;
		}
	}

	public static class Option
	{
		public static R Match<T, R>(this Option<T> source, Func<T, R> matchValue, Func<R> matchNone)
		{
			return source.IsSomeValue ? matchValue(source.SomeValue) : matchNone();
		}

		public static Option<R> Map<T, R>(this Option<T> source, Func<T, R> map)
		{
			return source.Match(x => Some.Of(map(x)), None.Of<R>);
		}

		public static T SomeValueOrDefault<T>(this Option<T> source, T orDefault = default(T))
		{
			return source.IsSomeValue ? source.SomeValue : orDefault;
		}
	}

	public sealed class Option<T>
	{
		public static implicit operator Option<T>(T value)
		{
			return new Option<T>(value);
		}

		public readonly static Option<T> None = new Option<T>();

		public T SomeValue
		{
			get
			{
				if (IsNone)
					throw new InvalidOperationException("Some is not defined for None.");
				return _value;
			}
		}

		public bool IsSomeValue
		{
			get { return _isSomeValue; }
		}

		public bool IsNone
		{
			get { return !_isSomeValue; }
		}

		public override string ToString()
		{
			return IsSomeValue
				? "Some<" + typeof(T).Name + ">(" + SomeValue + ")"
				: "None<" + typeof(T).Name + ">()";
		}

		public override bool Equals(object obj)
		{
			return (obj is Option<T>) &&
				(((Option<T>)obj).IsSomeValue 
					? IsSomeValue && Equals(((Option<T>)obj).SomeValue, SomeValue)
					: IsNone);
		}

		public override int GetHashCode()
		{
			return IsSomeValue ? SomeValue.GetHashCode() : 0;
		}

		#region Implementation

		private readonly T _value;

		private readonly bool _isSomeValue;

		internal Option(T value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			_value = value;
			_isSomeValue = true;
		}

		internal Option()
		{
		}

		#endregion
	}
}