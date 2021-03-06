﻿using System;
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
                this.NotifyAccess();
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

    public class GetComputed<TValue> : IGetChanged<TValue>, IDisposable
    {
        public GetComputed(Func<TValue> getValue)
        {
            _getValue = getValue.ThrowIfNull();
            _observed = new List<ObservedEntry>();
            ComputeValueAndSubscribeToChangedParticipants();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable<IGetChanged> Observed
        {
            get { return _observed.Select(entry => entry.Changed).Where(changed => changed != null); }
        }

        public TValue Value { get { return ComputeValueAndSubscribeToChangedParticipants(); } }

        public void Dispose()
        {
            for (var i = 0; i < _observed.Count; i++)
            {
                _observed[i].Changed.PropertyChanged -= NotifyChanged;
                _observed[i].Changed = null;
            }
        }

        #region Implementation

        private readonly Func<TValue> _getValue;
        private readonly List<ObservedEntry> _observed;

        private TValue ComputeValueAndSubscribeToChangedParticipants()
        {
            SetAllAsUnchanged();
            Computed.PushAction(UpdateChanged);
            TValue value;
            try
            {
                value = _getValue();
            }
            finally
            {
                Computed.PopAction();
            }
            RemoveUnchanged();
            return value;
        }

        private void NotifyChanged(object sender, PropertyChangedEventArgs e)
        {
            var evt = PropertyChanged;
            if (evt != null)
                evt(this, e);
        }

        private void SetAllAsUnchanged()
        {
            for (var i = 0; i < _observed.Count; i++)
                _observed[i].ChangedRecently = false;
        }

        private void UpdateChanged(IGetChanged changed)
        {
            ObservedEntry spareObserved = null;
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
                _observed.Add(new ObservedEntry { Changed = changed, ChangedRecently = true });
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
                if (!observed.ChangedRecently && observed.Changed != null)
                {
                    observed.Changed.PropertyChanged -= NotifyChanged;
                    observed.Changed = null;
                }
            }
        }

        private sealed class ObservedEntry
        {
            public IGetChanged Changed;
            public bool ChangedRecently;
        }

        #endregion
    }

    public sealed class Computed<TValue> : GetComputed<TValue>, IChanged<TValue>
    {
        public Computed(Func<TValue> getValue, Action<TValue> setValue)
            : base(getValue)
        {
            _setValue = setValue.ThrowIfNull();
        }

        public new TValue Value
        {
            get { return base.Value; }
            set { _setValue(value); }
        }

        private readonly Action<TValue> _setValue;
    }

    public static class Computed
    {
        public static GetComputed<TValue> From<TValue>(Func<TValue> getValue)
        {
            return new GetComputed<TValue>(getValue);
        }

        public static void PushAction(Action<IGetChanged> action)
        {
            _stack = new Stack<Action<IGetChanged>>(action, _stack);
        }

        public static void PopAction()
        {
            _stack = _stack.Tail;
        }

        public static void NotifyAccess(this IGetChanged changed)
        {
            var stack = _stack;
            if (stack != null)
                stack.Head.Invoke(changed);
        }

        #region Implementation

        [ThreadStatic]
        private static Stack<Action<IGetChanged>> _stack;

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

    public class ChangedException : InvalidOperationException
    {
        public ChangedException(string message) : base(message) { }
    }
}
