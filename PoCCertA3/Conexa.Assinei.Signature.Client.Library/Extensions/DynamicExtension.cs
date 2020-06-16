using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

namespace Conexa.Assinei.Signature.Client.Library.Extensions
{
    public static class DynamicExtension
    {
        public static T GetValue<T>(this object obj, string propertyName, T @default = default)
        {
            if (obj is ExpandoObject)
            {
                IDictionary<string, object> propertyValues = (ExpandoObject)obj;
                var prop = propertyValues.Keys.FirstOrDefault(f => string.Equals(f, propertyName, StringComparison.InvariantCultureIgnoreCase));
                return string.IsNullOrEmpty(prop) ? @default : propertyValues[prop].ConvertValue<T>();
            }
            else
            {
                var prop = obj.GetType().GetProperties().FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.InvariantCultureIgnoreCase));
                if (prop != null)
                {
                    var value = prop.GetValue(obj, null);
                    return value == null ? @default : value.ConvertValue<T>();
                }
            }

            return @default;
        }

        public static T ConvertValue<T>(this object obj)
        {
            if (obj is T)
                return (T)obj;

            try
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        public static dynamic ToDynamic(this object value)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
            {
                var name = property.Name.Substring(0, 1).ToLowerInvariant() + (property.Name.Length > 1 ? property.Name.Substring(1, property.Name.Length - 1) : string.Empty);
                expando.Add(name, property.GetValue(value));
            }

            return expando as ExpandoObject;
        }
    }
}
