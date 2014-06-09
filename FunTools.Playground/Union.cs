using System;
using NUnit.Framework;

namespace FunTools.Playground
{
    [TestFixture]
    public class UnionTests
    {
        [Test]
        public void Test()
        {
            var caseA = new Union<int, bool, string>.CaseA(25);

            var caseA2 = Union<int, bool, string>.Of(25);

        }

        public class ResultOrErrorOrCancel<TResult> : Union<TResult, Exception, Empty> 
        {
            public override T Match<T>(Func<TResult, T> a, Func<Exception, T> b, Func<Empty, T> c)
            {
                throw new NotImplementedException();
            }
        }
    }

    public abstract class Union<A, B, C>
    {
        public abstract T Match<T>(Func<A, T> a, Func<B, T> b, Func<C, T> c);

        public static Union<A, B, C> Of(A a)
        {
            return new CaseA(a);
        }

        public sealed class CaseA : Union<A, B, C>
        {
            public readonly A Item;

            public CaseA(A item)
            {
                Item = item;
            }

            public override T Match<T>(Func<A, T> a, Func<B, T> b, Func<C, T> c)
            {
                return a(Item);
            }
        }

        public sealed class CaseB : Union<A, B, C>
        {
            public readonly B Item;

            public CaseB(B item)
            {
                Item = item;
            }

            public override T Match<T>(Func<A, T> a, Func<B, T> b, Func<C, T> c)
            {
                return b(Item);
            }
        }

        public sealed class CaseC : Union<A, B, C>
        {
            public readonly C Item;
            public CaseC(C item) { Item = item; }
            public override T Match<T>(Func<A, T> a, Func<B, T> b, Func<C, T> c)
            {
                return c(Item);
            }
        }
    }
}
