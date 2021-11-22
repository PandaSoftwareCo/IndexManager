using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace IndexManager.Framework
{
    public class BaseModel : INotifyPropertyChanged, IDataErrorInfo, INotifyDataErrorInfo
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region IDataErrorInfo

        public string this[string propertyName]
        {
            get
            {
                var validationResults = new List<ValidationResult>();

                if (Validator.TryValidateProperty(
                        GetType().GetProperty(propertyName).GetValue(this)
                        , new ValidationContext(this)
                        {
                            MemberName = propertyName
                        }
                        , validationResults))
                    return null;

                return validationResults.First().ErrorMessage;
            }
        }

        protected string _error = string.Empty;

        public string Error => _error;

        #endregion IDataErrorInfo

        #region INotifyDataErrorInfo

        private Dictionary<string, List<string>> propErrors = new Dictionary<string, List<string>>();

        [NotMapped]
        public bool HasErrors
        {
            get
            {
                try
                {
                    var propErrorsCount = propErrors.Values.FirstOrDefault(r => r.Count > 0);
                    if (propErrorsCount != null)
                        return true;
                    else
                        return false;
                }
                catch { }
                return true;
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            List<string> errors = new List<string>();
            if (propertyName != null)
            {
                propErrors.TryGetValue(propertyName, out errors);
                return errors;
            }
            else
            {
                return null;
            }
        }

        private void AddError(string propertyName, string error)
        {
            if (!propErrors.ContainsKey(propertyName))
                propErrors[propertyName] = new List<string>();

            if (!propErrors[propertyName].Contains(error))
            {
                propErrors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (propErrors.ContainsKey(propertyName))
            {
                propErrors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        #endregion INotifyDataErrorInfo
    }
}
