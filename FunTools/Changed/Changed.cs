using System.ComponentModel;

namespace FunTools.Changed
{
    public abstract class Changed<TValue> : INotifyPropertyChanged
    {
        public abstract TValue Value { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public static implicit operator TValue(Changed<TValue> changed)
        {
            return changed.Value;
        }

        public static readonly string VALUE_AS_PROPERTY_NAME = "Value";

        #region Implementation

        protected virtual void NotifyChanged()
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(VALUE_AS_PROPERTY_NAME));
        }

        #endregion
    }
}
