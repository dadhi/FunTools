using System;
using System.Reflection;

namespace FunTools
{
	public static class Success
	{
		public static Result<T> Of<T>(T result)
		{
			return new Result<T>(result);
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
		public static R Match<T, R>(this Result<T> source, Func<T, R> matchSuccess, Func<Exception, R> matchFailure)
		{
			return source.IsSuccess ? matchSuccess(source.Success) : matchFailure(source.Failure);
		}

		public static Result<R> Map<T, R>(this Result<T> source, Func<T, R> map)
		{
			return source.Match(x => Success.Of(map(x)), Failure.Of<R>);
		}

		public static T SuccessOrDefault<T>(this Result<T> source, T orDefault = default(T))
		{
			return source.IsSuccess ? source.Success : orDefault;
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
				if (!IsFailure)
					throw new InvalidOperationException("Failure is not defined for Success value.");
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

	public sealed class Empty
	{
		public static readonly Empty Value = new Empty();
	}
}
