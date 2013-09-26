using System;
using System.ComponentModel;
using FunTools;

namespace PresentationTools.Reactives
{
	public class ValidatingReactive<T> : Reactive<T>, IDataErrorInfo
	{
		public ValidatingReactive(Reactive<T> source, Func<T, string> validate)
		{
			_source = source.ThrowIfNull();
			_validate = validate.ThrowIfNull();

			IsValid = this.To(_ => Error == null);
			ValidationError = this.To(_ => Error);

			_source.PropertyChanged += (sender, args) => NotifyValueChanged();
		}

		public override T Value
		{
			get { return _source.Value; }
			set { _source.Value = value; }
		}

		public string Error
		{
			get { return _validate(Value); }
		}

		public string this[string columnName]
		{
			get { return Error; }
		}

		public Reactive<bool> IsValid { get; private set; }

		public Reactive<string> ValidationError { get; private set; }

		public override void SetValueSilently(T value)
		{
			_source.SetValueSilently(value);
		}

		#region Implementation

		private readonly Reactive<T> _source;

		private readonly Func<T, string> _validate;

		internal ValidatingReactive<T> AddRule(Func<T, string> validate)
		{
			return new ValidatingReactive<T>(_source, x => _validate(x) ?? validate(x));
		}

		#endregion
	}
}
