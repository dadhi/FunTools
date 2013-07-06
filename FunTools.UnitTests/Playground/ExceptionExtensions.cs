using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace FunTools.UnitTests.Playground
{
	public static class ExceptionExtensions
	{
		public static void PreserveStackTrace(this Exception exception)
		{
			var context = new StreamingContext(StreamingContextStates.CrossAppDomain);
			var serializationInfo = new SerializationInfo(typeof(Exception), new FormatterConverter());
			var constructor = typeof(Exception).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);

			exception.GetObjectData(serializationInfo, context);
			constructor.Invoke(exception, new object[] { serializationInfo, context });
		}
	}
}
