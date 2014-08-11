using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using FunTools.Changed;

namespace FunTools.Validation
{
    public interface IValidated
    {
        Validator Validator { get; }
    }

    public sealed class Validator : INotifyPropertyChanged, IDisposable, IEnumerable<KeyValuePair<string, Validator.GetValidationErrorOrNull>>
    {
        // These two properties could be used for binding to UI.
        public bool IsValid { get; private set; }
        public Dictionary<string, string> Errors { get; private set; }

        public bool Enabled
        {
            get { return _enabled; }
            set { Enable(value); }
        }

        public delegate bool CanChangeEnabled(bool enabled, bool changedByParent, bool enabling, bool changingByParent);
        public static CanChangeEnabled CanChangeEnabledDefault = PreventEnablingIfDisabledByParent;

        public Validator(
            INotifyPropertyChanged model,
            bool enabledInitially = true,
            CanChangeEnabled canChangeEnabled = null)
        {
            if (model == null) throw new ArgumentNullException("model");

            _model = new WeakReference(model);
            _propertyValidationRules = new Dictionary<string, GetValidationErrorOrNull>();
            _enabled = enabledInitially;
            _canChangeEnabled = canChangeEnabled ?? CanChangeEnabledDefault;

            IsValid = true;
            Errors = new Dictionary<string, string>();

            BindValidatedProperties(model);
        }

        public delegate string GetValidationErrorOrNull();
        public void Add(string propertyName, GetValidationErrorOrNull validationRule)
        {
            _propertyValidationRules[propertyName] = validationRule;
        }

        public string Validate(string propertyName)
        {
            if (!_enabled ||
                !_propertyValidationRules.ContainsKey(propertyName))
                return null;

            var error = _propertyValidationRules[propertyName].Invoke();
            OnPropertyValidated(propertyName, error);
            return error;
        }

        public void ValidateAll()
        {
            if (!_enabled)
                return;

            if (_subscribedPropertyValidators != null)
                foreach (var validator in _subscribedPropertyValidators)
                    validator.Value.ValidateAll();

            if (_propertyValidationRules != null)
                foreach (var validator in _propertyValidationRules)
                    OnPropertyValidated(validator.Key, validator.Value.Invoke(), notifyIfSameValidity: true);

            NotifyAllModelPropertiesChanged();
        }

        #region IDisposable

        public void Dispose()
        {
            if (_subscribedPropertyValidators != null)
                foreach (var validator in _subscribedPropertyValidators)
                    validator.Value.PropertyChanged -= OnPropertyValidatorValidityChanged;

            _validatedPropertySelectors = null;
            _subscribedPropertyValidators = null;
            _propertyValidationRules = null;
        }

        #endregion

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, GetValidationErrorOrNull>> GetEnumerator()
        {
            return _propertyValidationRules.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Implementation

        private readonly WeakReference _model;
        private readonly CanChangeEnabled _canChangeEnabled;

        private Dictionary<string, GetValidationErrorOrNull> _propertyValidationRules;
        private Dictionary<string, Func<object, IValidated>> _validatedPropertySelectors;
        private Dictionary<string, Validator> _subscribedPropertyValidators;
        private bool _enabled;
        private bool _enabledChangedByParent;

        private static bool PreventEnablingIfDisabledByParent(bool enabled, bool changedByParent, bool enabling, bool changingByParent)
        {
            if (!enabled && changedByParent && enabling && !changingByParent)
                return false;
            return true;
        }

        private void BindValidatedProperties(INotifyPropertyChanged model)
        {
            var validatedProperties = model.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => typeof(IValidated).IsAssignableFrom(p.PropertyType)).ToArray();

            if (validatedProperties.Length == 0)
                return;

            _validatedPropertySelectors = new Dictionary<string, Func<object, IValidated>>();
            _subscribedPropertyValidators = new Dictionary<string, Validator>();

            for (var i = 0; i < validatedProperties.Length; i++)
            {
                var propertyInfo = validatedProperties[i];
                var property = (IValidated)propertyInfo.GetValue(model, null);
                var propertyName = propertyInfo.Name;
                if (property != null && property.Validator != null)
                {
                    var validator = property.Validator;
                    validator.PropertyChanged += OnPropertyValidatorValidityChanged;
                    _subscribedPropertyValidators[propertyName] = validator;
                }

                _validatedPropertySelectors.Add(propertyName, m => (IValidated)propertyInfo.GetValue(m, null));
            }

            model.PropertyChanged += OnValidatedPropertyChanged;
        }

