using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using DryTools;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools;

namespace PresentationTools.UnitTests.POC
{
	[TestClass]
	public class DifferentPropertyChangedNotificationApproaches : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName)
		{
			var evt = PropertyChanged;
			if (evt != null)
				evt(this, new PropertyChangedEventArgs(propertyName));
		}

		private string _somePropertyWithMethodBase;
		public string SomePropertyWithMethodBase
		{
			get { return _somePropertyWithMethodBase; }
			set
			{
				_somePropertyWithMethodBase = value;
				NotifyPropertyChanged(MethodBase.GetCurrentMethod().Name.Substring(4));
			}
		}

		private string _somePropertyWithAnonymousType;
		public string SomePropertyWithAnonymousType
		{
			get { return _somePropertyWithAnonymousType; }
			set
			{
				_somePropertyWithAnonymousType = value;
				NotifyPropertyChanged(FromAnonymType(new { SomePropertyWithAnonymousType }));
			}
		}

		private string _somePropertyWithName;
		public string SomePropertyWithName
		{
			get { return _somePropertyWithName; }
			set
			{
				_somePropertyWithName = value;
				NotifyPropertyChanged("SomePropertyWithName");
			}
		}

		private string _somePropertyWithLambda;
		public string SomePropertyWithLambda
		{
			get { return _somePropertyWithLambda; }
			set
			{
				_somePropertyWithLambda = value;
				NotifyPropertyChanged(ExtractName.From(() => SomePropertyWithLambda));
			}
		}

		private static TimeSpan NotifyTimingStep(Action action, int max, ref int count)
		{
			Debug.Assert(action != null);
			var sw = Stopwatch.StartNew();
			for (var i = 0; i < max; i++)
				action();
			var result = sw.Elapsed;
			sw.Stop();
			count++;
			return result;
		}

		/// <summary>
		/// We can't use this approach in Visual Studio 2010 because it is not Rename Refactoring friendly.
		/// When you are renaming parameter then the Anonymous type property won't be renamed.
		/// It's funny but it will work in SharpDevelop.
		/// </summary>
		/// <typeparam name="TAnonymous"></typeparam>
		/// <param name="source"></param>
		/// <returns></returns>
		private static string FromAnonymType<TAnonymous>(TAnonymous source)
			where TAnonymous : class
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var properties = source.GetType().GetProperties();
			if (properties.Length != 1)
				throw new ArgumentException(
					string.Format("Source object should contain only one property, but actually contains {0} properties",
					properties.Length));

			return properties[0].Name;
		}

		[TestMethod]
		[Ignore]
		public void Approach_with_String_is_faster_than_Anonymous_that_is_faster_than_MethodBase_that_is_faster_than_Lambda()
		{
			const int iterations = 100000;
			var executed = 0;
			var steps = 0;
			PropertyChanged += (sender, e) => executed++;

			var timeWithName = NotifyTimingStep(() => SomePropertyWithName = "wowSomeProperty", iterations, ref steps);
			var timeWithAnonymousType = NotifyTimingStep(() => SomePropertyWithAnonymousType = "wowSomeProperty", iterations, ref steps);
			var timeWithMethodBase = NotifyTimingStep(() => SomePropertyWithMethodBase = "wowSomeProperty", iterations, ref steps);
			var timeWithLambda = NotifyTimingStep(() => SomePropertyWithLambda = "wowSomeProperty", iterations, ref steps);

			Debug.WriteLine("timeWithName: " + timeWithName);
			Debug.WriteLine("timeWithAnonymousType: " + timeWithAnonymousType);
			Debug.WriteLine("timeWithMethodBase: " + timeWithMethodBase);
			Debug.WriteLine("timeWithLambda: " + timeWithLambda);

			var anonTypeSlowerThanName = timeWithAnonymousType.Ticks > timeWithName.Ticks;
			var methodBase10TimesSlowerThanName = timeWithMethodBase.Ticks / 10 > timeWithName.Ticks;
			var lambda50TimesSlowerThanName = timeWithLambda.Ticks / 50 > timeWithName.Ticks;
			var methodBaseTimesSlowerThanAnonType = timeWithMethodBase.Ticks > timeWithAnonymousType.Ticks;
			var lambda10TimesSlowerThanAnonType = timeWithLambda.Ticks / 10 > timeWithAnonymousType.Ticks;
			var lambdaSlowerThanMethodBase = timeWithLambda.Ticks > timeWithMethodBase.Ticks;

			anonTypeSlowerThanName.Should().BeTrue();
			methodBase10TimesSlowerThanName.Should().BeTrue();
			lambda50TimesSlowerThanName.Should().BeTrue();
			methodBaseTimesSlowerThanAnonType.Should().BeTrue();
			lambda10TimesSlowerThanAnonType.Should().BeTrue();
			lambdaSlowerThanMethodBase.Should().BeTrue();

			(timeWithName < timeWithAnonymousType).Should().BeTrue();
			(timeWithAnonymousType < timeWithMethodBase).Should().BeTrue();
			(timeWithMethodBase < timeWithLambda).Should().BeTrue();

			executed.Should().Be(iterations * steps);
		}
	}
}
