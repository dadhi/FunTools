using System;
using System.Threading;

namespace FunTools.Weak
{
	internal sealed class WeakSubscriberSubscription<TSubscriber, TEventHandler, TEvent> : IDisposable
		where TSubscriber : class
		where TEventHandler : class
	{
		public WeakSubscriberSubscription(
			TSubscriber subscriber,
			Func<Action<object, TEvent>, TEventHandler> convertActionToHandler,
			Action<TEventHandler> subscribe,
			Action<TEventHandler> unsubscribe,
			Action<TSubscriber, object, TEvent> handler)
		{
			_unsubscribe = unsubscribe.ThrowIfNull();
			_handler = handler.ThrowIfNull();
			_weakSubscriber = new WeakReference(subscriber.ThrowIfNull());
			_wrappedHandler = convertActionToHandler.ThrowIfNull()(OnEvent);
			subscribe.ThrowIfNull()(_wrappedHandler);
		}

		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
				return;

			_unsubscribe(_wrappedHandler);
			_unsubscribe = null;
			_wrappedHandler = null;
			_handler = null;
		}

		#region Implementation

		private readonly WeakReference _weakSubscriber;

		private Action<TEventHandler> _unsubscribe;

		private Action<TSubscriber, object, TEvent> _handler;

		private TEventHandler _wrappedHandler;

		private int _isDisposed;

		private void OnEvent(object sender, TEvent e)
		{
			var subscriber = _weakSubscriber.Target as TSubscriber;
			if (subscriber != null)
				_handler(subscriber, sender, e);
			else
				Dispose();
		}

		#endregion
	}
}