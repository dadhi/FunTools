namespace FunTools.Reactives
{
	internal class ValueReactive<T> : Reactive<T>
	{
		public override T Value
		{
			get { return _value; }
			set
			{
				if (Equals(_value, value)) 
					return;
				_value = value;
				NotifyValueChanged();
			}
		}

		public void SetValue(T value)
		{
			Value = value;
		}

		public ValueReactive(T value)
		{
			_value = value;
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