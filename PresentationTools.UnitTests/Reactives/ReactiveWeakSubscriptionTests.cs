using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PresentationTools.Events.Weak;
using PresentationTools.Reactives;

namespace PresentationTools.UnitTests.Reactives
{
	// ReSharper disable RedundantAssignment
	[TestClass]
	public class ReactiveWeakSubscriptionTests
	{
		[TestMethod]
		[Ignore]
		// NOTE Test is indicator that Reactive.OnChange is not working as expected
		public void OnChange_should_not_call_handler_when_subscriber_is_collected()
		{
			// Arrange
			var observer = new Observer();
			var model = new SomeModel(observer);
			var counter = Reactive.Of(0).OnChange(model.Change);

			// Act

			model = null;
			GC.Collect();
			counter.Value = 1;

			// Assert
			observer.ChangedObserved.Should().BeFalse();
		}

		[TestMethod]
		public void PropertyChanged_should_not_call_weakly_subscribed_subscriber_when_subscriber_is_collected()
		{
			// Arrange
			var observer = new Observer();
			var model = new SomeModel(observer);
			var counter = Reactive.Of(0);
			counter.SubscribeWeakly(model, (m, s, e) => m.Change(s));

			// Act
			model = null;
			GC.Collect();
			counter.Value = 1;

			// Assert
			observer.ChangedObserved.Should().BeFalse();
		}

		[TestMethod]
		public void PropertyChanged_should_call_weakly_subscribed_handler_when_handler_is_collected()
		{
			// Arrange
			var observer = new Observer();
			var model = new SomeModel(observer);
			Action<int> onChange = model.Change;
			var weakOnChange = new WeakReference(onChange);

			var counter = Reactive.Of(0);
			counter.PropertyChanged +=
				(sender, args) =>
				{
					var handler = weakOnChange.Target as Action<int>;
					if (handler != null)
						handler(counter);
				};

			// Act
			onChange = null;
			GC.Collect();
			counter.Value = 1;

			// Assert
			observer.ChangedObserved.Should().BeFalse();
		}

		[TestMethod]
		public void PropertyChanged_should_call_weakly_subscribed_handler_when_handler_is_collected2()
		{
			// Arrange
			var observer = new Observer();
			var model = new SomeModel(observer);

			var counter = Reactive.Of(0);
			counter.SubscribeWeakly(x => x.Value, model.Change);

			// Act
			model = null;
			GC.Collect();
			counter.Value = 1;

			// Assert
			observer.ChangedObserved.Should().BeFalse();
		}

		#region CUT

		public class Observer
		{
			public bool ChangedObserved;
		}

		public class SomeModel
		{
			private readonly Observer _observer;

			public SomeModel(Observer observer)
			{
				_observer = observer;
			}

			public void Change(int _)
			{
				_observer.ChangedObserved = true;
			}
		}

		#endregion
	}
	// ReSharper restore RedundantAssignment
}