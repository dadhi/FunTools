using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FunTools
{
	// Await.Async(..).Do(x => blah)
	// Await.Async(..).OnUI(x => blah)
	// Await.Event("click").OnUI(x => blah)
	// Await.WithEvent("click", x => x > 0).Do()
	// Await.UI(..).Do(..)
	// Await.Try(..).Do(..)
	// Await.All(Await.Async(..), Await.Event(..), Await.Try())
	// Await.Any(Await.Async(..), Await.Event(..), Await.Try())
	// Await.Async(..).Return()

	public delegate Cancel Await<T>(OnCompleted<T> onCompleted);

	public delegate void OnCompleted<T>(Option<Result<T>> result);

	public delegate void Cancel();

	public static class Await
	{
		public static Await<T> Try<T>(Func<T> action)
		{
			return completed =>
			{
				completed(Result.TryGet(action, Setup.LogFailure));
				return () => { };
			};
		}

		public static Await<T> WithInvoker<T>(Func<T> action, Func<Action, Cancel> invoker)
		{
			return completed =>
			{
				var completer = new CompleteFirst();

				var cancel = invoker(() => Try(action)(
					result => completer.Do(() => completed(result))));

				return () => completer.Do(() =>
				{
					cancel();
					completed(None.Of<Result<T>>());
				});
			};
		}

		public static Await<T> Async<T>(Func<T> action)
		{
			return WithInvoker(action, Setup.AsyncInvoker);
		}

		public static Await<T> UI<T>(Func<T> action)
		{
			return WithInvoker(action, Setup.UIInvoker);
		}

		public static Await<R> SomeOrDefault<T, R>(Func<Result<T>, int, Option<R>> chooseSome, R orDefault, params Await<T>[] sources)
		{
			return completed =>
			{
				var cancels = Stack<Cancel>.Empty;
				var completeFirst = new CompleteFirst();
				var completeLast = new CompleteLast(sources.Length);

				Action<Option<Result<R>>> completeWith = result => completeFirst.Do(() =>
				{
					cancels.ForEach(x => x());
					completed(result);
				});

				for (var i = 0; i < sources.Length; i++)
				{
					var index = i; // save index to use in lambda
					var current = sources[i](result =>
					{
						if (result.IsNone) // It means that we ignoring external canceling.
							return;

						var choice = Result.TryGet(() => chooseSome(result.Some, index));
						if (choice.IsFailure)
						{
							completeWith(Failure.Of<R>(choice.Failure));
						}
						else if (choice.Success.IsSome)
						{
							completeWith(Success.Of(choice.Success.Some));
						}
						else // at last try to complete whole workflow with default result.
						{
							completeLast.Do(() => completeFirst.Do(() => completed(Success.Of(orDefault))));
						}
					});

					if (completeFirst.IsDone) // if all is done just return
						return () => { };

					cancels = cancels.Add(current);
				}

				return () => completeWith(None.Of<Result<R>>());
			};
		}

		public static Await<Result<T>[]> All<T>(params Await<T>[] sources)
		{
			var results = new Result<T>[sources.Length];
			return SomeOrDefault((x, i) => None.Of<Result<T>[]>().Of(() => results[i] = x), results, sources);
		}

		public static Await<T> Any<T>(params Await<T>[] sources)
		{
			var ignored = default(T);
			return SomeOrDefault(
				(x, i) => Some.Of(x.Success), // if success fails, then we are automatically propagating this error into result Await<T> 
				ignored, sources);
		}

		public static Option<Result<T>> Return<T>(this Await<T> source, int timeoutMilliseconds = -1)
		{
			var completed = new AutoResetEvent(false);

			var result = Option<Result<T>>.None;
			var cancel = source(x =>
			{
				result = x;
				completed.Set();
			});

			if (completed.WaitOne(timeoutMilliseconds))
				return result;

			cancel();
			return Option<Result<T>>.None;
		}

		public static class Setup
		{
			public static Func<Action, Cancel> AsyncInvoker = Defaults.QueueToThreadPool;

			public static Func<Action, Cancel> UIInvoker = Defaults.JustInvoke;

			public static Action<Exception> LogFailure = ignored => { };

			public static class Defaults
			{
				public static Cancel JustInvoke(Action action)
				{
					action();
					return () => { };
				}

				public static Cancel QueueToThreadPool(Action action)
				{
					ThreadPool.QueueUserWorkItem(_ => action());
					return () => { };
				}
			}
		}
	}

	public sealed class Awaiting<T>
	{
		public static implicit operator Awaiting<T>(T result)
		{
			return new Awaiting<T>(result);
		}

		public static implicit operator Awaiting<T>(Await<Empty> proceed)
		{
			return new Awaiting<T>(proceed);
		}

		internal Awaiting(T result)
		{
			_result = result;
		}

		internal Awaiting(Await<Empty> proceed)
		{
			_proceed = proceed;
		}

		public T Result
		{
			get { return _result; }
		}

		public Await<Empty> Proceed
		{
			get { return _proceed; }
		}

		public bool IsCompleted
		{
			get { return _proceed == null; }
		}

		#region Implementation

		private readonly T _result;

		private readonly Await<Empty> _proceed;

		#endregion
	}

	public static class Awaiting
	{
		public static Awaiting<T> Of<T>(T result)
		{
			return new Awaiting<T>(result);
		}

		public static Await<T> Await<T>(this IEnumerable<Awaiting<T>> source)
		{
			return completed => DoAwait(source.GetEnumerator(), completed, new CompleteFirst());
		}

		#region Implementation

		private static Cancel DoAwait<T>(IEnumerator<Awaiting<T>> source, OnCompleted<T> completed, CompleteFirst completer)
		{
			if (completer.IsDone)
				return () => { };

			var movingNext = Result.TryGet(() =>
			{
				if (source.MoveNext())
				{
					var awaiting = source.Current;
					if (awaiting == null)
						throw new InvalidOperationException(EXPECTING_NOT_NULL_AWAITING_YIELDED.Of(typeof(Awaiting<T>)));
					return awaiting;
				}

				if (typeof(T) != typeof(Empty))
					throw new InvalidOperationException(EXPECTING_NON_EMPTY_RESULT.Of(typeof(T)));
				return None.Of<Awaiting<T>>();
			});

			if (movingNext.IsFailure)
			{
				completer.Do(() => completed(Failure.Of<T>(movingNext.Failure)));
				return () => { };
			}

			if (movingNext.Success.IsNone)
			{
				completer.Do(() => completed(Success.Of((T)(object)Empty.Value)));
				return () => { };
			}

			var currentAwaiting = movingNext.Success.Some;
			if (currentAwaiting.IsCompleted)
			{
				completer.Do(() => completed(Success.Of(currentAwaiting.Result)));
				return () => { };
			}

			Cancel cancelNext = null;
			var cancelCurrent = currentAwaiting.Proceed(
				_ => cancelNext = DoAwait(source, completed, completer));

			return () => completer.Do(() =>
			{
				(cancelNext ?? cancelCurrent)();
				completed(None.Of<Result<T>>());
			});
		}

		private const string EXPECTING_NOT_NULL_AWAITING_YIELDED = "Not expecting null of {0} to be yielded.";
		private const string EXPECTING_NON_EMPTY_RESULT = "Expecting result of {0} but found Empty.";

		#endregion
	}

	#region Implementation

	internal sealed class CompleteFirst
	{
		public bool IsDone
		{
			get { return _isDone == 1; }
		}

		public void Do(Action action)
		{
			if (Interlocked.CompareExchange(ref _isDone, 1, 0) == 0)
				action();
		}

		private volatile int _isDone;
	}

	internal sealed class CompleteLast
	{
		public CompleteLast(int count)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException("count", "Expecting count greater than 0, but found " + count + ".");
			_count = count;
		}

		public bool IsDone
		{
			get { return _count <= 0; }
		}

		public void Do(Action action)
		{
			if (Interlocked.Decrement(ref _count) == 0)
				action();
		}

		private volatile int _count;
	}

	internal class Stack<T> : IEnumerable<T>
	{
		public static readonly Stack<T> Empty = new Stack<T>();

		public readonly T Head;

		public readonly Stack<T> Tail;

		public Stack<T> Add(T head)
		{
			return new Stack<T>(head, this);
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (var x = this; x.Tail != null; x = x.Tail)
				yield return x.Head;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private Stack(T head, Stack<T> tail)
		{
			Head = head;
			Tail = tail;
		}

		private Stack() { }
	}

	internal static class Utils
	{
		public static string Display(this Type type)
		{
			var name = type.Name;
			if (!type.IsGenericType)
				return name;

			var genericArgs = type.GetGenericArguments();
			var genericArgsString = type.IsGenericTypeDefinition
										? new string(',', genericArgs.Length - 1)
										: String.Join(", ", genericArgs.Select(x => x.Display()).ToArray());

			return name.Substring(0, name.LastIndexOf('`')) + "<" + genericArgsString + ">";
		}

		public static string Of(this string format, params object[] args)
		{
			return string.Format(format, args.Select(x => x is Type ? ((Type)x).Display() : x).ToArray());
		}

		public static void ForEach<T>(this IList<T> source, Action<T> action)
		{
			var count = source.Count;
			for (var i = 0; i < count; i++)
				action(source[i]);
		}

		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var x in source)
				action(x);
		}

		public static T Of<T>(this T result, Action action)
		{
			action();
			return result;
		}
	}

	#endregion
}
