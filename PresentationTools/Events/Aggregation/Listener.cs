using System;
using DryTools;

namespace PresentationTools.Events.Aggregation
{
	public class Listener<TTarget, TEvent> : IListen<TEvent>, ILifeCheck
		where TTarget : class
	{
		public Listener(TTarget target, Action<TTarget, TEvent> handler)
		{
			Ensure.NotNull(() => handler, () => target);
			_weakTarget = new WeakReference(target);
			_handler = handler;
		}

		public void Listen(TEvent e)
		{
			if (_weakTarget == null)
				return;

			var target = _weakTarget.Target as TTarget;
			if (target == null)
			{
				_weakTarget = null;
				_handler = null;
				return;
			}

			_handler(target, e);
		}

		public bool IsAlive
		{
			get { return _weakTarget.IsAlive; }
		}

		#region Implementation

		private Action<TTarget, TEvent> _handler;

		private WeakReference _weakTarget;

		#endregion
	}
}