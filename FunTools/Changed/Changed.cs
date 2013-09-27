using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace FunTools.Changed
{
    using IGetChanged = INotifyPropertyChanged;

    public interface IGetChanged<TValue> : IGetChanged
    {
        TValue Value { get; }
    }

    public interface IChanged<TValue> : IGetChanged<TValue>
    {
        new TValue Value { get; set; }
    }

    public static class Changed
    {
        public static Changed<TValue> From<TValue>(TValue initialValue = default(TValue), Changed<TValue>.ChangeCondition isChanged = null)
        {
            return new Changed<TValue>(initialValue, isChanged);
        }

        public static void PushAction(Action<IGetChanged> action)
        {
            _stack = new Stack<Action<IGetChanged>>(action, _stack);
        }

        public static void PopAction()
        {
            _stack = _stack.Tail;
        }

        #region Implementation

        [ThreadStatic] private static Stack<Action<IGetChanged>> _stack;

        internal sealed class Stack<T>
        {
            public readonly T Head;
            public readonly Stack<T> Tail;

            public Stack(T head, Stack<T> tail = null)
            {
                Head = head;
                Tail = tail;
            }
        }

        #endregion
    }

    public sealed class Changed<TValue> : IChanged<TValue>
    {
        public static implicit operator TValue(Changed<TValue> changed)
        {
            return changed.Value;
        }

        public static readonly PropertyChangedEventArgs ChangedEventArgs = new PropertyChangedEventArgs("Value");

        public event PropertyChangedEventHandler PropertyChanged;

        public delegate bool ChangeCondition(TValue oldValue, TValue newValue);

        public Changed(TValue initialValue = default(TValue), ChangeCondition isChanged = null) // By default uses Changed.NotEqual
        {
            _value = initialValue;
            _isChanged = isChanged ?? NotEqual;
        }

        public TValue Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_isChanged == null)
                    throw new ChangedException(string.Format(
                        "Recursive change detected (OR change is intermingled by multiple threads) for value {0} of type {1}.", _value, typeof(TValue)));

                if (!_isChanged(_value, value))
                    return;

                _value = value;

                var changedEvent = PropertyChanged;
                if (changedEvent == null)
                    return;

                // Nullify change checker as indicator for recursive event invocation and restore it when event completed.
                // That way we don't need to introduce separate "_isChanging" field.
                var isChanged = _isChanged; _isChanged = null;
                try { changedEvent(this, ChangedEventArgs); }
                finally { _isChanged = isChanged; }
            }
        }

        #region Implementation

        private TValue _value;
        private ChangeCondition _isChanged;

        private static bool NotEqual(TValue oldValue, TValue newValue)
        {
            return !ReferenceEquals(oldValue, newValue) &&
                   (ReferenceEquals(oldValue, null) || !oldValue.Equals(newValue));
        }

        #endregion
    }

    public sealed class GetComputed<TValue> : IGetChanged<TValue>, IDisposable
    {
        public GetComputed(Func<TValue> compute)
        {
            _compute = compute.ThrowIfNull();
            _observed = new List<Observed>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TValue Value
        {
            get
            {
                SetAllAsUnchanged();
                Changed.PushAction(ObserveChanged);
                TValue value;
                try
                {
                    value = _compute();
                }
                finally
                {
                    Changed.PopAction();
                }
                RemoveUnchanged();
                return value;
            }
        }

        public void Dispose()
        {
            for (var i = 0; i < _observed.Count; i++)
            {
                _observed[i].Changed.PropertyChanged -= NotifyChanged;
                _observed[i].Changed = null;
            }
        }

        #region Implementation

        private readonly Func<TValue> _compute;
        private readonly List<Observed> _observed;

        private void NotifyChanged(object sender, PropertyChangedEventArgs e)
        {
            var changedEvent = PropertyChanged;
            if (changedEvent != null)
                changedEvent(this, e);
        }

        private void SetAllAsUnchanged()
        {
            for (var i = 0; i < _observed.Count; i++)
                _observed[i].ChangedRecently = false;
        }

        private void ObserveChanged(IGetChanged changed)
        {
            Observed spareObserved = null;
            for (var i = 0; i < _observed.Count; ++i)
            {
                var observed = _observed[i];
                if (observed.Changed == null)
                    spareObserved = observed;
                else if (observed.Changed == changed)
                {
                    observed.ChangedRecently = true;
                    return;
                }
            }

            if (spareObserved == null)
                _observed.Add(new Observed {Changed = changed, ChangedRecently = true});
            else
            {
                spareObserved.Changed = changed;
                spareObserved.ChangedRecently = true;
            }

            changed.PropertyChanged += NotifyChanged;
        }

        private void RemoveUnchanged()
        {
            for (var i = 0; i < _observed.Count; i++)
            {
                var observed = _observed[i];
                if (!observed.ChangedRecently)
                {
                    observed.Changed.PropertyChanged -= NotifyChanged;
                    observed.Changed = null;
                }
            }
        }

        private sealed class Observed
        {
            public IGetChanged Changed;
            public bool ChangedRecently;
        }

        #endregion
    }

    public class ChangedException : InvalidOperationException
    {
        public ChangedException(string message) : base(message) { }
    }
}
