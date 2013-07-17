using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace FunTools.UnitTests.Playground
{
	[TestFixture]
	public static class PropertyTests
	{
		[Test]
		[Ignore]
		public static void Main()
		{
			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			const int count = 100 * 1000;

			var sw = Stopwatch.StartNew();
			for (var i = 0; i < count; i++)
			{
				var p = Property.FromExpression((Stopwatch _) => _.Elapsed);
				GC.KeepAlive(p);
			}

			sw.Stop();
			var exprTime = sw.ElapsedMilliseconds;
			sw.Reset();
			sw.Start();

			for (var i = 0; i < count; i++)
			{
				var p = Property.FromExpressionCached<Stopwatch>(() => _ => _.Elapsed);
				GC.KeepAlive(p);
			}

			sw.Stop();
			var cachedExprTime = sw.ElapsedMilliseconds;
			Assert.GreaterOrEqual(exprTime, cachedExprTime * 100);
		}
	}

	public static class Property
	{
		public static PropertyInfo FromExpression<T>(
			Expression<Func<T, object>> propertyExpression)
		{
			var body = propertyExpression.Body; 

			if (body.NodeType == ExpressionType.Convert &&
				body.Type == typeof(object))
				body = ((UnaryExpression)body).Operand;

			var memberExpr = body as MemberExpression;
			if (memberExpr == null)
				throw new ArgumentException("MemberExpression expected");

			if (memberExpr.Member.MemberType != MemberTypes.Property)
				throw new ArgumentException("Property member expected");

			var propInfo = (PropertyInfo)memberExpr.Member;
			return propInfo;
		}

		public static PropertyInfo FromExpressionCached<T>(
			Func<Expression<Func<T, object>>> propertyExpression)
		{
			var data = propertyExpression.Target as CachedData;
			return data != null ? data.CachedValue : FromImpl(propertyExpression);
		}

		private static PropertyInfo FromImpl<T>(
			Func<Expression<Func<T, object>>> propertyExpression)
		{
			// если у делегата нет замыкания,
			// то и у вложенного в него дерева выражения не должно быть
			if (propertyExpression.Target != null)
				throw new ArgumentException("Delegate should not have any closures.");
			if (!propertyExpression.Method.IsStatic)
				throw new ArgumentException("Delegate should be static.");

			var body = propertyExpression().Body; // вызываем таки делегат

			// из-за object у нас может быть тут лишний боксинг
			if (body.NodeType == ExpressionType.Convert &&
				body.Type == typeof(object))
			{
				body = ((UnaryExpression)body).Operand;
			}

			var memberExpr = body as MemberExpression;
			if (memberExpr == null)
				throw new ArgumentException("MemberExpression expected");

			if (memberExpr.Member.MemberType != MemberTypes.Property)
				throw new ArgumentException("Property member expected");

			var propInfo = (PropertyInfo)memberExpr.Member;

			// раз делегат у нас статический, то он должен быть закэширован
			// компилятором в статическом поле типа, в котором он определён
			var declaringType = propertyExpression.Method.DeclaringType;
			foreach (var fieldInfo in declaringType
				.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
			{
				// проходимся по всем статическим полям в поисках делегата
				if (ReferenceEquals(fieldInfo.GetValue(null), propertyExpression))
				{
					// нашёлся - создаём специальный holder для PropertyInfo
					var cached = new CachedData { CachedValue = propInfo };
					// заменяем делегат в поле на делегат на stub-метод
					var stub = new Func<Expression<Func<T, object>>>(cached.Stub<T>);
					fieldInfo.SetValue(null, stub);
					return propInfo;
				}
			}

			throw new InvalidOperationException("Delegate is not cached.");
		}

		// аналог closure-класса, хранящий закэшированное значение
		private sealed class CachedData
		{
			public PropertyInfo CachedValue { get; set; }

			public Expression<Func<T, object>> Stub<T>()
			{
				throw new InvalidOperationException("Should never be called");
			}
		}
	}
}
