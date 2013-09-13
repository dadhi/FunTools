﻿using System;
using System.Collections.Generic;
using System.Linq;
using DryTools;

namespace PresentationTools.Events.Weak
{
	public class WeakHandlerEvent<TEventHandler, TEvent>
		where TEventHandler : class
	{
		public WeakHandlerEvent(Func<TEventHandler, Action<object, TEvent>> convertFromEventHandler, Action<Action> strategy = null)
		{
			Ensure.NotNull(() => convertFromEventHandler);

			_convertFromEventHandler = convertFromEventHandler;

			_strategy = strategy ?? (a => a());
		}

		public void Subscribe(TEventHandler handler)
		{
			Ensure.NotNull(() => handler);

			lock (_weakHandlers)
			{
				_weakHandlers.RemoveAll(x => !x.IsAlive);
				_weakHandlers.Add(new WeakReference(handler));
			}
		}

		public void Unsubscribe(TEventHandler handler)
		{
			Ensure.NotNull(() => handler);

			if (HandlerCount == 0)
				return;

			lock (_weakHandlers)
				_weakHandlers.RemoveAll(x => x.Target == handler);
		}

		public void Raise(object sender, TEvent e)
		{
			if (HandlerCount == 0)
				return;

			TEventHandler[] handlers;
			lock (_weakHandlers)
				handlers = _weakHandlers.Select(x => x.Target as TEventHandler).Where(x => x != null).ToArray();

			if (handlers.Length != 0)
				handlers.ForEach(x => ExecuteAction(_convertFromEventHandler(x), sender, e));
		}

		public int HandlerCount { get { return _weakHandlers.Count; } }

		#region Implementation

		private readonly Func<TEventHandler, Action<object, TEvent>> _convertFromEventHandler;

		private readonly Action<Action> _strategy;

		private readonly List<WeakReference> _weakHandlers = new List<WeakReference>();

		private void ExecuteAction(Action<object, TEvent> actionHandler, object sender, TEvent e)
		{
			_strategy(() => actionHandler(sender, e));
		}

		#endregion
	}
}