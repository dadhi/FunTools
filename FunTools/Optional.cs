using System;

namespace FunTools
{
    public static class Some
    {
        public static Optional<T> Of<T>(T value)
        {
            return new Optional<T>(value);
        }
    }

    public static class None
    {
        public static Optional<T> Of<T>()
        {
            return Optional<T>.None;
        }
    }

    public static class Optional
    {
        public static Optional<T> Of<T>(T value)
        {
            return (object)value == null ? None.Of<T>() : Some.Of(value);
        }

        public static R Match<T, R>(this Optional<T> source, Func<T, R> some, Func<R> none)
        {
            return source.IsSome ? some(source.Some) : none();
        }

        public static void Match<T>(this Optional<T> source, Action<T> some = null, Action none = null)
        {
            if (source.IsSome)
            {
                if (some != null)
                    some(source.Some);
            }
            else if (none != null)
                none();
        }

        public static Optional<R> Map<T, R>(this Optional<T> source, Func<T, R> map)
        {
            return source.Match(x => Some.Of(map(x)), None.Of<R>);
        }

        public static T SomeOrDefault<T>(this Optional<T> source, T defaultValue = default(T))
        {
            return source.IsSome ? source.Some : defaultValue;
        }
    }

    public sealed class Optional<T>
    {
        public static implicit operator Optional<T>(T value)
        {
            return Optional.Of(value);
        }

        public readonly static Optional<T> None = new Optional<T>();

        public T Some
        {
            get
            {
                if (IsSome) return _value;
                throw new InvalidOperationException("Expecting Some values but found None.");
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
            return (obj is Optional<T>) &&
                (((Optional<T>)obj).IsSome
                    ? IsSome && Equals(((Optional<T>)obj).Some, Some)
                    : IsNone);
        }

        public override int GetHashCode()
        {
            return IsSome ? Some.GetHashCode() : 0;
        }

        #region Implementation

        private readonly T _value;

        private readonly bool _isSome;

        internal Optional(T value)
        {
            if (value == null) throw new ArgumentNullException("value");
            _value = value;
            _isSome = true;
        }

        internal Optional()
        {
        }

        #endregion
    }
}