using System;

namespace PresentationTools.Events.Aggregation
{
	public static class EventAggregatorExtensions
	{
		public static void Raise<TEvent>(this IEventAggregator aggregator, TEvent e)
		{
			aggregator.RaiseOn<Action<TEvent>>(action => action(e));
			aggregator.RaiseOn<ISubscriber<TEvent>>(subscriber => subscriber.HandleEvent(e));
		}

		public static void SubscribeTo<TEvent>(this IEventAggregator aggregator, Action<TEvent> handler)
		{
			aggregator.Subscribe(handler);
		}

		public static void SubscribeTo<TEvent>(this IEventAggregator aggregator, Action<Action> executeOn, Action<TEvent> handler)
		{
			aggregator.SubscribeTo<TEvent>(e => executeOn(() => handler(e)));
		}
	}
}