        private void OnValidatedPropertyChanged(object modelObject, PropertyChangedEventArgs e)
        {
            if (_validatedPropertySelectors == null)
                return;

            var propertyName = e.PropertyName;
            if (!string.IsNullOrEmpty(propertyName))
            {
                Func<object, IValidated> selectProperty;
                if (_validatedPropertySelectors.TryGetValue(propertyName, out selectProperty))
                    ReBindValidatedProperty(selectProperty(modelObject), propertyName);
            }
            else
            {
                foreach (var propertySelector in _validatedPropertySelectors)
                    ReBindValidatedProperty(propertySelector.Value(modelObject), propertySelector.Key);
            }
        }

        private void ReBindValidatedProperty(IValidated property, string propertyName)
        {
            if (property == null || property.Validator == null)
                return;

            var validator = property.Validator;

            Validator oldValidator;
            if (_subscribedPropertyValidators.TryGetValue(propertyName, out oldValidator) &&
                validator == oldValidator)
                return;

            if (oldValidator != null)
                oldValidator.PropertyChanged -= OnPropertyValidatorValidityChanged;

            validator.PropertyChanged += OnPropertyValidatorValidityChanged;
            _subscribedPropertyValidators[propertyName] = validator;

            validator.ValidateAll(); // enforce validation on property to notify parent model
        }

        private void OnPropertyValidatorValidityChanged(object validatorObject, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsValid")
                return;

            var validator = (Validator)validatorObject;
            var error = validator.IsValid ? null : "Invalid";
            var propertyName = _subscribedPropertyValidators.Where(x => x.Value == validator).Select(x => x.Key).FirstOrDefault();
            if (propertyName == null)
                throw new InvalidOperationException("Unknown property validator we never subscribed for.");

            OnPropertyValidated(propertyName, error);
        }

        private void OnPropertyValidated(string propertyName, string error, bool notifyIfSameValidity = false)
        {
            if (!_enabled)
                return;

            var errorsChanged = false;
            string oldError;
            if (!Errors.TryGetValue(propertyName, out oldError) || error != oldError)
            {
                errorsChanged = true;
                Errors[propertyName] = error;
                OnPropertyChanged("Errors");
            }

            var oldValidity = IsValid;
            if (errorsChanged)
                IsValid = Errors.All(e => string.IsNullOrEmpty(e.Value));
            if (IsValid != oldValidity || notifyIfSameValidity)
                OnPropertyChanged("IsValid");
        }

        private void Enable(bool enabling, bool changingByParent = false)
        {
            if (!_canChangeEnabled(_enabled, _enabledChangedByParent, enabling, changingByParent))
                return;

            _enabledChangedByParent = changingByParent;

            var enabled = _enabled;
            _enabled = enabling;

            // first changed enabled for all validated properties
            if (enabled != enabling && _subscribedPropertyValidators != null)
                foreach (var validator in _subscribedPropertyValidators)
                    validator.Value.Enable(enabling, changingByParent: true);

            if (enabled && !enabling)
            {
                if (!IsValid)
                {
                    Errors.Clear();
                    OnPropertyChanged("Errors");
                    
                    IsValid = true;
                    OnPropertyChanged("IsValid");

                    NotifyAllModelPropertiesChanged();
                }
            }
            else if (!enabled && enabling)
            {
                ValidateAll();
            }

            if (enabled != !enabling)
                OnPropertyChanged("Enabled");
        }

        private void NotifyAllModelPropertiesChanged()
        {
            var model = _model.Target as INotifyPropertyChanged;
            if (model != null)
                model.RaisePropertyChanged(string.Empty);
        }

        #endregion
    }
}