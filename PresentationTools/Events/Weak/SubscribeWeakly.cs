using System;
using System.ComponentModel;

namespace PresentationTools.Events.Weak
{
	public static class SubscribeWeakly
	{
		public static IDisposable OnCustomDelegate<TEventHandler, TEvent, TSubscriber>(
			Func<Action<object, TEvent>, TEventHandler> convertActionToHandler,
			Action<TEventHandler> subscribe,
			Action<TEventHandler> unsubscribe,
			TSubscriber subscriber,
			Action<TSubscriber, object, TEvent> handler)
			where TEventHandler : class
			where TSubscriber : class
		{
			return new WeakSubscriberSubscription<TSubscriber, TEventHandler, TEvent>(
				subscriber,
				convertActionToHandler,
				subscribe,
				unsubscribe,
				handler);
		}

		public static IDisposable OnCustomDelegate<TEventHandler, TEvent>(
			Func<Action<object, TEvent>, TEventHandler> convertActionToHandler,
			Action<TEventHandler> subscribe,
			Action<TEventHandler> unsubscribe,
			Action<object, TEvent> method)
			where TEventHandler : class
			where TEvent : class
		{
			return OnCustomDelegate(
				convertActionToHandler,
				subscribe,
				unsubscribe,
				method.Target,
				GetOpenHandler.FromEventHandler<TEvent>(method.Method));
		}

		public static IDisposable OnGenericAction<TEvent, TSubscriber>(
			Action<Action<TEvent>> subscribe,
			Action<Action<TEvent>> unsubscribe,
			TSubscriber subscriber,
			Action<TSubscriber, TEvent> handler)
			where TSubscriber : class
		{
			return OnCustomDelegate<Action<TEvent>, TEvent, TSubscriber>(
				h => e => h(null, e),
				subscribe,
				unsubscribe,
				subscriber,
				(s, sender, e) => handler(s, e));
		}

		public static IDisposable OnGenericEventHandler<TEventArgs, TSubscriber>(
			Action<EventHandler<TEventArgs>> subscribe,
			Action<EventHandler<TEventArgs>> unsubscribe,
			TSubscriber subscriber,
			Action<TSubscriber, object, TEventArgs> handler)
			where TEventArgs : EventArgs
			where TSubscriber : class
		{
			return OnCustomDelegate(h => h.Invoke, subscribe, unsubscribe, subscriber, handler);
		}

		public static IDisposable OnEventHandler<TSubscriber>(
			Action<EventHandler> subscribe,
			Action<EventHandler> unsubscribe,
			TSubscriber subscriber,
			Action<TSubscriber, object, EventArgs> handler)
			where TSubscriber : class
		{
			return OnCustomDelegate(h => h.Invoke, subscribe, unsubscribe, subscriber, handler);
		}
	}

	public static class WeakSubscriptionExtensions
	{
		public static IDisposable SubscribeWeakly<TSender, TSubscriber>(
			this TSender sender,
			TSubscriber subscriber,
			Action<TSubscriber, TSender, PropertyChangedEventArgs> handler)
			where TSender : class, INotifyPropertyChanged
			where TSubscriber : class
		{
			return Weak.SubscribeWeakly.OnCustomDelegate<PropertyChangedEventHandler, PropertyChangedEventArgs, TSubscriber>(
				a => a.Invoke,
				h => sender.PropertyChanged += h,
				h => sender.PropertyChanged -= h,
				subscriber,
				(sub, s, e) => handler(sub, (TSender)s, e));
		}

        //public static IDisposable SubscribeWeakly<TSender, TProperty>(
        //    this TSender sender,
        //    Func<TSender, TProperty> property,
        //    Action<TProperty> method)
        //    where TSender : class, INotifyPropertyChanged
        //{
        //    if (method == null)
        //        throw new ArgumentNullException("method");

        //    var subscriber = method.Target;
        //    var methodInfo = method.Method;
        //    if (subscriber == null || methodInfo.IsStatic)
        //        throw new InvalidOperationException("Static methods are not supported");

        //    var propertyName = ExtractName.From(property);
        //    var openHandler = GetOpenHandler.FromEventAction<TProperty>(methodInfo);

        //    return sender.SubscribeWeakly(
        //        subscriber,
        //        (t, s, e) =>
        //        {
        //            if (e == null ||
        //                string.IsNullOrEmpty(e.PropertyName) ||
        //                string.Equals(e.PropertyName, propertyName))
        //            {
        //                openHandler(t, property(s));
        //            }
        //        });
        //}
	}
}
