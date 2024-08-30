using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CaptaTrader.Business.Base
{
    public class Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;

            RaisePropertyChanged(propertyName);

            return true;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            ValidationContext context = new(this, serviceProvider: null, items: null);
            List<ValidationResult> results = [];

            Validator.TryValidateObject(this, context, results, validateAllProperties: true);

            return results;
        }

        //public static T? ConvertToNewEntity<T>(object source) where T : IEntity
        //{
        //    if (source != null)
        //    {
        //        Type entityType = typeof(T);
        //        PropertyInfo[] entityProperties = entityType.GetProperties();

        //        if (Activator.CreateInstance(entityType) is T entityInstance)
        //        {
        //            foreach (PropertyInfo sourceProperty in source.GetType().GetProperties())
        //            {
        //                PropertyInfo? entityProperty = entityProperties.SingleOrDefault(x => x.Name == sourceProperty.Name);

        //                if (entityProperty != null)
        //                {
        //                    object? value = sourceProperty.GetValue(source, null);
        //                    entityProperty.SetValue(entityInstance, value, null);
        //                }
        //            }

        //            return entityInstance;
        //        }
        //    }

        //    return default;
        //}















        internal virtual T ConvertToDTO<T>(T returnObject)
        {
            return ConvertBetweenDTOAndModel(this, returnObject);
        }

        internal virtual void ConvertFromDTO<T>(T sourceObject)
        {
            ConvertBetweenDTOAndModel(sourceObject, this);
        }

        private static U ConvertBetweenDTOAndModel<T, U>(T sourceObject, U returnObject)
        {
            try
            {
                if (sourceObject != null && returnObject != null)
                {
                    var returnProperties = returnObject.GetType().GetProperties();

                    foreach (var sourceProp in sourceObject.GetType().GetProperties())
                    {
                        var returnProperty = returnProperties.SingleOrDefault(x => x.Name == sourceProp.Name);

                        if (returnProperty != null)
                        {
                            var value = sourceProp.GetValue(sourceObject, null);
                            returnProperty.SetValue(returnObject, value, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return returnObject;
        }
    }
}
