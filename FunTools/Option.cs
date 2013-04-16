using System;

namespace FunTools
{
	public static class Value
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
	}

	public static class Option
	{
		public static R Match<T, R>(this Option<T> source, Func<T, R> matchValue, Func<R> matchNone)
		{
			return source.HasValue ? matchValue(source.Value) : matchNone();
		}

		public static Option<R> Map<T, R>(this Option<T> source, Func<T, R> map)
		{
			return source.Match(x => Value.Of(map(x)), None.Of<R>);
		}

		public static T ValueOrDefault<T>(this Option<T> source, T orDefault = default(T))
		{
			return source.HasValue ? source.Value : orDefault;
		}
	}

	public sealed class Option<T>
	{
		public static implicit operator Option<T>(T value)
		{
			return new Option<T>(value);
		}

		public readonly static Option<T> None = new Option<T>();

		public T Value
		{
			get
			{
				if (IsNone)
					throw new InvalidOperationException("Some is not defined for None.");
				return _value;
			}
		}

		public bool HasValue
		{
			get { return _hasValue; }
		}

		public bool IsNone
		{
			get { return !_hasValue; }
		}

		public override string ToString()
		{
			return HasValue
				? "Value<" + typeof(T).Name + ">(" + Value + ")"
				: "None<" + typeof(T).Name + ">()";
		}

		public override bool Equals(object obj)
		{
			return (obj is Option<T>) &&
				(((Option<T>)obj).HasValue 
					? HasValue && Equals(((Option<T>)obj).Value, Value)
					: IsNone);
		}

		public override int GetHashCode()
		{
			return HasValue ? Value.GetHashCode() : 0;
		}

		#region Implementation

		private readonly T _value;

		private readonly bool _hasValue;

		internal Option(T value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			_value = value;
			_hasValue = true;
		}

		internal Option()
		{
		}

		#endregion
	}
}