using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OnAuth.Migrate.AdoNet
{
    public class PropertyCache
    {
        Dictionary<Type, OnTmsPropertyDescriptor[]> _cache = new Dictionary<Type, OnTmsPropertyDescriptor[]>();

        public OnTmsPropertyDescriptor[] GetProperties(Type type)
        {
            if (_cache.ContainsKey(type) == false)
            {
                _cache[type] = GetAllProperties(type);
            }
            return _cache[type].Where(r=> r.IgnoreMapping == false).ToArray();
        }

        OnTmsPropertyDescriptor[] GetAllProperties(Type type)
        {
            return type.GetProperties().Select(s => new OnTmsPropertyDescriptor(s)).ToArray();
        }
    }

    public class OnTmsPropertyDescriptor
    {
        public string FieldName { get; private set; }
        public PropertyInfo PropertyInfo { get; private set; }

        public bool IgnoreMapping { get; private set; }

        public OnTmsPropertyDescriptor(PropertyInfo pi)
        {
            PropertyInfo = pi;
            FieldName = pi.Name;
            IgnoreMapping = pi.GetCustomAttributes<IgnoreMappingAttribute>().Any();
        }

        public void SetValue(object obj, object value)
        {
            if (PropertyInfo.PropertyType == typeof(Guid))
            {
                if(value.GetType() != typeof(Guid))
                    value = new Guid((byte[])Convert.ChangeType(value, typeof(byte[])));
                PropertyInfo.SetValue(obj, value);
            }
            else
                PropertyInfo.SetValue(obj, value);
        }

        internal object GetValue(object obj)
        {
            var dbValue =  PropertyInfo.GetValue(obj);
            if (ReferenceEquals(dbValue, null))
                return DBNull.Value;
            else
                return dbValue;
        }
    }
}
