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

	public static class Failure
	{
		public static Result<T> Of<T>(Exception failure)
		{
			return new Result<T>(failure);
		}
	}

	public static class Result
	{
		public static Result<T> Of<T>(T success, Exception failure)
		{
			return failure == null ? Success.Of(success) : Failure.Of<T>(failure);
		}
		
		public static R ConvertTo<T, R>(this Result<T> source, Func<T, R> success, Func<Exception, R> failure)
		{
			return source.IsSuccess ? success(source.Success) : failure(source.Failure);
		}

		public static R ConvertTo<T, R>(this Result<T> source, Func<T, R> success, R defaultValue)
		{
			return source.IsSuccess ? success(source.Success) : defaultValue;
		}

		public static void Do<T>(this Result<T> source, Action<T> success, Action<Exception> failure)
		{
			if (source.IsSuccess) success(source.Success);
			else failure(source.Failure);
		}

		public static Result<R> Map<T, R>(this Result<T> source, Func<T, R> map)
		{
			return source.ConvertTo(x => Success.Of(map(x)), Failure.Of<R>);
		}

		public static Result<T> OnSuccess<T>(this Result<T> source, Action<T> action)
		{
			if (source.IsSuccess) action(source.Success);
			return source;
		}

		public static Result<T> OnFailure<T>(this Result<T> source, Action<Exception> action)
		{
			if (source.IsFailure) action(source.Failure);
			return source;
		}

		public static T SuccessOrDefault<T>(this Result<T> source, T defaultValue = default(T))
		{
			return source.IsSuccess ? source.Success : defaultValue;
		}

		/// <summary>
		/// Re-throws exception with preserving it previous stack-trace.
		/// </summary>
		/// <param name="failure">exception to re-throw</param>
		public static void ReThrow(this Exception failure)
		{
			Setup.PreserveStackTrace(failure);
			throw failure;
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

		public static implicit operator Result<T>(Exception failure)
		{
			return new Result<T>(failure);
		}

		public T Success
		{
			get
			{
				if (IsFailure)
					_failure.ReThrow();
				return _success;
			}
		}

		public Exception Failure
		{
			get
			{
				if (IsSuccess)
					throw new InvalidOperationException("Expecting Failure but found Success instead.");
				return _failure;
			}
		}

		public bool IsSuccess
		{
			get { return _isSuccess; }
		}

		public bool IsFailure
		{
			get { return !_isSuccess; }
		}

		public override string ToString()
		{
			return IsSuccess
				? "Success<" + typeof(T).Name + ">(" + ((object)Success == null ? "null" : Success.ToString()) + ")"
				: "Failure<" + typeof(T).Name + ">(\n" + Failure + "\n)";
		}

		#region Implementation

		private readonly T _success;

		private readonly Exception _failure;

		private readonly bool _isSuccess;

		internal Result(T success)
		{
			_success = success;
			_failure = null;
			_isSuccess = true;
		}

		internal Result(Exception failure)
		{
			if (failure == null) throw new ArgumentNullException("failure");
			_failure = failure;
			_success = default(T);
			_isSuccess = false;
		}

		#endregion
	}

	public static class Try
	{
		public static Result<T> Do<T>(Func<T> action, Action<Exception> onFailure = null)
		{
			try
			{
				return Success.Of(action());
			}
			catch (Exception ex)
			{
				(onFailure ?? Setup.OnFailure)(ex);
				return Failure.Of<T>(ex);
			}
		}

		public static Result<Empty> Do(Action action, Action<Exception> onFailure = null)
		{
			try
			{
				action();
				return Empty.Value;
			}
			catch (Exception ex)
			{
				(onFailure ?? Setup.OnFailure)(ex);
				return Failure.Of<Empty>(ex);
			}
		}

		public static class Setup
		{
			public static Action<Exception> OnFailure = DoNothing;

			public static void DoNothing(Exception ex) { }
		}
	}

	public sealed class Empty
	{
		public static readonly Empty Value = new Empty();

		public static Empty Do(Action action)
		{
			action();
			return Value;
		}
	}
}
