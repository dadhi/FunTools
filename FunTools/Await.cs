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

	public delegate Cancelable Await<T>(OnCompleted<T> onCompleted);

	public delegate void OnCompleted<T>(Option<Result<T>> result);

	public delegate void Cancelable();

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

		public static Await<T> WithInvoker<T>(Func<T> action, Func<Action, Cancelable> invoker)
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

		public static Await<Option<Result<T>>[]> All<T>(params Await<T>[] sources)
		{
			return completed =>
			{
				var sourceCount = sources.Length;
				var results = new Option<Result<T>>[sourceCount];
				var cancels = new Cancelable[sourceCount];
				var completer = new CompleteLast(sourceCount);

				var subscription = Result.TryGet(() =>
				{
					for (var i = 0; i < sourceCount; i++)
					{
						var index = i;
						cancels[i] = sources[i](result =>
						{
							results[index] = result;
							completer.Do(() => completed(Success.Of(results)));
						});
					}
				});

				if (subscription.IsFailure)
				{
					completer.Do(() => completed(Failure.Of<Option<Result<T>>[]>(subscription.Failure)));
					return () => { };
				}

				return () => completer.Do(() =>
				{
					cancels.ForEach(_ => _());
					completed(Option<Result<Option<Result<T>>[]>>.None);
				});
			};
		}

		public static Await<U> Any<T, U>(Func<Result<T>, int, Option<U>> choose, params Await<T>[] sources)
		{
			return completed =>
			{
				var cancels = Stack<Cancelable>.Empty;
				var completer = new CompleteFirst();

				var subscription = Result.TryGet(() =>
				{
					for (var i = 0; i < sources.Length && !completer.IsDone; i++)
					{
						var index = i; // save inside local scope var to use in lambda below

						Cancelable cancel = null;
						cancel = sources[i](result =>
						{
							if (result.IsNone)
								return;

							var chosen = Result.TryGet(() => choose(result.Some, index));
							if (chosen.IsSuccess && chosen.Success.IsSome)
							{
								completer.Do(() =>
								{
									cancels.Where(x => x != cancel).ForEach(_ => _());
									completed(Success.Of(chosen.Success.Some));
								});
							}
						});

						cancels = cancels.Add(cancel);
					}
				});

				if (subscription.IsFailure)
				{
					completer.Do(() => completed(Failure.Of<U>(subscription.Failure)));
					return () => { };
				}

				return () => completer.Do(() =>
				{
					cancels.ForEach(_ => _());
					completed(None.Of<Result<U>>());
				});
			};
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
			public static Func<Action, Cancelable> AsyncInvoker = Defaults.QueueToThreadPool;

			public static Func<Action, Cancelable> UIInvoker = Defaults.JustInvoke;

			public static Action<Exception> LogFailure = ignored => { };

			public static class Defaults
			{
				public static Cancelable JustInvoke(Action action)
				{
					action();
					return () => { };
				}

				public static Cancelable QueueToThreadPool(Action action)
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

		private static Cancelable DoAwait<T>(IEnumerator<Awaiting<T>> source, OnCompleted<T> completed, CompleteFirst completer)
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

			Cancelable cancelNext = null;
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
	}

	#endregion
}
