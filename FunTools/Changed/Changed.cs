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

        public delegate bool ChangeCondition(TValue oldValue, TValue setValue);

        public Changed(
            TValue initialValue = default(TValue),
            ChangeCondition changeCondition = null) // Use !Equals(a, b) by default.
        {
            _value = initialValue;
            _changeCondition = changeCondition;
        }

        public TValue Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_changeCondition != null && !_changeCondition(_value, value) || Equals(_value, value))
                    return;

                if (_changing)
                    throw new ChangedException(string.Format("Recursive change dectected of {0}.", _value));

                _value = value;

                var changed = PropertyChanged;
                if (changed == null) 
                    return;

                _changing = true;
                try { changed(this, ChangedEventArgs); }
                finally { _changing = false; }
            }
        }

        private TValue _value;
        private readonly ChangeCondition _changeCondition;
        private bool _changing;
    }

    public class ChangedException : InvalidOperationException
    {
        public ChangedException(string message) : base(message) { }
    }
}
