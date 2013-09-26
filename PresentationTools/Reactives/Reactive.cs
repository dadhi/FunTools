using System;
using System.ComponentModel;
using System.Linq;
using FunTools;
using PresentationTools.Events.Weak;

namespace PresentationTools.Reactives
{
	public static class Reactive
	{
		public const string SET_IS_NOT_SUPPORTED = "Set is not supported";

		public static ReactiveCommand Command<T>(Action<T> execute, Func<T, bool> canExecute, params INotifyPropertyChanged[] canExecuteNotifiers)
		{
			return new ReactiveCommand(x => execute((T)x), x => canExecute((T)x), canExecuteNotifiers);
		}

		public static ReactiveCommand Command<T>(Action<T> execute)
		{
			return Command(execute, _ => true);
		}

		public static ReactiveCommand Command(Action execute, Reactive<bool> canExecute)
		{
			return Command<object>(_ => execute(), _ => canExecute, canExecute);
		}

		public static ReactiveCommand Command(Action execute)
		{
			return Command<object>(_ => execute(), _ => true);
		}

		public static ReactiveCommand Named(this ReactiveCommand command, string name)
		{
			command.Name = name;
			return command;
		}

		public static Reactive<T> Of<T>(bool notifyAlways = false)
		{
			return new ValueReactive<T>(default(T), notifyAlways);
		}

		public static Reactive<T> Of<T>(T value, bool notifyAlways = false)
		{
			return new ValueReactive<T>(value, notifyAlways);
		}

		public static Reactive<T> Of<T>(Func<T> getValue, Action<T> setValue, params INotifyPropertyChanged[] notifiers)
		{
			return new ProxyReactive<T>(getValue, setValue, notifiers);
		}

		public static Reactive<T> Of<T>(Func<T> getValue, params INotifyPropertyChanged[] notifiers)
		{
			return Of(
				getValue,
				_ => { throw new NotSupportedException(SET_IS_NOT_SUPPORTED); },
				notifiers);
		}

		public static Reactive<T> OfLazy<T>(Func<T> initializer, bool notifyAlways = false)
		{
			return new LazyValueReactive<T>(initializer, notifyAlways);
		}

		/// <summary>
		/// Will create Reactive of new type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="source"></param>
		/// <param name="getValue"></param>
		/// <param name="setValue"></param>
		/// <param name="additionalNotifiers"></param>
		/// <returns></returns>
		public static Reactive<TResult> To<T, TResult>(
			this Reactive<T> source,
			Func<Reactive<T>, TResult> getValue,
			Action<Reactive<T>, TResult> setValue,
			params INotifyPropertyChanged[] additionalNotifiers)
		{
			var sources = new INotifyPropertyChanged[] { source };
			if (additionalNotifiers.Length != 0)
				sources = sources.Concat(additionalNotifiers).ToArray();

			return Of(
				() => getValue(source),
				value => setValue(source, value),
				sources);
		}

		public static Reactive<T> Get<T>(
			this Reactive<T> source,
			Func<Reactive<T>, T> getValue,
			params INotifyPropertyChanged[] additionalNotifiers)
		{
			return source.To(getValue, (x, value) => x.Value = value, additionalNotifiers);
		}

		public static Reactive<T> Set<T>(this Reactive<T> source, Action<Reactive<T>, T> setValue)
		{
			return source.To(x => x.Value, setValue);
		}

		public static Reactive<T> Set<T>(this Reactive<T> source, Func<T, T> setValue)
		{
			return source.Set((x, value) => x.Value = setValue(value));
		}

        //public static Reactive<T> OnChange<T>(this Reactive<T> source, Action<T> onChanged)
        //{
        //    source.SubscribeWeakly(x => x.Value, onChanged);
        //    return source;
        //}

