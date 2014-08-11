using System;
using System.ComponentModel;
using FunTools.Weak;

namespace FunTools.Reactives
{
	internal class ProxyReactive<T> : Reactive<T>
	{
		public override T Value
		{
			get { return _getValue(); }
			set { _setValue(value); }
		}

		public ProxyReactive(Func<T> getValue, Action<T> setValue, params INotifyPropertyChanged[] notifiers)
		{
			_getValue = getValue.ThrowIfNull();
			_setValue = setValue.ThrowIfNull();

			notifiers.ForEach(x => x.SubscribeWeakly(this, (r, s, e) => r.NotifyPropertyChanged()));
		}

		public override void SetValueSilently(T value)
		{
			_shouldSetValueSilently = true;
			try
			{
				_setValue(value);
			}
			finally
			{
				_shouldSetValueSilently = false;
			}
		}

		#region Implementation

		private readonly Action<T> _setValue;

		private readonly Func<T> _getValue;

		private bool _shouldSetValueSilently;

		private void NotifyPropertyChanged()
		{
			if (!_shouldSetValueSilently)
				NotifyValueChanged();
		}

		#endregion
	}
}