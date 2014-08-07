using System.ComponentModel;
using System.Runtime.CompilerServices;
using FunTools.Changed;
using NUnit.Framework;

namespace FunTools.Playground
{
    [TestFixture]
    public class EventInValueType
    {
        [Test][Ignore]
        public void Test()
        {
            var wrapEvent = new WrapEvent();
            bool notified1 = false;
            wrapEvent.PropertyChanged += (sender, args) => notified1 = true;

            var wrapEvent2 = wrapEvent;
            bool notified2 = false;
            wrapEvent2.PropertyChanged += (sender, args) => notified2 = true;

            wrapEvent.Notify();
            Assert.That(notified1, Is.True);
            Assert.That(notified2, Is.True);
        }

        public struct WrapEvent : INotifyPropertyChanged
        {
            public void Notify()
            {
                OnPropertyChanged();
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                var handler = PropertyChanged;
                if (handler != null) 
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
