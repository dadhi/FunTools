using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTools.MsUnit;
using System.Linq;

namespace PresentationTools.UnitTests.POC
{
	[TestClass]
	public class RuntimeCastTests
	{
		[TestMethod]
		public void Should_be_able_to_cast_at_runtime()
		{
			// Arrange
			SomeEvent handledEvent = null;
			var listener = new Listener { OnSomeEvent = e => handledEvent = e };

			// Act
			var publishedEvent = new SomeEventSubtype();
			Handle(listener, publishedEvent);

			// Assert
			handledEvent.ShouldNotBeNull();
			handledEvent.ShouldBeOfType<SomeEventSubtype>();
		}

		private static void Handle<TListener, TEvent>(TListener listener, TEvent e)
			where TListener : class
		{
			var concreteListenerType = typeof(TListener);
			var concreteEventType = typeof(TEvent);
			var genericListenerType = typeof(IListener<>);

			var listenerHandlers = concreteListenerType.GetInterfaces()
				.Where(x => x.GetGenericTypeDefinition() == genericListenerType)
				.ToDictionary(
					x => x.GetGenericArguments()[0],
					x => x.GetMethods()[0].ToActionOfOneArgument(listener));

			foreach (var handler in listenerHandlers)
			{
				if (handler.Key.IsAssignableFrom(concreteEventType))
					handler.Value(e);
			}
		}

		interface IListener<TEvent>
		{
			void Listen(TEvent e);
		}

		internal class Listener : IListener<SomeEvent>
		{
			public Action<SomeEvent> OnSomeEvent { get; set; }

			public void Listen(SomeEvent e)
			{
				OnSomeEvent(e);
			}
		}

		internal class SomeEvent
		{
		}

		internal class SomeEventSubtype : SomeEvent
		{
		}
	}

	public static class MethodInfoToDelegateConverter
	{
		public static Action<object> ToActionOfOneArgument<TTarget>(this MethodInfo method, TTarget instance)
			where TTarget : class
		{
			var openAction = method.ToOpenActionOfOneArgument<TTarget>();
			return arg => openAction(instance, arg);
		}

		public static Action<TTarget, object> ToOpenActionOfOneArgument<TTarget>(this MethodInfo method)
			where TTarget : class
		{
			if (!method.GetParameters().Any())
				throw new ArgumentException("Method should take at least one argument", "method");

			var parameterType = method.GetParameters()[0].ParameterType;

			var converter = _genericConverterMethod.MakeGenericMethod(typeof(TTarget), parameterType);

			var action = converter.Invoke(null, new object[] { method });

			return (Action<TTarget, object>)action;
		}

		#region Implementation

		private static readonly MethodInfo _genericConverterMethod =
			typeof(MethodInfoToDelegateConverter).GetMethod("ConvertToActionOfOneArgument", BindingFlags.Static | BindingFlags.NonPublic);

		internal static Action<TTarget, object> ConvertToActionOfOneArgument<TTarget, TArg>(MethodInfo method)
		{
			var action = (Action<TTarget, TArg>)Delegate.CreateDelegate(typeof(Action<TTarget, TArg>), method);
			return (target, arg) => action(target, (TArg)arg);
		}

		#endregion
	}
}