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
			var result = Await.Many(
				Timer(1000),
				Timer(100),
				0,
				(x, y) => Some.Of(
					x.IsSome && y.IsNone ? 1 :
					x.IsNone && y.IsSome ? 2 :
					3)
				).WaitSuccess();

			Assert.AreEqual(2, result);
		}

		private static Await<Empty> Timer(int milliseconds)
		{
			return (Complete<Empty> complete) =>
			{
				TimerCallback timerCallback = _ => complete(Success.Of(Empty.Value));
				var timer = new Timer(timerCallback, null, milliseconds, Timeout.Infinite);
				return timer.Dispose;
			};
		}
	}
}