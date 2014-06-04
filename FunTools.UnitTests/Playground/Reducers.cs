using System;
using System.Collections.Generic;

namespace FunTools.UnitTests.Playground
{
    public static class Reducers
    {
        public delegate Func<IEnumerable<T>, R> Reduce<T, R>(R seed, Func<R, T, R> reduce);

        //public static Func<Reduce<X, R>, Reduce<X, R>> Mapping<X, Y, R>(Func<X, Y> map)
        //{
        //    return reduce => (seed, func) => xs =>
        //    {
        //        foreach (var x in xs)
        //        {
                    
        //        }
        //    };
        //}
    }

}