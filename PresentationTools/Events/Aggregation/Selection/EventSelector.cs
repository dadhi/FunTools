using System;
using DryTools;

namespace PresentationTools.Events.Aggregation.Selection
{
	public class EventSelector<T>
	{
		public EventSelector(IListen<T> handler)
		{
			_handler = Ensure.NotNull(() => handler);
		}

		public IListen<TSource> From<TSource>(Func<TSource, T> selector, Func<TSource, bool> condition = null)
		{
			Ensure.NotNull(() => selector);
			return new Listener<IListen<T>, TSource>(
				_handler,
				(x, e) =>
				{
					if (condition == null || condition(e))
						x.Listen(selector(e));
				});
		}

		public IListen<TSource> From<TSource>(Func<TSource, bool> condition = null)
			where TSource : T
		{
			return From(source => (T)source, condition);
		}

		#region Implementation

		private readonly IListen<T> _handler;

		#endregion
	}
}