using System;
using System.ComponentModel;

namespace FunTools.Changed
{
    public interface IGetChanged<TValue> : INotifyPropertyChanged
    {
        TValue Value { get; }
    }

    public interface IChanged<TValue> : IGetChanged<TValue>
    {
        new TValue Value { get; set; }
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

        public Changed(
            TValue initialValue = default(TValue),
            ChangeCondition isChanged = null) // Use !Equals(a, b) by default.
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

        private TValue _value;
        private ChangeCondition _isChanged;

        private bool NotEqual(TValue oldValue, TValue newValue)
        {
            return !ReferenceEquals(oldValue, newValue) &&
                   (ReferenceEquals(oldValue, null) || !oldValue.Equals(newValue));
        }
    }

    public class ChangedException : InvalidOperationException
    {
        public ChangedException(string message) : base(message) { }
    }
}
