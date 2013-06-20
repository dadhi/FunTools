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
		public static R To<T, R>(this Option<T> source, Func<T, R> onSome, Func<R> onNone)
		{
			return source.IsSome ? onSome(source.Some) : onNone();
		}

		public static void Do<T>(this Option<T> source, Action<T> onSome, Action onNone = null)
		{
			if (source.IsSome) onSome(source.Some);
			else if (onNone != null) onNone();
		}

		public static Option<R> Map<T, R>(this Option<T> source, Func<T, R> map)
		{
			return source.To(x => Some.Of(map(x)), None.Of<R>);
		}

		public static T SomeOrDefault<T>(this Option<T> source, T orDefault = default(T))
		{
			return source.IsSome ? source.Some : orDefault;
		}
	}

	public sealed class Option<T>
	{
		public static implicit operator Option<T>(T value)
		{
			return new Option<T>(value);
		}

		public readonly static Option<T> None = new Option<T>();

		public T Some
		{
			get
			{
				if (IsNone)
					throw new InvalidOperationException("Some is not defined for None.");
				return _value;
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

		private readonly T _value;

		private readonly bool _isSome;

		internal Option(T value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			_value = value;
			_isSome = true;
		}

		internal Option()
		{
		}

		#endregion
	}
}