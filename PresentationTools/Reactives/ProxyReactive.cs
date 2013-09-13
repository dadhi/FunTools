using System;
using System.ComponentModel;
using DryTools;
using PresentationTools.Events.Weak;

namespace PresentationTools.Reactives
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
			Ensure.NotNull(() => getValue, () => setValue);

			_getValue = getValue;
			_setValue = setValue;

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