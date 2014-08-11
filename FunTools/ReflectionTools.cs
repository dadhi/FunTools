using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FunTools
{
    public static class ReflectionTools
    {
        public static void RaisePropertyChanged(this INotifyPropertyChanged source, string propertyName)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            FieldInfo propertyChanged = null;
            for (var modelType = source.GetType(); modelType != null && propertyChanged == null; modelType = modelType.BaseType)
                propertyChanged = modelType.GetField("PropertyChanged",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            if (propertyChanged == null)
                throw new InvalidOperationException("Unable to find PropertyChanged event in model and its base types.");

            var handler = (MulticastDelegate)propertyChanged.GetValue(source);
            if (handler == null) // There are no subscribers to event yet.
                return;

            var subscribers = handler.GetInvocationList();
            var eventArgs = new object[] { source, new PropertyChangedEventArgs(propertyName) };
            for (var i = 0; i < subscribers.Length; i++)
                subscribers[i].Method.Invoke(subscribers[i].Target, eventArgs);
        }

        public static string GetPropertyName<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty>> propertySelector)
        {
            var expr = propertySelector.ToString();
            var exprParts = expr.Split(new[] { "=>" }, StringSplitOptions.None);
            if (exprParts.Length != 2)
                throw new InvalidOperationException("Unable to get property from expression: " + expr);
            var propertyAccessor = exprParts[1].Trim();
            var propertyAccessorParts = propertyAccessor.Split('.');
            if (propertyAccessorParts.Length < 2)
                throw new InvalidOperationException("Unable to get property from expression: " + expr);
            var propertyName = propertyAccessorParts.Last();
            return propertyName.Trim();
        }
    }
}