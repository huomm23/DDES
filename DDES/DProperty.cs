using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DDES
{
    public class DProperty
    {
        private static readonly Dictionary<PropertyInfo, DProperty> _Cache = new Dictionary<PropertyInfo, DProperty>();

        public static DProperty Create(PropertyInfo pi)
        {
            DProperty dp;
            if (_Cache.TryGetValue(pi, out dp))
            {
                return dp;
            }

            lock (_Cache)
            {
                if (_Cache.TryGetValue(pi, out dp))
                {
                    return dp;
                }
                else
                {
                    dp = new DProperty(pi);
                }
                _Cache[pi] = dp;
                return dp;
            }
        }

        public PropertyInfo PropertyInfo { get; private set; }
        public Type PropertyType { get; private set; }
        public Type DeclaringType { get; private set; }
        public bool CanRead { get; private set; }
        public bool CanWrite { get; private set; }
        public bool IsPublic { get; private set; }
        public bool IsStatic { get; private set; }
        public string Name { get; private set; }

        /// <summary> 用于读取对象当前属性/字段的委托
        /// </summary>
        public Func<object, object> Getter { get; private set; }

        /// <summary> 用于设置对象当前属性/字段的委托
        /// </summary>
        public Action<object, object> Setter { get; private set; }

        private DProperty(PropertyInfo pi)
        {
            PropertyInfo = pi;
            Name = pi.Name;
            PropertyType = pi.PropertyType;
            DeclaringType = pi.DeclaringType;
            CanRead = pi.CanRead;
            CanWrite = pi.CanWrite;
            IsPublic = pi.PropertyType.IsPublic;

            var get = pi.GetGetMethod(true); //获取属性get方法,不论是否公开
            var set = pi.GetSetMethod(true); //获取属性set方法,不论是否公开
            if (get != null)
            {
                IsStatic = get.IsStatic;
            }
            if (set != null)
            {
                IsStatic = set.IsStatic;
            }

            if (pi.DeclaringType.Name.StartsWith("<>f__AnonymousType")) //匿名类
            {
                CanWrite = true;
                IsPublic = false;
            }

            //var nullable = System.Nullable.GetUnderlyingType(PropertyType);
            //if (nullable != null)
            //{
            //    Nullable = true;
            //    MemberType = nullable;
            //}
            //else
            //{
            //    MemberType = OriginalType;
            //}
            Getter = CreateGetter(pi);

            //Init();
            //if (set == null && CanWrite) //匿名类的属性设置特殊处理
            //{
            //    Setter = (o, v) =>
            //    {
            //        var field = ClassType.GetField("<" + Name + ">i__Field", (BindingFlags)(-1));
            //        Setter = Literacy.CreateSetter(field, ClassType);
            //        Setter(o, v);
            //    };
            //}
        }
        /// <summary> typeof(Object)
        /// </summary>
        private static readonly Type TypeObject = typeof(Object);

        /// <summary> [ typeof(Object) ]
        /// </summary>
        private static readonly Type[] TypesObject = { typeof(Object) };

        /// <summary> [ typeof(Object),typeof(Object) ]
        /// </summary>
        private static readonly Type[] Types2Object = { typeof(Object), typeof(Object) };

        /// <summary> [ typeof(object[]) ]
        /// </summary>
        private static readonly Type[] TypesObjects = { typeof(object[]) };

        /// <summary> [ typeof(Object), typeof(object[])  ]
        /// </summary>
        private static readonly Type[] TypesObjectObjects = { typeof(Object), typeof(object[]) };

        /// <summary> IL构造一个用于获取对象属性值的委托
        /// </summary>
        public Func<object, object> CreateGetter(PropertyInfo prop, Type owner = null)
        {
            if (prop == null)
            {
                return null;
            }
            var dm = new DynamicMethod("", TypeObject, TypesObject, owner ?? GetOwnerType(prop), true);
            var il = dm.GetILGenerator();
            var met = prop.GetGetMethod(true);
            if (met == null)
            {
                return null;
            }
            if (met.IsStatic)
            {
                il.Emit(OpCodes.Call, met);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                EmitCast(il, prop.DeclaringType);
                if (prop.DeclaringType.IsValueType)
                {
                    il.Emit(OpCodes.Call, met);
                }
                else
                {
                    il.Emit(OpCodes.Callvirt, met);
                }
            }
            if (prop.PropertyType.IsValueType)
            {
                il.Emit(OpCodes.Box, prop.PropertyType);
            }
            il.Emit(OpCodes.Ret);
            return (Func<object, object>)dm.CreateDelegate(typeof(Func<object, object>));
        }

        /// <summary> IL类型转换指令
        /// </summary>
        private static void EmitCast(ILGenerator il, Type type, bool check = true)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
                if (check && Nullable.GetUnderlyingType(type) == null)
                {
                    var t = il.DeclareLocal(type);
                    il.Emit(OpCodes.Stloc, t);
                    il.Emit(OpCodes.Ldloca_S, t);
                }
            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
            }
        }
        /// <summary> 获取对象的属性/字段值
        /// </summary>
        /// <param name="obj">将要获取其属性/字段值的对象</param>
        /// <exception cref="ArgumentNullException">实例属性instance对象不能为null</exception>
        /// <exception cref="ArgumentException">对象无法获取属性/字段的值</exception>
        public object GetValue(object obj)
        {
            if (!CanRead)
            {
                throw new Exception();
                //ErrorGetter(null);
            }
            else if (obj == null)
            {
                if (IsStatic == false)
                {
                    throw new ArgumentNullException("instance", "实例属性对象不能为null");
                }
            }
            else if (DeclaringType.IsInstanceOfType(obj) == false)
            {
                throw new ArgumentException("对象[" + obj + "]无法获取[" + DeclaringType + "]的值");
            }

            try
            {
                return Getter(obj);
            }
            catch (Exception ex)
            {
                var message = $"{PropertyInfo.ToString()}.{Name}属性取值失败";
                Trace.WriteLine(ex, message);
                throw new TargetInvocationException(message + ",原因见内部异常", ex);
            }
        }

        /// <summary> 如果是数组,则获取数组中元素的类型
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private static Type GetOwnerType(MemberInfo member)
        {
            //owner 是一个接口、一个数组、一个开放式泛型类型或一个泛型类型或方法的类型参数。
            Type type = member.ReflectedType;
            while (true)
            {
                if (type.IsArray)
                {
                    type = member.ReflectedType.GetElementType();
                }
                else if (
                    type.IsGenericParameter ||
                    type.IsInterface)
                {
                    return typeof(object);
                }
                else
                {
                    return type;
                }
            }
        }
    }
}
