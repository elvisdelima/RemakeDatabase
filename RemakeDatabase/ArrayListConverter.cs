using System;
using System.Linq;

namespace RemakeDatabase
{
    public class ArrayListConverter
    {
        private readonly static Type AttributeType = typeof(ArrayListConverterPropertyAttribute);
        public static T Convert<T>(string[] config) where T : new()
        {
            var type = typeof(T);
            var instance = new T();
            var dictionary = config.Select(s => s.Split('=')).ToDictionary(s => s[0], s => s[1]);

            foreach (var propertyInfo in type.GetProperties())
            {
                if (!propertyInfo.CanWrite)
                    continue;

                string value = null;
                var hasPropertyWithSameName = dictionary.ContainsKey(propertyInfo.Name);

                if (hasPropertyWithSameName)
                    value = dictionary[propertyInfo.Name];
                else
                    foreach (var attribute in propertyInfo.GetCustomAttributes(AttributeType, false).Cast<ArrayListConverterPropertyAttribute>().Where(attribute => dictionary.ContainsKey(attribute.PropertyName)))
                        value = dictionary[attribute.PropertyName];
                if (value != null) 
                    propertyInfo.SetValue(instance, value, null);
            }
            return instance;
        }
    }
}