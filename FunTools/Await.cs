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
	// Await.All(Await.Async(..), Await.Event(..))
	// Await.Any(Await.Async(..), Await.Event(..))
	// Await.Async(..).Wait()

	public delegate Cancelable Await<T>(Complete<T> complete);

	public delegate void Complete<T>(Option<Result<T>> result);

	public delegate void Cancelable();

	public static class Await
	{
		public static Await<T> Operation<T>(Func<T> operation, Func<Action, Cancelable> invoker)
		{
			return complete =>
			{
				var completeFirst = new CompleteFirst();
				var cancel = invoker(() =>
				{
					var result = Try.Do(operation);
					completeFirst.Do(() => complete(result));
				});

				return () => completeFirst.Do(() =>
				{
					Try.Do(() => cancel());
					complete(None.Of<Result<T>>());
				});
			};
		}

		public static Await<T> Async<T>(Func<T> action)
		{
			return Operation(action, Setup.AsyncInvoker);
		}

		public static Await<T> UI<T>(Func<T> action)
		{
			return Operation(action, Setup.UIInvoker);
		}

		public static Await<R> Map<T, R>(this Await<T> source, Func<T, R> map)
		{
			return complete => source(result =>
			{
				if (result.IsNone)
					complete(None.Of<Result<R>>());
				else if (result.Some.IsFailure)
					complete(Failure.Of<R>(result.Some.Failure));
				else
				{
					var converted = Try.Do(() => map(result.Some.Success));
					complete(converted.IsSuccess ? Success.Of(converted.Success) : Failure.Of<R>(converted.Failure));
				}
			});
		}

		public static Await<Empty> Take<T>(this Await<T> source, Action<T> take)
		{
			return source.Map(result =>
			{
				take(result);
				return Empty.Value;
			});
		}

		public static Await<R> Many<T, R>(
			Func<Result<T>, int, Option<R>> choose,
			R defaultResult,
			params Await<T>[] sources)
		{
			return complete =>
			{
				var cancels = Stack<Cancelable>.Empty;
				var completeFirst = new CompleteFirst();
				var completeLast = new CompleteLast(sources.Length);

				Complete<R> cancelRestAndComplete = result => completeFirst.Do(() =>
				{
					cancels.Each(x => Try.Do(() => x()));
					complete(result);
				});

				for (var i = 0; i < sources.Length; i++)
				{
					var index = i; // save index to use in lambda
					var current = sources[i](result =>
					{
						if (result.IsNone) // it means that we ignoring external canceling.
							return;

						var choice = Try.Do(() => choose(result.Some, index));
						if (choice.IsFailure)
						{
							cancelRestAndComplete(Failure.Of<R>(choice.Failure));
						}
						else if (choice.Success.IsSome)
						{
							cancelRestAndComplete(Success.Of(choice.Success.Some));
						}
						else // at last try to complete whole workflow with default result.
						{
							completeLast.Do(() => completeFirst.Do(() => complete(Success.Of(defaultResult))));
						}
					});

					if (completeFirst.IsDone) // if all is done just return
						return NothingToCancel;

					cancels = cancels.Add(current);
				}

				return () => cancelRestAndComplete(None.Of<Result<R>>());
			};
		}

		public static Await<R> AwaitSome<T, R>(
			this IEnumerable<Await<T>> sources,
			Func<Result<T>, int, Option<R>> choose,
			R defaultResult = default(R))
		{
			return Many(choose, defaultResult, sources.ToArray());
		}

		public static Await<R> Many<T1, T2, R>(
			Await<T1> source1,
			Await<T2> source2,
			R defaultResult,
			Func<Option<Result<T1>>, Option<Result<T2>>, Option<R>> choose)
		{
			var result1 = None.Of<Result<T1>>();
			var result2 = None.Of<Result<T2>>();
			return Many(
				(_, i) => choose(result1, result2),
				defaultResult,
				source1.Take(result => result1 = Success.Of(result)),
				source2.Take(result => result2 = Success.Of(result)));
		}

		public static Await<Result<T>[]> All<T>(params Await<T>[] sources)
		{
			var results = new Result<T>[sources.Length];
			return Many((x, i) => None.Of<Result<T>[]>().Of(() => results[i] = x), results, sources);
		}

		public static Await<T> Any<T>(params Await<T>[] sources)
		{
			var ignored = default(T);
			return Many(
				(x, i) => Some.Of(x.Success), // if success fails, then we are automatically propagating this error into result Await<T> 
				ignored, sources);
		}

		public static Await<R> Condition<TEventArgs, TEventHandler, R>(
			Func<Option<TEventArgs>, Option<R>> choose,
			Action<TEventHandler> subscribe,
			Action<TEventHandler> unsubscribe,
			Func<Action<object, TEventArgs>, TEventHandler> convert)
		{
			return complete =>
			{
				// Create helper action to safely invoke choose action and supply result to completed.
				Func<Option<TEventArgs>, Complete<R>, bool> tryChooseAndComplete = (e, doComplete) =>
				{
					var choice = Try.Do(() => choose(e));
					if (choice.IsSuccess && choice.Success.IsNone)
						return false;

					doComplete(choice.Map(x => x.Some));
					return true;
				};

				// When some result is chosen or exception thrown in process,
				// Then handle it and return immediately - Nothing to Cancel here.
				if (tryChooseAndComplete(None.Of<TEventArgs>(), complete))
					return NothingToCancel;

				var eventHandler = default(TEventHandler);
				var completeFirst = new CompleteFirst();
				Complete<R> completeAndUnsubscribe = x => completeFirst.Do(() =>
				{
					var unsubscription = Try.Do(() => unsubscribe(eventHandler));

					// Replacing original failure with unsubscription failure if got one.
					complete(unsubscription.IsFailure ? Failure.Of<R>(unsubscription.Failure) : x);
				});

				// Convert action to event handler delegate (ignoring event source) and subscribe it.
				var subscription = Try.Do(() => subscribe(
					eventHandler = convert((_, e) => tryChooseAndComplete(e, completeAndUnsubscribe))));

				if (subscription.IsFailure)
				{
					complete(Failure.Of<R>(subscription.Failure));
					return NothingToCancel;
				}

				// In case that during subscribe, condition become true (e.g. event was already raised and will never be raised again)
				// We are checking condition one more time.
				if (tryChooseAndComplete(None.Of<TEventArgs>(), completeAndUnsubscribe))
					return NothingToCancel;

				// Return Cancel with None result.
				return () => completeAndUnsubscribe(None.Of<Result<R>>());
			};
		}

		public static Await<R> Event<TEventArgs, TEventHandler, R>(
			Func<TEventArgs, Option<R>> choose,
			Action<TEventHandler> subscribe,
			Action<TEventHandler> unsubscribe,
			Func<Action<object, TEventArgs>, TEventHandler> convert)
		{
			return Condition(e => e.IsSome ? choose(e.Some) : None.Of<R>(), subscribe, unsubscribe, convert);
		}

		public static Cancelable Do<T>(
			this Await<T> source,
			Action<T> onSuccess = null,
			Action<Exception> onFailure = null,
			Action onCancel = null,
			Action<Result<T>> onResult = null)
		{
			return source(x => x.Do(onResult ?? (result => result.Do(onSuccess, onFailure)), onCancel));
		}

		public static Option<Result<T>> Wait<T>(this Await<T> source, int timeoutMilliseconds = Timeout.Infinite)
		{
			var completed = new AutoResetEvent(false);

			var result = None.Of<Result<T>>();
			var cancel = source(x =>
			{
				result = x;
				completed.Set();
			});

			if (completed.WaitOne(timeoutMilliseconds))
				return result;

			cancel();
			return None.Of<Result<T>>();
		}

		public static T WaitSuccess<T>(this Await<T> source, int timeoutMilliseconds = Timeout.Infinite)
		{
			return source.Wait(timeoutMilliseconds).Some.Success;
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
					return NothingToCancel;
				}

				public static Cancelable QueueToThreadPool(Action action)
				{
					ThreadPool.QueueUserWorkItem(_ => action());
					return NothingToCancel;
				}
			}
		}

		public static Cancelable NothingToCancel = () => { };
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

		private static Cancelable DoAwait<T>(IEnumerator<Awaiting<T>> source, Complete<T> complete, CompleteFirst completer)
		{
			if (completer.IsDone)
				return NothingToCancel;

			var movingNext = Try.Do(() =>
			{
				if (source.MoveNext())
				{
					var awaiting = source.Current;
					if (awaiting == null)
						throw new InvalidOperationException(EXPECTING_NOTNULL_AWAITING_YIELDED.Of(typeof(Awaiting<T>)));
					return awaiting;
				}

				if (typeof(T) != typeof(Empty))
					throw new InvalidOperationException(EXPECTING_NONEMPTY_RESULT.Of(typeof(T)));
				return None.Of<Awaiting<T>>();
			});

			if (movingNext.IsFailure)
			{
				completer.Do(() => complete(Failure.Of<T>(movingNext.Failure)));
				return NothingToCancel;
			}

			if (movingNext.Success.IsNone)
			{
				completer.Do(() => complete(Success.Of((T)(object)Empty.Value)));
				return NothingToCancel;
			}

			var currentAwaiting = movingNext.Success.Some;
			if (currentAwaiting.IsCompleted)
			{
				completer.Do(() => complete(Success.Of(currentAwaiting.Result)));
				return NothingToCancel;
			}

			Cancelable cancelNext = null;
			var cancelCurrent = currentAwaiting.Proceed(
				_ => cancelNext = DoAwait(source, complete, completer));

			return () => completer.Do(() =>
			{
				(cancelNext ?? cancelCurrent)();
				complete(None.Of<Result<T>>());
			});
		}

		internal static Cancelable NothingToCancel = () => { };

		private static readonly string EXPECTING_NOTNULL_AWAITING_YIELDED = "Not expecting null of {0} to be yielded.";
		private static readonly string EXPECTING_NONEMPTY_RESULT = "Expecting result of {0} but found Empty.";

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
			var genericArgsString = type.IsGenericTypeDefinition ? new string(',', genericArgs.Length - 1)
				: string.Join(", ", genericArgs.Select(x => x.Display()).ToArray());
			return name.Substring(0, name.LastIndexOf('`')) + "<" + genericArgsString + ">";
		}

		public static string Of(this string format, params object[] args)
		{
			return string.Format(format, args.Select(x => x is Type ? ((Type)x).Display() : x).ToArray());
		}

		public static void Each<T>(this IList<T> source, Action<T> action)
		{
			var count = source.Count;
			for (var i = 0; i < count; i++)
				action(source[i]);
		}

		public static void Each<T>(this IEnumerable<T> source, Action<T> action)
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
