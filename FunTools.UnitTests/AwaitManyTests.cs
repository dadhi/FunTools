using System;
using System.Threading;
using NUnit.Framework;

namespace FunTools.UnitTests
{
	[TestFixture]
	public class AwaitManyTests
	{
		[Test]
		public void Simple_joinad_test()
		{
			var joinad = Await.Many(
				Timer(1000, () => true),
				Timer(100, () => false),
				0,
				(x, y) => Value.Of(
					y.IsNone ? 1 :
					x.IsNone ? 2 :
					3)
				);

			var result = joinad.WaitSuccess();
			Assert.AreEqual(2, result);
		}

		private static Await<T> Timer<T>(int milliseconds, Func<T> operation)
		{
			return complete =>
			{
				Timer timer = null;
				timer = new Timer(
					_ =>
					{
						if (timer != null) 
							timer.Dispose();
						complete(Try.Do(operation));
					},
					null,
					milliseconds,
					Timeout.Infinite);

				return timer.Dispose;
			};
		}
	}
}