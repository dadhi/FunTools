using System.ComponentModel;

namespace FunTools.Reactives
{
	public abstract class Reactive<T> : INotifyPropertyChanged
	{
		/// <summary>
		/// PropertyName is empty string that means that there is only one possible property (Value) to change.
		/// </summary>
		// ReSharper disable StaticFieldInGenericType
		public static readonly PropertyChangedEventArgs ValueChanged = new PropertyChangedEventArgs(string.Empty);
		// ReSharper restore StaticFieldInGenericType

		public static implicit operator T(Reactive<T> reactive)
		{
			return reactive == null ? default(T) : reactive.Value;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public abstract T Value { get; set; }

		public abstract void SetValueSilently(T value);

		public void NotifyValueChanged()
		{
			var handlers = PropertyChanged;
			if (handlers != null)
				handlers(this, ValueChanged);
		}

		public override string ToString()
		{
			object valueObject = Value;
			return valueObject == null ? string.Empty : valueObject.ToString();
		}

		public override int GetHashCode()
		{
			object valueObject = Value;
			return valueObject == null ? 0 : valueObject.GetHashCode();
		}

		public override bool Equals(object target)
		{
			if (target is Reactive<T>)
				return Equals(Value, ((Reactive<T>)target).Value);

			return false;
		}
	}
}