using System;

namespace FunTools
{
    public static class Throw
    {
        public static Func<string, Exception> GetException = message => new InvalidOperationException(message);

        public static T ThrowIfNull<T>(this T arg, string message = null, object arg0 = null, object arg1 = null, object arg2 = null) where T : class
        {
            if (arg != null) return arg;
            throw GetException(message == null ? Format(ARG_IS_NULL, typeof(T)) : Format(message, arg0, arg1, arg2));
        }

        public static T ThrowIf<T>(this T arg, bool throwCondition, string message = null, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            if (!throwCondition) return arg;
            throw GetException(message == null ? Format(ARG_HAS_IMVALID_CONDITION, typeof(T)) : Format(message, arg0, arg1, arg2));
        }

        public static void If(bool throwCondition, string message, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            if (!throwCondition) return;
            throw GetException(Format(message, arg0, arg1, arg2));
        }

        #region Implementation

        private static string Format(this string message, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            return string.Format(message, Print(arg0), Print(arg1), Print(arg2));
        }

        private static string Print(object obj)
        {
            return obj == null ? null : obj is Type ? ((Type)obj).Print() : obj.ToString();
        }

        private static readonly string ARG_IS_NULL = "Argument of type {0} is null.";
        private static readonly string ARG_HAS_IMVALID_CONDITION = "Argument of type {0} has invalid condition.";

        #endregion
    }
}
