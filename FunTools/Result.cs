using System;
using System.Reflection;

namespace FunTools
{
    public static class Success
    {
        public static Result<T> Of<T>(T success)
        {
            return new Result<T>(success);
        }
    }

    public static class Error
    {
        public static Result<T> Of<T>(Exception error)
        {
            return new Result<T>(error);
        }
    }

    public static class Result
    {
        public static Result<T> Of<T>(T success, Exception error)
        {
            return error == null ? Success.Of(success) : Error.Of<T>(error);
        }

        public static R Match<T, R>(this Result<T> source, Func<T, R> success, Func<Exception, R> error)
        {
            return source.IsSuccess ? success(source.Success) : error(source.Error);
        }

        public static R Match<T, R>(this Result<T> source, Func<T, R> success, R defaultValue)
        {
            return source.IsSuccess ? success(source.Success) : defaultValue;
        }

        public static void Match<T>(this Result<T> source, Action<T> success = null, Action<Exception> error = null)
        {
            if (source.IsSuccess)
            {
                if (success != null)
                    success(source.Success);
            }
            else if (error != null)
                error(source.Error);
        }

        public static Result<R> Map<T, R>(this Result<T> source, Func<T, R> map)
        {
            return source.Match(x => Success.Of(map(x)), Error.Of<R>);
        }

        public static Result<T> OnSuccess<T>(this Result<T> source, Action<T> action)
        {
            if (source.IsSuccess) action(source.Success);
            return source;
        }

        public static Result<T> OnError<T>(this Result<T> source, Action<Exception> action)
        {
            if (source.IsError) action(source.Error);
            return source;
        }

        public static T SuccessOrDefault<T>(this Result<T> source, T defaultValue = default(T))
        {
            return source.IsSuccess ? source.Success : defaultValue;
        }

        /// <summary>
        /// Re-throws exception with preserving it previous stack-trace.
        /// </summary>
        /// <param name="error">exception to re-throw</param>
        public static void ReThrow(this Exception error)
        {
            Setup.PreserveStackTrace(error);
            throw error;
        }

        public static class Setup
        {
            public static Action<Exception> PreserveStackTrace = Defaults.CallPrivateMethodOnExceptionWithReflection;

            public static class Defaults
            {
                public static readonly Action<Exception> CallPrivateMethodOnExceptionWithReflection =
                    (Action<Exception>)Delegate.CreateDelegate(
                        typeof(Action<Exception>),
                        null,
                        typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }
    }

    public sealed class Result<T>
    {
        public static implicit operator Result<T>(T value)
        {
            return new Result<T>(value);
        }

        public static implicit operator Result<T>(Exception error)
        {
            return new Result<T>(error);
        }

        public T Success
        {
            get
            {
                if (IsError)
                    _error.ReThrow();
                return _success;
            }
        }

        public Exception Error
        {
            get
            {
                if (IsSuccess)
                    throw new InvalidOperationException("Expecting Error but found Success instead.");
                return _error;
            }
        }

        public bool IsSuccess
        {
            get { return _isSuccess; }
        }

        public bool IsError
        {
            get { return !_isSuccess; }
        }

        public override string ToString()
        {
            return IsSuccess
                ? "Success<" + typeof(T).Name + ">(" + ((object)Success == null ? "null" : Success.ToString()) + ")"
                : "Error<" + typeof(T).Name + ">(\n" + Error + "\n)";
        }

        #region Implementation

        private readonly T _success;

        private readonly Exception _error;

        private readonly bool _isSuccess;

        internal Result(T success)
        {
            _success = success;
            _error = null;
            _isSuccess = true;
        }

        internal Result(Exception error)
        {
            if (error == null) throw new ArgumentNullException("error");
            _error = error;
            _success = default(T);
            _isSuccess = false;
        }

        #endregion
    }

    public static class Try
    {
        public static Result<T> Do<T>(Func<T> action, Action<Exception> onError = null)
        {
            try
            {
                return Success.Of(action());
            }
            catch (Exception error)
            {
                (onError ?? Setup.OnError)(error);
                return Error.Of<T>(error);
            }
        }

        public static Result<Empty> Do(Action action, Action<Exception> onError = null)
        {
            try
            {
                action();
                return Empty.Value;
            }
            catch (Exception ex)
            {
                (onError ?? Setup.OnError)(ex);
                return Error.Of<Empty>(ex);
            }
        }

        public static class Setup
        {
            public static Action<Exception> OnError = DoNothing;

            public static void DoNothing(Exception ex) { }
        }
    }

    public sealed class Empty
    {
        public static readonly Empty Value = new Empty();

        public static Func<Empty> ReturnOf(Action action)
        {
            return () =>
            {
                action();
                return Empty.Value;
            };
        }
    }
}
