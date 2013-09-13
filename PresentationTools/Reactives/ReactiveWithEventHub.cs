using System;
using PresentationTools.Events.Aggregation;
using PresentationTools.Events.Weak;
using PresentationTools.Events;

namespace PresentationTools.Reactives
{
	public static class ReactiveWithEventHub
	{
		public static Reactive<T> Use<T>(this Reactive<T> source, EventHub eventHub, Action<UseEvenHubTo<T>> to)
		{
			to(new UseEvenHubTo<T>(source, eventHub));
			return source;
		}

		#region Implemetation

		public class UseEvenHubTo<T>
		{
			public UseEvenHubTo(Reactive<T> source, EventHub eventHub)
			{
				_source = source;
				_eventHub = eventHub;
			}

			public UseEvenHubTo<T> Handle<TEvent>(Func<TEvent, T> selector, Func<TEvent, bool> condition = null)
			{
				_eventHub.Subscribe(new Listener<Reactive<T>, TEvent>(
					_source,
					(x, e) =>
					{
						if (condition == null || condition(e))
							x.Value = selector(e);
					}));

				return this;
			}

			public UseEvenHubTo<T> Publish<TEvent>(Func<T, TEvent> getEvent, Func<T, bool> condition = null)
			{
				_source.SubscribeWeakly(
					_eventHub,
					(hub, source, e) =>
					{
						if (condition == null || condition(source))
							hub.Publish(getEvent(source));
					});

				return this;
			}

			#region Implementation

			private readonly Reactive<T> _source;

			private readonly EventHub _eventHub;

			#endregion
		}

		#endregion
	}
}