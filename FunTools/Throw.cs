using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FunTools
{
    public class DryToolsException : InvalidOperationException
    {
        public DryToolsException(string message) : base(message) { }
    }

        public static class PrintTools
    {
        public static string ItemSeparatorStr = ";" + Environment.NewLine;

        public static StringBuilder Print(this StringBuilder str, object x)
        {
            return x == null ? str.Append("null")
                : x is string ? str.Print((string)x)
                : x is Type ? str.Print((Type)x)
                : x is IEnumerable<Type> || x is IEnumerable ? str.Print((IEnumerable)x, ItemSeparatorStr)
                : str.Append(x);
        }

        public static StringBuilder Print(this StringBuilder str, string s)
        {
            return str.Append(s);
        }

        public static StringBuilder Print(this StringBuilder str, IEnumerable items,
            string separator = ", ", Func<StringBuilder, object, StringBuilder> printItem = null)
        {
            if (items == null) return str;
            printItem = printItem ?? Print;
            var itemCount = 0;
            foreach (var item in items)
                printItem(itemCount++ == 0 ? str : str.Append(separator), item);
            return str;
        }

        public static Func<Type, string> GetDefaultTypeName = t => t.FullName ?? t.Name;

        public static StringBuilder Print(this StringBuilder str, Type type, Func<Type, string> getTypeName = null)
        {
            if (type == null) return str;

            getTypeName = getTypeName ?? GetDefaultTypeName;
            var typeName = getTypeName(type);

            var isArray = type.IsArray;
            if (isArray)
                type = type.GetElementType();

            if (!type.IsGenericType)
                return str.Append(typeName.Replace('+', '.'));

            str.Append(typeName.Substring(0, typeName.IndexOf('`')).Replace('+', '.')).Append('<');

            var genericArgs = type.GetGenericArguments();
            if (type.IsGenericTypeDefinition)
                str.Append(',', genericArgs.Length - 1);
            else
                str.Print(genericArgs, ", ", (s, t) => s.Print((Type)t, getTypeName));

            str.Append('>');

            if (isArray)
                str.Append("[]");

            return str;
        }
    }

    public static class Throw
    {
        public static Func<string, Exception> GetException = message => new DryToolsException(message);

        public static Func<object, string> PrintArg = x => new StringBuilder().Print(x).ToString();

        public static T ThrowIfNull<T>(this T arg, string message = null, object arg0 = null, object arg1 = null, object arg2 = null) where T : class
        {
            if (arg != null) return arg;
            throw GetException(message == null ? Format(ERROR_ARG_IS_NULL, typeof(T)) : Format(message, arg0, arg1, arg2));
        }

        public static T ThrowIf<T>(this T arg, bool throwCondition, string message = null, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            if (!throwCondition) return arg;
            throw GetException(message == null ? Format(ERROR_ARG_HAS_IMVALID_CONDITION, typeof(T)) : Format(message, arg0, arg1, arg2));
        }

        public static T ThrowIf<T>(this T arg, Func<T, Exception> getErrorOrNull)
        {
            var error = getErrorOrNull(arg);
            if (error == null) return arg;
            throw error;
        }

        public static void If(bool throwCondition, string message, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            if (!throwCondition) return;
            throw GetException(Format(message, arg0, arg1, arg2));
        }

        public static Exception Of(this string message, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            return GetException(Format(message, arg0, arg1, arg2));
        }

        public static string Format(this string message, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            return string.Format(message, PrintArg(arg0), PrintArg(arg1), PrintArg(arg2));
        }

        public static readonly string ERROR_ARG_IS_NULL = "Argument of type {0} is null.";
        public static readonly string ERROR_ARG_HAS_IMVALID_CONDITION = "Argument of type {0} has invalid condition.";
    }
}
