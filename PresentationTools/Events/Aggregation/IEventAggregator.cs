using System;

namespace PresentationTools.Events.Aggregation
{
	public interface IEventAggregator
	{
		void Subscribe(object subscriber);

		void Unsubscribe(object subscriber);

		void RaiseOn<TSubscriber>(Action<TSubscriber> handler);
	}
}