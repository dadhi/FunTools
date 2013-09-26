using FunTools;

namespace PresentationTools.Events.Aggregation.Selection
{
	public static class ListenerExtensions
	{
		public static EventSelector<T> Select<T>(this IListen<T> listener)
		{
			return new EventSelector<T>(listener);
		}

		public static IListen<T> Of<T>(this IListen<T> listener, EventHub eventHub)
		{
			eventHub.ThrowIfNull().Subscribe(listener);
			return listener;
		}
	}
}