		/// <summary>
		/// Will create Reactive of new type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="source"></param>
		/// <param name="getValue"></param>
		/// <param name="setValue"></param>
		/// <param name="additionalNotifiers"></param>
		/// <returns></returns>
		public static Reactive<TResult> To<T, TResult>(
			this Reactive<T> source,
			Func<T, TResult> getValue,
			Func<TResult, T> setValue,
			params INotifyPropertyChanged[] additionalNotifiers)
		{
			return source.To(
				x => getValue(x.Value),
				(x, value) => x.Value = setValue(value),
				additionalNotifiers);
		}

		/// <summary>
		/// Will create read-only Reactive of new type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="source"></param>
		/// <param name="getValue"></param>
		/// <param name="additionalNotifiers"></param>
		/// <returns></returns>
		public static Reactive<TResult> To<T, TResult>(
			this Reactive<T> source,
			Func<T, TResult> getValue,
			params INotifyPropertyChanged[] additionalNotifiers)
		{
			return source.To(
				getValue,
				_ => { throw new NotSupportedException(SET_IS_NOT_SUPPORTED); },
				additionalNotifiers);
		}

		/// <summary>
		/// Creates new reactive which subscribes to provided changes sources and will raise PropertyChanged when any source is changed.
		/// </summary>
		/// <typeparam name="T">Type of reactive value.</typeparam>
		/// <param name="source">Subscribing reactive.</param>
		/// <param name="additionalNotifiers">Notifiers to subscribe to.</param>
		/// <returns></returns>
		public static Reactive<T> NotifiedBy<T>(this Reactive<T> source, params INotifyPropertyChanged[] additionalNotifiers)
		{
			return source.To(
				x => x.Value,
				(x, value) => x.Value = value,
				additionalNotifiers);
		}

		/// <summary>Creates new reactive implementing <see cref="IDataErrorInfo"/> interface.</summary>
		/// <typeparam name="T">Type of reactive value.</typeparam>
		/// <param name="source">Reactive to validate.</param>
		/// <param name="validate">When value is valid function should return null, otherwise it should return validation error string.</param>
		/// <returns></returns>
		public static ValidatingReactive<T> Validate<T>(this Reactive<T> source, Func<T, string> validate)
		{
			return source is ValidatingReactive<T>
					? ((ValidatingReactive<T>)source).AddRule(validate)
					: new ValidatingReactive<T>(source, validate);
		}

		public static ValidatingReactive<T> Validate<T>(this Reactive<T> source, Func<T, bool> validate, Func<T, string> getErrorMessage)
		{
			validate.ThrowIfNull();
		    getErrorMessage.ThrowIfNull();

			return source.Validate(x => validate(x) ? null : getErrorMessage(x).ThrowIfNull());
		}

		public static ValidatingReactive<T> Validate<T>(this Reactive<T> source, Func<T, bool> validate, string errorMessage)
		{
			errorMessage.ThrowIfNull();
			return source.Validate(validate, _ => errorMessage);
		}

		public static Reactive<bool> IsValid<T>(this Reactive<T> source)
		{
			if (source is ValidatingReactive<T>)
				return ((ValidatingReactive<T>)source).IsValid;

			return Of(true);
		}

		public static Reactive<string> ValidationError<T>(this Reactive<T> source)
		{
			if (source is ValidatingReactive<T>)
				return ((ValidatingReactive<T>)source).ValidationError;

			return Of((string)null);
		}

        //public static Reactive<TProperty> ToReactive<TModel, TProperty>(
        //    this TModel model,
        //    Func<TModel, TProperty> property)
        //    where TModel : class, INotifyPropertyChanged
        //{
        //    var reactive = new ValueReactive<TProperty>(property.ThrowIfNull().Invoke(model.ThrowIfNull()));
        //    model.SubscribeWeakly(property, reactive.SetValue);
        //    return reactive;
        //}

		public static Reactive<T> Where<T>(this Reactive<T> source, Func<T, bool> condition)
		{
			return source.Set(
				(r, value) =>
				{
					if (condition(value))
						r.Value = value;
				});
		}
	}
}