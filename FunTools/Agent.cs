using System.Collections.Generic;
using System;
using System.Threading;

namespace FunTools
{
	public interface IAgent<TState>
	{
		void Post(Action<TState> action);
	}

	public class Agent<TState> : IAgent<TState>, IDisposable
	{
		public event EventHandler<UnhandledExceptionEventArgs> OnError;

		public Agent(TState state)
		{
			_state = state;
			_thread = new Thread(Execute) { IsBackground = true };
			_thread.Start();
		}

		public void Post(Action<TState> action)
		{
			_actionQueue.Enqueue(action);
		}

		public void Dispose()
		{
			_disposed = true;
			_thread.Join();
		}

		#region Implementation

		private readonly TState _state;

		private readonly AgentQueue<Action<TState>> _actionQueue = new AgentQueue<Action<TState>>();

		private readonly Thread _thread;

		private volatile bool _disposed;

		private void Execute()
		{
			while (!_disposed)
			{
				try
				{
					var action = _actionQueue.Dequeue();
					action(_state);
				}
				catch (Exception ex)
				{
					RaiseOnError(new UnhandledExceptionEventArgs(ex, false));
				}
			}
		}

		private void RaiseOnError(UnhandledExceptionEventArgs e)
		{
			var handler = OnError;
			if (handler != null) handler(this, e);
		}

		#endregion
	}

	public class AgentQueue<T>
	{
		private readonly Queue<T> _queue = new Queue<T>();

		public readonly int MaxSize;

		public static readonly int MaxSizeDeafult = 100;

		public AgentQueue() : this(MaxSizeDeafult) { }

		public AgentQueue(int maxSize)
		{
			if (maxSize <= 0)
				throw new ArgumentException("AgentQueue needs a positive maxSize");
			MaxSize = maxSize;
		}

		public void Enqueue(T value)
		{
			lock (_locker)
			{
				while (_queue.Count >= MaxSize)
					Monitor.Wait(_locker);

				_queue.Enqueue(value);
				Monitor.PulseAll(_locker);
			}
		}

		public T Dequeue()
		{
			lock (_locker)
			{
				while (_queue.Count == 0)
					Monitor.Wait(_locker);

				var value = _queue.Dequeue();
				Monitor.PulseAll(_locker);
				return value;
			}
		}

		private readonly object _locker = new object();
	}
}
