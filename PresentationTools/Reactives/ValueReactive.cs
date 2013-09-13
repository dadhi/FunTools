namespace PresentationTools.Reactives
{
	internal class ValueReactive<T> : Reactive<T>
	{
		public override T Value
		{
			get { return _value; }
			set
			{
				if (NotifyAlways || !Equals(_value, value))
				{
					_value = value;
					NotifyValueChanged();
				}
			}
		}

		public void SetValue(T value)
		{
			Value = value;
		}

		public ValueReactive(T value, bool notifyAlways = false)
		{
			_value = value;
			NotifyAlways = notifyAlways;
		}

		public override void SetValueSilently(T value)
		{
			_value = value;
		}

		#region Implementation

		private T _value;

		#endregion
	}
}