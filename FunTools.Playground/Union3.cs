using System;

namespace FunTools.Playground
{
    public abstract class Union3<A, B, C>
    {
        public abstract T Match<T>(Func<A, T> f, Func<B, T> g, Func<C, T> h);

        public sealed class Case1 : Union3<A, B, C>
        {
            public readonly A Item;
            public Case1(A item) : base() { this.Item = item; }
            public override T Match<T>(Func<A, T> f, Func<B, T> g, Func<C, T> h)
            {
                return f(Item);
            }
        }

        public sealed class Case2 : Union3<A, B, C>
        {
            public readonly B Item;
            public Case2(B item) { this.Item = item; }
            public override T Match<T>(Func<A, T> f, Func<B, T> g, Func<C, T> h)
            {
                return g(Item);
            }
        }

        public sealed class Case3 : Union3<A, B, C>
        {
            public readonly C Item;
            public Case3(C item) { this.Item = item; }
            public override T Match<T>(Func<A, T> f, Func<B, T> g, Func<C, T> h)
            {
                return h(Item);
            }
        }
    }
}
