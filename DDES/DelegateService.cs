using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DDES
{
    public static class DelegateService
    {
        public static Func<object, object> CreateGetter(PropertyInfo property)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            if (property.CanRead == false)
                return null;

            var getter = property.GetGetMethod();
            if (getter == null)
                return null;
            //throw new ArgumentException("The specified property does not have a public accessor.");

            var genericMethod = typeof(DelegateService).GetMethod("CreateGetterGeneric", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo genericHelper = genericMethod.MakeGenericMethod(property.DeclaringType, property.PropertyType);
            return (Func<object, object>)genericHelper.Invoke(null, new object[] { getter });
        }

        private static Func<object, object> CreateGetterGeneric<T, R>(MethodInfo getter) where T : class
        {
            Func<T, R> getterTypedDelegate = (Func<T, R>)Delegate.CreateDelegate(typeof(Func<T, R>), getter);
            Func<object, object> getterDelegate = (Func<object, object>)((object instance) => getterTypedDelegate((T)instance));
            return getterDelegate;
        }

        public static Action<object, object> CreateSetter(PropertyInfo property)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            if (property.CanWrite == false)
                return null;

            var setter = property.GetSetMethod();
            if (setter == null)
                return null;
            //throw new ArgumentException("The specified property does not have a public setter.");

            var genericMethod = typeof(DelegateService).GetMethod("CreateSetterGeneric", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo genericHelper = genericMethod.MakeGenericMethod(property.DeclaringType, property.PropertyType);
            return (Action<object, object>)genericHelper.Invoke(null, new object[] { setter });
        }

        private static Action<object, object> CreateSetterGeneric<T, V>(MethodInfo setter) where T : class
        {
            Action<T, V> setterTypedDelegate = (Action<T, V>)Delegate.CreateDelegate(typeof(Action<T, V>), setter);
            Action<object, object> setterDelegate = (Action<object, object>)((object instance, object value) => { setterTypedDelegate((T)instance, (V)value); });
            return setterDelegate;
        }
    }
}
