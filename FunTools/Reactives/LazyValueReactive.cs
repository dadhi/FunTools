using System;

namespace FunTools.Reactives
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
				if (Equals(Value, value))
					return;
				SetValueSilently(value);
				NotifyValueChanged();
			}
		}

		public LazyValueReactive(Func<T> initializer)
		{
			_initializer = initializer.ThrowIfNull();
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