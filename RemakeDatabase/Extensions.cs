namespace RemakeDatabase
{
    static class Extensions
    {
        public static void Set(this object obj, string propertyName, object value)
        {
            var type = obj.GetType();
            type.GetProperty(propertyName).SetValue(obj, value);
        }

        public static object Get(this object obj, string propertyName)
        {
            var type = obj.GetType();
            return type.GetProperty(propertyName).GetValue(obj);
        }
        public static void InvokeMethod(this object obj, string methodName, params object[] parameters)
        {
            var type = obj.GetType();
            type.GetMethod(methodName).Invoke(obj, parameters);
        }
    }
}
