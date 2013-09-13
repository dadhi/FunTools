using System;
using DryTools;

namespace PresentationTools.Reactives
{
	internal class LazyValueReactive<T> : Reactive<T>
	{
		public override T Value
		{
			get
			{
				if (!_initialized)
					lock (_initializationLocker)
						if (!_initialized)
						{
							_value = _initializer();
							_initialized = true;
						}

				return _value;
			}
			set
			{
				if (NotifyAlways || !Equals(Value, value))
				{
					SetValueSilently(value);
					NotifyValueChanged();
				}
			}
		}

		public LazyValueReactive(Func<T> initializer, bool notifyAlways = false)
		{
			Ensure.NotNull(() => initializer);
			_initializer = initializer;
			NotifyAlways = notifyAlways;
		}

		public override void SetValueSilently(T value)
		{
			if (_initialized)
				_value = value;
			else
				lock (_initializationLocker)
				{
					_value = value;
					_initialized = true;
				}
		}

		#region Implementation

		private readonly Func<T> _initializer;

		private readonly object _initializationLocker = new object();

		private volatile bool _initialized;

		private T _value;

		#endregion
	}
}