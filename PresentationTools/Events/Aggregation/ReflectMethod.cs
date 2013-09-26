using System;
using System.Reflection;
using FunTools;

namespace PresentationTools.Events.Aggregation
{
	public static class ReflectMethod
	{
		public static Action<object, object> ToOpenHandlerOfOneArg<TTarget>(this MethodInfo method)
			where TTarget : class
		{
		    var parameters = method.ThrowIfNull()
                .GetParameters().ThrowIf(ps => ps.Length != 1,  "Method should have only one parameter");

			var handler = (Action<TTarget, object>)_targetMethodOfOneArg
				.MakeGenericMethod(typeof(TTarget), parameters[0].ParameterType)
				.Invoke(null, new object[] { method });

			return (target, arg) => handler((TTarget)target, arg);
		}

		#region Implementation

		private static readonly MethodInfo _targetMethodOfOneArg =
			typeof(ReflectMethod).GetMethod("GetTargetMethodOfOneArg", BindingFlags.Static | BindingFlags.NonPublic);

		internal static Action<TTarget, object> GetTargetMethodOfOneArg<TTarget, TArg>(MethodInfo method)
		{
			var openAction = (Action<TTarget, TArg>)Delegate.CreateDelegate(typeof(Action<TTarget, TArg>), null, method);
			return (target, arg) => openAction(target, (TArg)arg);
		}

		#endregion
	}
}