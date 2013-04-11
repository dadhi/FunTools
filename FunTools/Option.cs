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
	}

	public static class Option
	{
		public static R Match<T, R>(this Option<T> source, Func<T, R> matchSome, Func<R> matchNone)
		{
			return source.IsSome ? matchSome(source.Some) : matchNone();
		}

		public static Option<R> Map<T, R>(this Option<T> source, Func<T, R> map)
		{
			return source.Match(x => Some.Of(map(x)), None.Of<R>);
		}

		public static T Unwrap<T>(this Option<T> source, T defaultValue = default(T))
		{
			return source.IsSome ? source.Some : defaultValue;
		}
	}

	public sealed class Option<T>
	{
		public static implicit operator Option<T>(T some)
		{
			return new Option<T>(some);
		}

		public readonly static Option<T> None = new Option<T>();

		public T Some
		{
			get
			{
				if (IsNone)
					throw new InvalidOperationException("Some is not defined for None.");
				return _some;
			}
		}

		public bool IsSome
		{
			get { return _isSome; }
		}

		public bool IsNone
		{
			get { return !_isSome; }
		}

		public override string ToString()
		{
			return IsSome
				? "Some<" + typeof(T).Name + ">(" + Some + ")"
				: "None<" + typeof(T).Name + ">()";
		}

		public override bool Equals(object obj)
		{
			return (obj is Option<T>) &&
					(((Option<T>)obj).IsSome
						? IsSome && Equals(((Option<T>)obj).Some, Some)
						: IsNone);
		}

		public override int GetHashCode()
		{
			return IsSome ? Some.GetHashCode() : 0;
		}

		#region Implementation

		private readonly T _some;

		private readonly bool _isSome;

		internal Option(T some)
		{
			if (some == null)
				throw new ArgumentNullException("some");
			_some = some;
			_isSome = true;
		}

		internal Option()
		{
		}

		#endregion
	}
}