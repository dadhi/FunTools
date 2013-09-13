using System;
using System.Collections.Generic;
using System.Linq;
using DryTools;

namespace PresentationTools.Events.Aggregation
{
	public class EventHub
	{
		public void Subscribe<THandle>(THandle handler, Action<Action> strategy = null)
			where THandle : class
		{
			Ensure.NotNull(() => handler);

			lock (_entriesLocker)
			{
				_entries.RemoveAll(x => !x.IsAlive);
				if (!_entries.Any(x => x.Matches(handler)))
					_entries.AddRange(GetHandlerEntries(handler, strategy));
			}
		}

		public void Unsubscribe<THandle>(THandle handler)
			where THandle : class
		{
			Ensure.NotNull(() => handler);
			lock (_entriesLocker)
				_entries.RemoveAll(x => !x.IsAlive || x.Matches(handler));
		}

		public void Publish(object e, Action<Action> strategy = null)
		{
			Entry[] entries;
			lock (_entriesLocker)
				entries = _entries.ToArray();

			entries.ForEach(x => x.TryHandle(e, strategy));
		}

		#region Configuration

		public static readonly Action<Action> InvokeSyncOnPublishingThread = a => a();

		public static readonly Func<Action<Action>, Action<Action>, Action<Action>, Action<Action>>
			UseDefaultOrHandlerOrPublisherStrategy = (defaults, handlers, publishers) => handlers ?? publishers ?? defaults;

		public Action<Action> DefaultStrategy = InvokeSyncOnPublishingThread;

		public Func<Action<Action>, Action<Action>, Action<Action>, Action<Action>>
			StrategyFormula = UseDefaultOrHandlerOrPublisherStrategy;

		#endregion

		#region Implemenation

		internal int Size { get { return _entries.Count; } }

		private readonly List<Entry> _entries = new List<Entry>();

		private readonly object _entriesLocker = new object();

		private readonly Type _genericHandlerType = typeof(IListen<>);

		private IEnumerable<Entry> GetHandlerEntries<THandle>(THandle handler, Action<Action> handlerStrategy)
			where THandle : class
		{
			var strategyFormula = GetStrategyFormula(handlerStrategy);
			return handler.GetType().GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == _genericHandlerType)
				.Select(x => new Entry(
					handler,
					x.GetGenericArguments()[0],
					x.GetMethods()[0].ToOpenHandlerOfOneArg<THandle>(),
					strategyFormula));
		}

		private Func<Action<Action>, Action<Action>> GetStrategyFormula(Action<Action> handlerStrategy)
		{
			var formula = StrategyFormula ?? UseDefaultOrHandlerOrPublisherStrategy;
			return publisherStrategy => formula(DefaultStrategy, handlerStrategy, publisherStrategy) ?? InvokeSyncOnPublishingThread;
		}

		private class Entry
		{
			public Entry(
				object handler,
				Type eventType,
				Action<object, object> handleMethod,
				Func<Action<Action>, Action<Action>> strategyFormula)
			{
				_handler = handler is ILifeCheck ? handler : new WeakReference(handler);
				_eventType = eventType;
				_handleMethod = handleMethod;
				_strategyFormula = strategyFormula;
			}

			public bool IsAlive
			{
				get { return _handler is ILifeCheck ? ((ILifeCheck)_handler).IsAlive : ((WeakReference)_handler).IsAlive; }
			}

			public bool Matches(object handler)
			{
				return RealHandler == handler;
			}

			public void TryHandle(object e, Action<Action> publisherStrategy = null)
			{
				var handler = RealHandler;
				if (handler == null)
					return;

				if (_eventType.IsAssignableFrom(e.GetType()))
					_strategyFormula(publisherStrategy).Invoke(() => _handleMethod(handler, e));
			}

			private readonly object _handler;

			private readonly Type _eventType;

			private readonly Action<object, object> _handleMethod;

			private readonly Func<Action<Action>, Action<Action>> _strategyFormula;

			private object RealHandler
			{
				get { return _handler is WeakReference ? ((WeakReference)_handler).Target : _handler; }
			}
		}

		#endregion
	}
}