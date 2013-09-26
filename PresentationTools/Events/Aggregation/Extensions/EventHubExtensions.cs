using System;
using FunTools;
using PresentationTools.Events.Aggregation.Selection;

namespace PresentationTools.Events.Aggregation.Extensions
{
	public static class EventHubExtensions
	{
		public static IListen<TEvent> Subscribe<TEvent>(this EventHub hub, Action<TEvent> handler, Action<Action> strategy = null)
		{
			return new Handler<TEvent>(handler).Of(hub);
		}

		#region Implementation

		private class Handler<TEvent> : IListen<TEvent>
		{
			private readonly Action<TEvent> _handler;

			public Handler(Action<TEvent> handler)
			{
				_handler = handler.ThrowIfNull();
			}

			public void Listen(TEvent e)
			{
				_handler(e);
			}
		}

		#endregion
	}
}