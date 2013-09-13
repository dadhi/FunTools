using System;
using System.ComponentModel;

namespace FunTools.Changed
{
	public sealed class NotifyChange<T> : INotifyPropertyChanged, IDataErrorInfo
	{
		public NotifyChange(
			Func<T> getter,
			Action<T> setter = null,
			Func<T, bool> changeCondition = null,
			INotifyPropertyChanged[] changeSources = null,
			Func<object, PropertyChangedEventArgs, bool> sourceNotifyCondition = null)
		{
			_getter = getter.ThrowIfNull();
			_setter = setter ?? NotifyChange.Defaults.SetterIsNotSupported;

			_changeCondition = changeCondition ?? NotifyChange.Defaults.ChangeAlways;

			_sourceNotifyCondition = sourceNotifyCondition ?? NotifyChange.Defaults.SourceAlwaysNotifies;

			if (changeSources != null && changeSources.Length != 0)
				SubscribeToChangeSourcesWeakly(changeSources);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public Func<T, string> Validate = NotifyChange.Defaults.IsValid;

		public T Value
		{
			get { return _getter(); }
			set
			{
				if (!_changeCondition(value))
					return;

				_setter(value);
				NotifyChanged();
			}
		}

		public string Error
		{
			get { return Validate(Value); }
		}

		public string this[string ignored]
		{
			get { return Error; }
		}

		public void NotifyChanged()
		{
			var evt = PropertyChanged;
			if (evt != null)
				evt(this, NotifyChange.Defaults.PropertyChangedEventArgs);
		}

		public override string ToString()
		{
			object value = Value;
			return value == null ? "Value: Null" : value.ToString();
		}

		#region Implementation

		private readonly Func<T> _getter;

		private readonly Action<T> _setter;

		private readonly Func<T, bool> _changeCondition;

		private readonly Func<object, PropertyChangedEventArgs, bool> _sourceNotifyCondition;

		private void SubscribeToChangeSourcesWeakly(INotifyPropertyChanged[] sources)
		{
			var selfWeakRef = new WeakReference(this);
			for (var i = 0; i < sources.Length; i++)
			{
				var source = sources[i];

				PropertyChangedEventHandler onSourceChanged = null;
				onSourceChanged = (sender, args) =>
				{
					if (selfWeakRef == null)
						return;

					var self = selfWeakRef.Target as NotifyChange<T>;
					if (self == null)
					{
						source.PropertyChanged -= onSourceChanged;
						selfWeakRef = null;
						return;
					}

					if (self._sourceNotifyCondition(sender, args))
						self.NotifyChanged();
				};

				source.PropertyChanged += onSourceChanged;
			}
		}

		#endregion
	}

	public static class NotifyChange
	{
		public static NotifyChange<T> Of<T>(
			Func<T> getter,
			Action<T> setter = null,
			Func<T, bool> changeCondition = null,
			INotifyPropertyChanged[] changeSources = null,
			Func<object, PropertyChangedEventArgs, bool> sourceNotifyCondition = null)
		{
			return new NotifyChange<T>(getter, setter, changeCondition, changeSources, sourceNotifyCondition);
		}

		public static NotifyChange<T> Of<T>(
			T initialValue = default(T),
			Func<T, bool> changeCondition = null)
		{
			var value = new ValueWrapper<T>(initialValue);
			return Of(value.Get, value.Set, changeCondition);
		}

		public static NotifyChange<T> Of<T>(
			Func<T, bool> changeCondition)
		{
			return Of(default(T), changeCondition);
		}

		public static NotifyChange<T> Select<T>(
			Func<T> selector,
			params INotifyPropertyChanged[] changeSources)
		{
			return new NotifyChange<T>(selector, changeSources: changeSources);
		}

		public static NotifyChange<T> Select<T>(
			Func<T> selector,
			Func<object, PropertyChangedEventArgs, bool> sourceNotifyCondition,
			params INotifyPropertyChanged[] notifiers)
		{
			return new NotifyChange<T>(selector, changeSources: notifiers, sourceNotifyCondition: sourceNotifyCondition);
		}

		public static NotifyChange<TResult> Select<T, TResult>(
			this NotifyChange<T> source,
			Func<T, TResult> selector)
		{
			return Select(() => selector(source.Value), source);
		}

		public static NotifyChange<TProperty> SelectNotifyChange<TModel, TProperty>(
			this TModel model,
			Func<TModel, TProperty> getProperty,
			Func<TProperty, bool> modelNotifyCondition = null)
			where TModel : class, INotifyPropertyChanged
		{
		    model.ThrowIfNull();
		    getProperty.ThrowIfNull();

			modelNotifyCondition = modelNotifyCondition ?? (_ => true);

			var propertyName = ExtractName.From(getProperty);

			return Select(
				() => getProperty(model),
				(_, e) => IsTargetProperty(e, propertyName) && modelNotifyCondition(getProperty(model)),
				model);
		}

		public static NotifyChange<T> ValidateThat<T>(
			this NotifyChange<T> source,
			Func<T, bool> isValid,
			string error = null)
		{
		    isValid.ThrowIfNull();
			source.Validate = x => isValid(x) ? null : (error ?? Defaults.ValidationErrorMessage);
			return source;
		}

		public static class Defaults
		{
			public static readonly PropertyChangedEventArgs PropertyChangedEventArgs = new PropertyChangedEventArgs(string.Empty);

			public static void SetterIsNotSupported<T>(T _)
			{
				throw new NotSupportedException("Setter is not provided, therefore value assignment is not supported");
			}

			public static bool ChangeAlways<T>(T _)
			{
				return true;
			}

			public static bool SourceAlwaysNotifies(object s, PropertyChangedEventArgs e)
			{
				return true;
			}

			public static string IsValid<T>(T _)
			{
				return null;
			}

			public static string ValidationErrorMessage = "Invalid value!";
		}

		#region Implementation

		private static bool IsTargetProperty(PropertyChangedEventArgs e, string targetPropertyName)
		{
			return e == null
				|| string.IsNullOrEmpty(e.PropertyName)
				|| string.Equals(e.PropertyName, targetPropertyName);
		}

		private class ValueWrapper<T>
		{
			public ValueWrapper(T initialValue)
			{
				_value = initialValue;
			}

			public T Get()
			{
				return _value;
			}

			public void Set(T value)
			{
				_value = value;
			}

			private T _value;
		}

		#endregion
	}
}