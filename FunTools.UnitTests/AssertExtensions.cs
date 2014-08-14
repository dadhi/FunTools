using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FunTools.UnitTests
{
    public static class AssertExtensions
    {
        public static T Should<T>(this T it)
        {
            return it;
        }

        public static T And<T>(this T it)
        {
            return it;
        }

        public static T BeSameAs<T>(this T it, T expected)
        {
            Assert.AreSame(expected, it);
            return it;
        }

        public static T Be<T>(this T it, T expected)
        {
            Assert.AreEqual(expected, it);
            return it;
        }

        public static T NotBe<T>(this T it, T expected)
        {
            Assert.AreNotEqual(expected, it);
            return it;
        }

        public static void BeTrue(this bool it)
        {
            Assert.IsTrue(it);
        }

        public static void BeFalse(this bool it)
        {
            Assert.IsFalse(it);
        }

        public static T BeNull<T>(this T it)
        {
            Assert.IsNull(it);
            return it;
        }

        public static T NotBeNull<T>(this T it)
        {
            Assert.NotNull(it);
            return it;
        }

        public static object BeOfType<T>(this object it)
        {
            Assert.IsInstanceOf<T>(it);
            return it;
        }

        public static T BeLessOrEqualTo<T>(this T it, T expected)
        {
            Assert.That(it, Is.LessThanOrEqualTo(expected));
            return it;
        }

        public static T BeLessThan<T>(this T it, T expected)
        {
            Assert.That(it, Is.LessThan(expected));
            return it;
        }

        public static T BeGreaterThan<T>(this T it, T expected)
        {
            Assert.That(it, Is.GreaterThan(expected));
            return it;
        }

        public static T BeGreaterOrEqualTo<T>(this T it, T expected)
        {
            Assert.That(it, Is.GreaterThanOrEqualTo(expected));
            return it;
        }

        public static string BeEmpty(this string it)
        {
            Assert.That(it, Is.Empty);
            return it;
        }

        public static string Contain(this string it, string fragment)
        {
            Assert.That(it, Is.StringContaining(fragment));
            return it;
        }

        public static string StartWith(this string it, string fragment)
        {
            Assert.That(it, Is.StringStarting(fragment));
            return it;
        }

        public static IEnumerable<T> ContainInOrder<T>(this IEnumerable<T> it, IEnumerable<T> subset)
        {
            CollectionAssert.IsSubsetOf(subset, it);
            return it;
        }

        public static IEnumerable<T> Equal<T>(this IEnumerable<T> it, IEnumerable<T> expected)
        {
            CollectionAssert.AreEqual(expected, it);
            return it;
        }

        public static IEnumerable<T> HaveCount<T>(this IEnumerable<T> it, int count)
        {
            it.Count().Be(count);
            return it;
        }

        public static T Where<T>(this T ex, Func<T, bool> condition) where T : Exception
        {
            Assert.IsTrue(condition(ex));
            return ex;
        }

        public static T WithMessage<T>(this T ex, string message) where T : Exception
        {
            ex.Message.Be(message);
            return ex;
        }
    }
}