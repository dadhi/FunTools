using System;
using System.Collections.Generic;
using System.Linq;

namespace FunTools
{
	public static class Sugar
	{
		public static string Of(this string format, params object[] args)
		{
			return string.Format(format, args.Select(x => x is Type ? ((Type)x).Print() : x).ToArray());
		}

		public static T Apply<T>(this T source, Action<T> action)
		{
			action(source);
			return source;
		}

		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var x in source)
				action(x);
			return source;
		}

		public static IList<T> ForEach<T>(this IList<T> source, Action<T> action)
		{
			var count = source.Count;
			for (var i = 0; i < count; i++)
				action(source[i]);
			return source;
		}

		public static string Print(this Type type, Func<Type, string> what = null /* prints Type.FullName by default */)
		{
			var name = what == null ? type.FullName : what(type);
			if (type.IsGenericType) // for generic types
			{
				var genericArgs = type.GetGenericArguments();
				var genericArgsString = type.IsGenericTypeDefinition
					? new string(',', genericArgs.Length - 1)
					: String.Join(", ", genericArgs.Select(x => x.Print(what)).ToArray());
				name = name.Substring(0, name.LastIndexOf('`')) + "<" + genericArgsString + ">";
			}
			return name.Replace('+', '.'); // for nested classes
		}
	}
}