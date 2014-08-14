using System;
using System.ComponentModel;
using System.Diagnostics;
using FunTools.Weak;
using NUnit.Framework;

namespace FunTools.UnitTests.Weak
{
	[TestFixture]
	public class WeakSubscriptionPerformanceTests
	{
		[Test]
		[Ignore]
		public void Subscription_performance_comparison()
		{
			const int times = 20000;

			var timeToSubcribeAsUsual = SubcribeAsUsual(times).TotalMilliseconds;
			var timeToSubcribeHandlers = SubcribeHandlers(times).TotalMilliseconds;
			var timeToSubcribeMethodsWithReflection = SubcribeMethodsWithReflection(times).TotalMilliseconds;

			timeToSubcribeHandlers.Should().BeGreaterThan(timeToSubcribeAsUsual);
			timeToSubcribeMethodsWithReflection.Should().BeGreaterThan(timeToSubcribeHandlers);
		}

		[Test]
		[Ignore]
		public void Event_notification_performance_comparison()
		{
			const int times = 100000;

			var timeForAsUsual = NotifyAsUsual(times).TotalMilliseconds;
			var timeForMethodsWithReflection = NotifyMethodsWithReflection(times).TotalMilliseconds;

			timeForMethodsWithReflection.Should().BeGreaterThan(timeForAsUsual);
		}

		#region Subscription runners

		internal TimeSpan SubcribeAsUsual(int times)
		{
			var someView = new SomeView();
			var someViewModel = new SomeViewModel();

			var stopwatch = Stopwatch.StartNew();

			for (var i = 0; i < times; i++)
			{
				someViewModel.PropertyChanged += someView.OnPropertyChanged;
			}

			stopwatch.Stop();
			return stopwatch.Elapsed;
		}

		internal TimeSpan SubcribeHandlers(int times)
		{
			var view = new SomeView();
			var viewModel = new SomeViewModel();

			var stopwatch = Stopwatch.StartNew();

			for (var i = 0; i < times; i++)
				viewModel.SubscribeWeakly(view, (v, m, e) => v.OnFlagChanged(false));

			stopwatch.Stop();
			return stopwatch.Elapsed;
		}

		internal TimeSpan SubcribeMethodsWithReflection(int times)
		{
			var view = new SomeView();
			var viewModel = new SomeViewModel();

			var stopwatch = Stopwatch.StartNew();

			for (var i = 0; i < times; i++)
				viewModel.SubscribeWeakly(x => x.Flag, view.OnFlagChanged);

			stopwatch.Stop();
			return stopwatch.Elapsed;
		}

		#endregion

		#region Notification runners

		internal TimeSpan NotifyAsUsual(int times)
		{
			var someView = new SomeView();
			var someViewModel = new SomeViewModel();
			someViewModel.PropertyChanged += someView.OnPropertyChanged;

			var stopwatch = Stopwatch.StartNew();

			for (var i = 0; i < times; i++)
			{
				someViewModel.NotifyPropertyChanged();
			}

			stopwatch.Stop();
			return stopwatch.Elapsed;
		}

		internal TimeSpan NotifyMethodsWithReflection(int times)
		{
			var view = new SomeView();
			var viewModel = new SomeViewModel();

			viewModel.SubscribeWeakly(x => x.Flag, view.OnFlagChanged);

			var stopwatch = Stopwatch.StartNew();

			for (var i = 0; i < times; i++)
				viewModel.NotifyPropertyChanged();

			stopwatch.Stop();
			return stopwatch.Elapsed;
		}

		#endregion

		#region CUT

		public class EventObserver
		{
			public bool IsPropertyChangedHandled { get; set; }
		}

		public class SomeViewModel : INotifyPropertyChanged
		{
			public bool Flag { get; set; }

			public event PropertyChangedEventHandler PropertyChanged;

			public void NotifyPropertyChanged()
			{
				var handler = PropertyChanged;
				if (handler != null) handler(this, null);
			}
		}

		public class SomeView
		{
			public readonly EventObserver Observer = new EventObserver();

			public void OnFlagChanged(bool flag)
			{
				Observer.IsPropertyChangedHandled = true;
			}

			public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				Observer.IsPropertyChangedHandled = true;
			}
		}

		#endregion
	}
}