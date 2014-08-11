using System;
using System.Collections.Generic;

namespace FunTools
{
    public static class Sugar
    {
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
    }
}