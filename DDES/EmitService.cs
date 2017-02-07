using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DDES
{
    public static class EmitService
    {
        public static Func<object, object> CreateGetter(PropertyInfo prop, Type owner = null)
        {
            if (prop == null)
            {
                return null;
            }
            var method = new DynamicMethod("", typeof(Object), new Type[] { typeof(Object) }, owner ?? GetOwnerType(prop), true);
            var il = method.GetILGenerator();
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
            return (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
        }

        public static Action<object, object> CreateSetter(PropertyInfo prop, Type owner = null)
        {
            if (prop == null)
            {
                return null;
            }
            if (prop.DeclaringType.IsValueType)
            {
                throw new NotSupportedException("值类型无法通过方法给其属性或字段赋值");
            }
            var dm = new DynamicMethod("", null, new Type[] { typeof(object), typeof(object) }, owner ?? GetOwnerType(prop), true);
            var set = prop.GetSetMethod(true);
            if (set == null)
            {
                return null;
            }
            var il = dm.GetILGenerator();

            if (set.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitCast(il, prop.PropertyType, false);
                il.Emit(OpCodes.Call, set);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, prop.DeclaringType);
                il.Emit(OpCodes.Ldarg_1);
                EmitCast(il, prop.PropertyType, false);
                if (prop.DeclaringType.IsValueType)
                {
                    il.Emit(OpCodes.Call, set);
                }
                else
                {
                    il.Emit(OpCodes.Callvirt, set);
                }
            }
            il.Emit(OpCodes.Ret);

            return (Action<object, object>)dm.CreateDelegate(typeof(Action<object, object>));
        }

        public static void EmitCast(ILGenerator il, Type type, bool check = true)
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

        public static Type GetOwnerType(MemberInfo mi)
        {
            //owner 是一个接口、一个数组、一个开放式泛型类型或一个泛型类型或方法的类型参数。
            Type type = mi.ReflectedType;
            while (true)
            {
                if (type.IsArray)
                {
                    type = type.GetElementType();
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

        public static Func<object, object, object[]> CreateCaller(MethodInfo method, Type owner = null)
        {
            var dm = new DynamicMethod("", typeof(object), new Type[] { typeof(object), typeof(object) }, owner ?? GetOwnerType(method), true);
            var il = dm.GetILGenerator();
            var isRef = false;
            var ps = method.GetParameters();
            LocalBuilder[] loc = new LocalBuilder[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                Type pt = p.ParameterType;
                if (pt.IsByRef) //ref,out获取他的实际类型
                {
                    isRef = true;
                    pt = pt.GetElementType();
                }

                loc[i] = il.DeclareLocal(pt);
                if (p.IsOut == false)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    EmitCast(il, pt, false);
                    il.Emit(OpCodes.Stloc, loc[i]); //保存到本地变量
                }
            }

            if (method.IsStatic == false)
            {
                il.Emit(OpCodes.Ldarg_0);
                EmitCast(il, method.DeclaringType);
            }
            //将参数加载到参数堆栈
            foreach (var pa in method.GetParameters())
            {
                if (pa.ParameterType.IsByRef) //out或ref
                {
                    il.Emit(OpCodes.Ldloca_S, loc[pa.Position]);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, loc[pa.Position]);
                    loc[pa.Position] = null;
                }
            }
            LocalBuilder ret = null;
            if (method.ReturnType != typeof(void))
            {
                ret = il.DeclareLocal(method.ReturnType);
            }

            if (method.IsStatic || method.DeclaringType.IsValueType)
            {
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, method);
            }

            //设置参数
            if (isRef)
            {
                for (int i = 0; i < loc.Length; i++)
                {
                    var l = loc[i];
                    if (l == null)
                    {
                        continue;
                    }
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldloc, l);
                    if (l.LocalType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, l.LocalType);
                    }
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }

            if (ret == null)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else if (method.ReturnType.IsValueType)
            {
                il.Emit(OpCodes.Box, method.ReturnType);
            }
            else
            {
                il.Emit(OpCodes.Castclass, typeof(object));
            }

            il.Emit(OpCodes.Ret);

            return (Func<object, object, object[]>)dm.CreateDelegate(typeof(Func<object, object, object[]>));

        }

    }
}
