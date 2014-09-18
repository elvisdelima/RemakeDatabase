using System;

namespace RemakeDatabase
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ArrayListConverterPropertyAttribute : Attribute
    {
        public ArrayListConverterPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; private set; }
    }
}