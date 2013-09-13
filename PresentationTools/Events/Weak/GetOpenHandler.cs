using System;
using System.Reflection;

namespace PresentationTools.Events.Weak
{
	public static class GetOpenHandler
	{
		public static Action<object, object, TEvent> FromEventHandler<TEvent>(MethodInfo method)
		{
			return (Action<object, object, TEvent>)
				_createFromHandler.MakeGenericMethod(method.DeclaringType, typeof(TEvent)).Invoke(null, new object[] { method });
		}

		public static Action<object, TEvent> FromEventAction<TEvent>(MethodInfo method)
		{
			return (Action<object, TEvent>)
				_createFromAction.MakeGenericMethod(method.DeclaringType, typeof(TEvent)).Invoke(null, new object[] { method });
		}

		#region Implementation

		private static readonly MethodInfo _createFromHandler =
			typeof(GetOpenHandler).GetMethod("CreateFromHandler", BindingFlags.Static | BindingFlags.NonPublic);

		private static readonly MethodInfo _createFromAction =
			typeof(GetOpenHandler).GetMethod("CreateFromAction", BindingFlags.Static | BindingFlags.NonPublic);

		internal static Action<object, object, TEvent> CreateFromHandler<TTarget, TEvent>(MethodInfo method)
		{
			return CreateFrom<Action<TTarget, object, TEvent>, Action<object, object, TEvent>>(
				method,
				h => (target, sender, e) => h((TTarget)target, sender, e));
		}

		internal static Action<object, TEvent> CreateFromAction<TTarget, TEvent>(MethodInfo method)
		{
			return CreateFrom<Action<TTarget, TEvent>, Action<object, TEvent>>(
				method,
				h => (target, e) => h((TTarget)target, e));
		}

		private static TWeakOpenHandler CreateFrom<THandler, TWeakOpenHandler>(
			MethodInfo method,
			Func<THandler, TWeakOpenHandler> convertToWeakTypedOpenHandler)
			where THandler : class
		{
			var openHandler = Delegate.CreateDelegate(typeof(THandler), null, method, true);
			if (openHandler == null)
				throw new InvalidOperationException("Open delegate should not be null");

			return convertToWeakTypedOpenHandler(openHandler as THandler);
		}

		#endregion
	}
}