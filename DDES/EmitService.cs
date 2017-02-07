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

        public static Func<object, object[], object> CreateIndexGetter(PropertyInfo prop, Type owner = null)
        {
            if (prop == null)
            {
                return null;
            }
            var method = new DynamicMethod("", typeof(Object), new Type[] { typeof(Object), typeof(Object) }, owner ?? GetOwnerType(prop), true);
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

                var ps = prop.GetIndexParameters();
                var l = ps.Length;

                for (int i = 0; i < l; i++)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);

                    if (ps[i].ParameterType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, ps[i].ParameterType);
                    }
                }

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
            return (Func<object, object[], object>)method.CreateDelegate(typeof(Func<object, object[], object>));
        }

        public static Action<object, object,object[]> CreateIndexSetter(PropertyInfo prop, Type owner = null)
        {

            if (prop == null)
            {
                return null;
            }
            if (prop.DeclaringType.IsValueType)
            {
                throw new NotSupportedException("值类型无法通过方法给其属性或字段赋值");
            }
            var dm = new DynamicMethod("", null, new Type[] { typeof(object), typeof(object), typeof(object) }, owner ?? GetOwnerType(prop), true);
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

                var ps = prop.GetIndexParameters();
                var l = ps.Length;
                for (int i = 0; i < l; i++)
                {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);

                    if (ps[i].ParameterType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, ps[i].ParameterType);
                    }
                }

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

            return (Action<object, object,object[]>)dm.CreateDelegate(typeof(Action<object, object, object[]>));
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

    public static class PropertyAccessorGenerator
    {
        /// <summary>
        /// Creates a dynamic method for getting the value of the given property.
        /// </summary>
        /// <param name="propertyInfo">
        /// The instance of <see cref="PropertyInfo"/> from which the dynamic method would be created.
        /// </param>
        /// <returns>
        /// A dynamic method for getting the value of the given property.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyInfo"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// The property is an indexer.
        /// -or-
        /// The get accessor method from <paramref name="propertyInfo"/> cannot be retrieved.
        /// </exception>
        public static Func<object, object> CreateGetter(PropertyInfo propertyInfo)
        {
            return CreateGetter(propertyInfo, true);
        }

        /// <summary>
        /// Creates a dynamic method for getting the value of the given property.
        /// </summary>
        /// <param name="propertyInfo">
        /// The instance of <see cref="PropertyInfo"/> from which the dynamic method would be created.
        /// </param>
        /// <param name="nonPublic">
        /// Indicates whether to use the non-public property getter method.
        /// </param>
        /// <returns>
        /// A dynamic method for getting the value of the given property.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyInfo"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// The property is an indexer.
        /// -or-
        /// The get accessor method from <paramref name="propertyInfo"/> cannot be retrieved.
        /// </exception>
        public static Func<object, object> CreateGetter(PropertyInfo propertyInfo, bool nonPublic)
        {
            return CreateGetter<object, object>(propertyInfo, nonPublic);
        }

        /// <summary>
        /// Creates a dynamic method for getting the value of the given property.
        /// </summary>
        /// <typeparam name="TSource">The type of the intance from which to get the value.</typeparam>
        /// <typeparam name="TRet">The type of the return value.</typeparam>
        /// <param name="propertyInfo">
        /// The instance of <see cref="PropertyInfo"/> from which the dynamic method would be created.
        /// </param>
        /// <param name="nonPublic">
        /// Indicates whether to use the non-public property getter method.
        /// </param>
        /// <returns>
        /// A dynamic method for getting the value of the given property.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyInfo"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// The property is an indexer.
        /// -or -
        /// The get accessor method from <paramref name="propertyInfo"/> cannot be retrieved.
        /// -or-
        /// <typeparamref name="TSource"/> is not <see cref="object"/>, and from which 
        /// the declaring type the property is not assignable.
        /// -or-
        /// <typeparamref name="TRet"/> is not assignable from the property type.
        /// </exception>
        public static Func<TSource, TRet> CreateGetter<TSource, TRet>(PropertyInfo propertyInfo, bool nonPublic)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");

            if (propertyInfo.GetIndexParameters().Length > 0)
            {
                throw new ArgumentException(
                   "Cannot create a dynamic getter for an indexed property.",
                   "propertyInfo");
            }

            if (typeof(TSource) != typeof(object)
                && !propertyInfo.DeclaringType.IsAssignableFrom(typeof(TSource)))
            {
                throw new ArgumentException(
                   "The declaring type of the property is not assignable from the type of the instance.",
                   "propertyInfo");
            }

            if (!typeof(TRet).IsAssignableFrom(propertyInfo.PropertyType))
            {
                throw new ArgumentException(
                    "The type of the return value is not assignable from the type of the property.",
                    "propertyInfo");
            }

            //the method call of the get accessor method fails in runtime 
            //if the declaring type of the property is an interface and TSource is a value type, 
            //in this case, we should find the property from TSource whose DeclaringType is TSource itself
            if (typeof(TSource).IsValueType && propertyInfo.DeclaringType.IsInterface)
            {
                propertyInfo = typeof(TSource).GetProperty(propertyInfo.Name);
            }

            var getMethod = propertyInfo.GetGetMethod(nonPublic);
            if (getMethod == null)
            {
                if (nonPublic)
                {
                    throw new ArgumentException(
                        "The property does not have a get method.", "propertyInfo");
                }

                throw new ArgumentException(
                    "The property does not have a public get method.", "propertyInfo");
            }

            return EmitPropertyGetter<TSource, TRet>(propertyInfo, getMethod);
        }

        /// <summary>
        /// Creates a dynamic method for setting the value of the given property.
        /// </summary>
        /// <param name="propertyInfo">
        /// The instance of <see cref="PropertyInfo"/> from which the dynamic method would be created.
        /// </param>
        /// <returns>
        /// A dynamic method for setting the value of the given property.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyInfo"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// The property is an indexer.
        /// -or-
        /// The set accessor method from the <paramref name="propertyInfo"/> cannot be retrieved.
        /// </exception>
        /// <remarks>
        /// In order to set a property value on a value type succesfully, the value type must be boxed 
        /// in and <see cref="object"/>, and unboxed from the object after the dynamic
        /// set mothod is called, e.g.
        /// <code>
        ///   object boxedStruct = new SomeStruct();
        ///   setter(s, "the value");
        ///   SomeStruct unboxedStruct = (SomeStruct)boxedStruct;
        /// </code>
        /// </remarks>
        public static Action<object, object> CreateSetter(PropertyInfo propertyInfo)
        {
            return CreateSetter(propertyInfo, true);
        }

        /// <summary>
        /// Creates a dynamic method for setting the value of the given property.
        /// </summary>
        /// <param name="propertyInfo">
        /// The instance of <see cref="PropertyInfo"/> from which the dynamic method would be created.
        /// </param>
        /// <param name="nonPublic">
        /// Indicates whether to use the non-public property setter method.
        /// </param>
        /// <returns>
        /// A dynamic method for setting the value of the given property.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyInfo"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// The property is an indexer.
        /// -or-
        /// The set accessor method from the <paramref name="propertyInfo"/> cannot be retrieved.
        /// </exception>
        /// <remarks>
        /// In order to set a property value on a value type succesfully, the value type must be boxed 
        /// in and <see cref="object"/>, and unboxed from the object after the dynamic
        /// set mothod is called, e.g.
        /// <code>
        ///   object boxedStruct = new SomeStruct();
        ///   setter(s, "the value");
        ///   SomeStruct unboxedStruct = (SomeStruct)boxedStruct;
        /// </code>
        /// </remarks>
        public static Action<object, object> CreateSetter(PropertyInfo propertyInfo, bool nonPublic)
        {
            return CreateSetter<object, object>(propertyInfo, nonPublic);
        }

        /// <summary>
        /// Creates a dynamic method for setting the value of the given property.
        /// </summary>
        /// <typeparam name="TTarget">The type of the instance the property belongs to.</typeparam>
        /// <typeparam name="TValue">The type of the value to set.</typeparam>
        /// <param name="propertyInfo">
        /// The instance of <see cref="PropertyInfo"/> from which the dynamic method would be created.
        /// </param>
        /// <param name="nonPublic">
        /// Indicates whether to use the non-public property setter method.
        /// </param>
        /// <returns>
        /// A dynamic method for setting the value of the given property.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyInfo"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// The property is an indexer.
        /// -or-
        /// The set accessor method from the <paramref name="propertyInfo"/> cannot be retrieved.
        /// -or-
        /// <typeparamref name="TTarget"/> is a value type.
        /// -or-
        /// <typeparamref name="TTarget"/> is not <see cref="object"/>, and from which 
        /// the declaring type of <paramref name="propertyInfo"/> is not assignable.
        /// -or-
        /// <typeparamref name="TValue"/> is not <see cref="object"/>, and the type of property 
        /// is not assignable from <typeparamref name="TValue"/>. 
        /// </exception>
        /// <remarks>
        /// In order to set a property value on a value type succesfully, the value type must be boxed 
        /// in and <see cref="object"/>, and unboxed from the object after the dynamic
        /// set mothod is called, e.g.
        /// <code>
        ///   object boxedStruct = new SomeStruct();
        ///   setter(s, "the value");
        ///   SomeStruct unboxedStruct = (SomeStruct)boxedStruct;
        /// </code>
        /// </remarks>
        public static Action<TTarget, TValue> CreateSetter<TTarget, TValue>(PropertyInfo propertyInfo, bool nonPublic)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");

            if (typeof(TTarget).IsValueType)
            {
                throw new ArgumentException(
                   "The type of the isntance should not be a value type. " +
                   "For a value type, use System.Object instead.",
                   "propertyInfo");
            }

            if (propertyInfo.GetIndexParameters().Length > 0)
            {
                throw new ArgumentException(
                   "Cannot create a dynamic setter for an indexed property.",
                   "propertyInfo");
            }

            if (typeof(TTarget) != typeof(object)
                && !propertyInfo.DeclaringType.IsAssignableFrom(typeof(TTarget)))
            {
                throw new ArgumentException(
                   "The declaring type of the property is not assignable from the type of the isntance.",
                   "propertyInfo");
            }

            if (typeof(TValue) != typeof(object)
                && !propertyInfo.PropertyType.IsAssignableFrom(typeof(TValue)))
            {
                throw new ArgumentException(
                    "The type of the property is not assignable from the type of the value.",
                    "propertyInfo");
            }

            var setMethod = propertyInfo.GetSetMethod(nonPublic);
            if (setMethod == null)
            {
                if (nonPublic)
                {
                    throw new ArgumentException(
                        "The property does not have a set method.", "propertyInfo");
                }

                throw new ArgumentException(
                    "The property does not have a public set method.", "propertyInfo");
            }

            return EmitPropertySetter<TTarget, TValue>(propertyInfo, setMethod);
        }

        private static Func<TSource, TReturn> EmitPropertyGetter<TSource, TReturn>(
            PropertyInfo propertyInfo, MethodInfo getMethod)
        {
            var dynamicMethod = EmitUtils.CreateDynamicMethod(
                "$Get" + propertyInfo.Name,
                typeof(TReturn),
                new[] { typeof(TSource) },
                propertyInfo.DeclaringType);
            var il = dynamicMethod.GetILGenerator();

            if (!getMethod.IsStatic)
            {
                //unbox the input value if needed
                if (typeof(TSource).IsValueType)
                {
                    il.Ldarga_S(0);
                }
                else
                {
                    il.Ldarg_0();
                    il.CastReference(propertyInfo.DeclaringType);
                }
            }

            il.CallMethod(getMethod);

            //box the return value if needed
            if (!typeof(TReturn).IsValueType && propertyInfo.PropertyType.IsValueType)
            {
                il.Box(propertyInfo.PropertyType);
            }

            il.Ret();

            return (Func<TSource, TReturn>)dynamicMethod.CreateDelegate(typeof(Func<TSource, TReturn>));
        }

        private static Action<TTarget, TValue> EmitPropertySetter<TTarget, TValue>(
            PropertyInfo propertyInfo, MethodInfo setMethod)
        {
            var propType = propertyInfo.PropertyType;
            var declaringType = propertyInfo.DeclaringType;
            var dynamicMethod = EmitUtils.CreateDynamicMethod(
                "$Set" + propertyInfo.Name,
                null,
                new[] { typeof(TTarget), typeof(TValue) },
                declaringType);
            var il = dynamicMethod.GetILGenerator();

            //copy the value to a local variable, unbox if needed
            il.DeclareLocal(propType);
            il.Ldarg_1();
            if (!typeof(TValue).IsValueType)
            {
                il.CastValue(propType);
            }
            il.Stloc_0();

            //push the instance, unbox if needed
            if (!setMethod.IsStatic)
            {
                il.Ldarg_0();
                il.CastReference(declaringType);
            }

            //push the value and call the method
            il.Ldloc_0();
            il.CallMethod(setMethod);
            il.Ret();

            return (Action<TTarget, TValue>)dynamicMethod.CreateDelegate(typeof(Action<TTarget, TValue>));
        }
    }

    public static class MethodInvokerGenerator
    {
        /// <summary>
        /// Creates a dynamic method for invoking the method from the given <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">
        /// The instance of <see cref="MemberInfo"/> from which the dyanmic method is to be created.
        /// </param>
        /// <returns>
        /// The delegate has two parameters: the first for the object instance (will be ignored 
        /// if the method is static), and the second for the arguments of the method (will be 
        /// ignored if the method has no arguments)/
        /// The return value of the delegate will be <c>null</c> if the method has no return value.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="methodInfo"/> is null.</exception>
        public static Func<object, object[], object> CreateDelegate(MethodInfo methodInfo)
        {
            return CreateDelegate(methodInfo, true);
        }

        /// <summary>
        /// Creates a dynamic method for invoking the method from the given <see cref="MethodInfo"/>
        /// and indicates whether to perform a arguments validation in the dynamic method.
        /// </summary>
        /// <param name="methodInfo">
        /// The instance of <see cref="MemberInfo"/> from which the dyanmic method is to be created.
        /// </param>
        /// <param name="validateArguments">
        /// If <c>true</c>, the dynamic method will validate if the instance or the array of arguments 
        /// is null and check the length of the array to avoid the exceptions such as 
        /// <see cref="NullReferenceException"/> or <see cref="IndexOutOfRangeException"/>,
        /// an <see cref="ArgumentNullException"/> or <see cref="ArgumentException"/> will be thrown instead.
        /// </param>
        /// <returns>
        /// The delegate has two parameters: the first for the object instance (will be ignored 
        /// if the method is static), and the second for the arguments of the method (will be 
        /// ignored if the method has no arguments)/
        /// The return value of the delegate will be <c>null</c> if the method has no return value.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="methodInfo"/> is null.</exception>
        public static Func<object, object[], object> CreateDelegate(
            MethodInfo methodInfo, bool validateArguments)
        {
            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            var args = methodInfo.GetParameters();
            var dynamicMethod = EmitUtils.CreateDynamicMethod(
                "$Call" + methodInfo.Name,
                typeof(object),
                new[] { typeof(object), typeof(object[]) },
                methodInfo.DeclaringType);
            var il = dynamicMethod.GetILGenerator();

            var lableValidationCompleted = il.DefineLabel();
            if (!validateArguments || (methodInfo.IsStatic && args.Length == 0))
            {
                il.Br_S(lableValidationCompleted); //does not need validation
            }
            else
            {
                var lableCheckArgumentsRef = il.DefineLabel();
                var lableCheckArgumentsLength = il.DefineLabel();

                //check if the instance is null
                if (!methodInfo.IsStatic)
                {
                    // if (instance == null) throw new ArgumentNullExcpeiton("instance");
                    il.Ldarg_0();
                    il.Brtrue_S(args.Length > 0 ? lableCheckArgumentsRef : lableValidationCompleted);

                    il.ThrowArgumentsNullExcpetion("instance");
                }

                //check the arguments
                if (args.Length > 0)
                {
                    // if (arguments == null) throw new ArgumentNullExcpeiton("arguments");
                    il.MarkLabel(lableCheckArgumentsRef);
                    il.Ldarg_1();
                    il.Brtrue_S(lableCheckArgumentsLength);

                    il.ThrowArgumentsNullExcpetion("arguments");

                    // if (arguments.Length < $(args.Length)) throw new ArgumentExcpeiton(msg, "arguments");
                    il.MarkLabel(lableCheckArgumentsLength);
                    il.Ldarg_1();
                    il.Ldlen();
                    il.Conv_I4();
                    il.LoadInt32(args.Length);
                    il.Bge_S(lableValidationCompleted);

                    il.ThrowArgumentsExcpetion("Not enough arguments in the argument array.", "arguments");
                }
            }

            il.MarkLabel(lableValidationCompleted);
            if (!methodInfo.IsStatic)
            {
                il.Ldarg_0();
                il.CastReference(methodInfo.DeclaringType);
            }

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    il.Ldarg_1();
                    il.LoadInt32((short)i);
                    il.Ldelem_Ref();
                    il.CastValue(args[i].ParameterType);
                }
            }

            il.CallMethod(methodInfo);
            if (methodInfo.ReturnType == typeof(void))
            {
                il.Ldc_I4_0(); //return null
            }
            else
            {
                il.BoxIfNeeded(methodInfo.ReturnType);
            }
            il.Ret();

            var methodDelegate = dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
            return (Func<object, object[], object>)methodDelegate;
        }
    }


    public static class EmitUtils
    {
        /// <summary>
        /// Creates an instance of <see cref="DynamicMethod"/>.
        /// </summary>
        /// <param name="methodName">The name of the dynamic method.</param>
        /// <param name="returnType">
        /// The return type of the dynamic method, null if the method has no return type.
        /// </param>
        /// <param name="parameterTypes">
        /// An array of <see cref="Type"/> specifying the types of the parameters of the dynamic method, 
        /// or null if the method has no parameters. 
        /// </param>
        /// <param name="owner">
        /// Specifies with which type the dynamic method is to be logically associated,
        /// if the type is an interface, the dynamic method will be associated to the module.
        /// </param>
        public static DynamicMethod CreateDynamicMethod(
            string methodName, Type returnType, Type[] parameterTypes, Type owner)
        {
            var dynamicMethod = owner.IsInterface ?
                new DynamicMethod(methodName, returnType, parameterTypes, owner.Module, true) :
                new DynamicMethod(methodName, returnType, parameterTypes, owner, true);

            return dynamicMethod;
        }

        /// <summary>
        /// Performs a boxing operation if the given type is a value type.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="type">The type.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator BoxIfNeeded(this ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                il.Box(type);
            }

            return il;
        }

        /// <summary>
        /// Performs a unboxing operation (unbox.any) if the given type is a value type.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="type">The type.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator UnBoxIfNeeded(this ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                il.Unbox_Any(type);
            }

            return il;
        }

        /// <summary>
        /// Calls the method indicated by the passed method descriptor.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="methodInfo">The method descriptor.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator CallMethod(this ILGenerator il, MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            if (methodInfo.IsVirtual)
            {
                il.Callvirt(methodInfo);
            }
            else
            {
                il.Call(methodInfo);
            }

            return il;
        }

        /// <summary>
        /// Casts an object or value type passed by reference to the specified type
        /// and pushes the result onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="targetType">The type.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator CastValue(this ILGenerator il, Type targetType)
        {
            il.Unbox_Any(targetType);
            return il;
        }

        /// <summary>
        /// Casts an object or value type passed by reference to the specified type
        /// and pushes the object reference or the value type pointer of the result
        /// onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="targetType">The type.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator CastReference(this ILGenerator il, Type targetType)
        {
            if (targetType.IsValueType)
            {
                il.Unbox(targetType);
            }
            else
            {
                il.Castclass(targetType);
            }

            return il;
        }

        /// <summary>
        /// Loads the argument at the specified index onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator LoadArgument(this ILGenerator il, short index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "The index should not be less than zero.");

            switch (index)
            {
                case 0: il.Ldarg_0(); break;
                case 1: il.Ldarg_1(); break;
                case 2: il.Ldarg_2(); break;
                case 3: il.Ldarg_3(); break;

                default:
                    if (index <= byte.MaxValue)
                    {
                        il.Ldarg_S((byte)index);
                    }
                    else
                    {
                        il.Ldarg(index);
                    }
                    break;
            }

            return il;
        }

        /// <summary>
        /// Loads the address of the argument at the specified index onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index"></param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator LoadArgumentAddress(this ILGenerator il, short index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "The index should not be less than zero.");

            if (index <= byte.MaxValue)
            {
                il.Ldarga_S((byte)index);
            }
            else
            {
                il.Ldarga(index);
            }

            return il;
        }

        /// <summary>
        /// Pushes the integer value onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="i">The integer value.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator LoadInt32(this ILGenerator il, int i)
        {
            switch (i)
            {
                case -1: il.Ldc_I4_M1(); break;
                case 0: il.Ldc_I4_0(); break;
                case 1: il.Ldc_I4_1(); break;
                case 2: il.Ldc_I4_2(); break;
                case 3: il.Ldc_I4_3(); break;
                case 4: il.Ldc_I4_4(); break;
                case 5: il.Ldc_I4_5(); break;
                case 6: il.Ldc_I4_6(); break;
                case 7: il.Ldc_I4_7(); break;
                case 8: il.Ldc_I4_8(); break;

                default:
                    if (i <= byte.MaxValue)
                    {
                        il.Ldc_I4_S((byte)i);
                    }
                    else
                    {
                        il.Ldc_I4(i);
                    }
                    break;
            }

            return il;
        }

        /// <summary>
        /// Loads the local variable at the specified index onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index of the local variable.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator LoadLocalVariable(this ILGenerator il, short index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "The index should not be less than zero.");

            switch (index)
            {
                case 0: il.Ldloc_0(); break;
                case 1: il.Ldloc_1(); break;
                case 2: il.Ldloc_2(); break;
                case 3: il.Ldloc_3(); break;

                default:
                    if (index <= byte.MaxValue)
                    {
                        il.Ldloc_S((byte)index);
                    }
                    else
                    {
                        il.Ldloc(index);
                    }
                    break;
            }

            return il;
        }

        /// <summary>
        /// Loads the address of the local variable at a specific index onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index of the local variable.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator LoadLocalVariableAddress(this ILGenerator il, short index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "The index should not be less than zero.");

            if (index <= byte.MaxValue)
            {
                il.Ldloca_S((byte)index);
            }
            else
            {
                il.Ldloca(index);
            }

            return il;
        }

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="paramName">The parameter name used to initialize the exception instance.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator ThrowArgumentsNullExcpetion(this ILGenerator il, string paramName)
        {
            il.Ldstr(paramName);
            il.Newobj(typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) }));
            il.Throw();

            return il;
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/>, specifying the message and the parameter name.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="message">The message.</param>
        /// <param name="paramName">The parameter name.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator ThrowArgumentsExcpetion(this ILGenerator il, string message, string paramName)
        {
            il.Ldstr(message);
            il.Ldstr(paramName);
            il.Newobj(typeof(ArgumentException).GetConstructor(new[] { typeof(string), typeof(string) }));
            il.Throw();

            return il;
        }
    }

    /// <summary>
    /// Provides a set of extention methods for <see cref="ILGenerator"/>
    /// for emitting the <see cref="OpCode"/>s.
    /// </summary>
    public static class OpCodeExtention
    {
        /// <summary>
        /// Transfers control to a target instruction if the first value is greater than
        /// or equal to the second value.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="label">The label.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Bge(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Bge, label);
            return il;
        }

        /// <summary>
        /// Transfers control to a target instruction (short form) if the first value 
        /// is greater than or equal to the second value.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="label">The label.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Bge_S(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Bge_S, label);
            return il;
        }

        /// <summary>
        /// Converts a value type to an object reference (type O).
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="type">The value type.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Box(this ILGenerator il, Type type)
        {
            il.Emit(OpCodes.Box, type);
            return il;
        }

        /// <summary>
        /// Unconditionally transfers control to a target instruction.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="label">The label.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Br(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Br, label);
            return il;
        }

        /// <summary>
        /// Unconditionally transfers control to a target instruction (short form).
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="label">The label.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Br_S(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Br_S, label);
            return il;
        }

        /// <summary>
        /// Transfers control to a target instruction if value is <c>false</c>, 
        /// a null reference, or zero. 
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="label">The label.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Brfalse(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Brfalse, label);
            return il;
        }

        /// <summary>
        /// Transfers control to a target instruction (short form) if value is <c>false</c>, 
        /// a null reference, or zero. 
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="label">The label.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Brfalse_S(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Brfalse_S, label);
            return il;
        }

        /// <summary>
        /// Transfers control to a target instruction if value is <c>true</c>, 
        /// not null, or non-zero.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="label">The label.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Brtrue(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Brtrue, label);
            return il;
        }

        /// <summary>
        /// Transfers control to a target instruction (short form) if value is <c>true</c>, 
        /// not null, or non-zero.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="label">The label.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Brtrue_S(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Brtrue_S, label);
            return il;
        }

        /// <summary>
        /// Calls the method indicated by the passed method descriptor.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="methodInfo">The mechod to call.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Call(this ILGenerator il, MethodInfo methodInfo)
        {
            il.Emit(OpCodes.Call, methodInfo);
            return il;
        }

        /// <summary>
        /// Calls a late-bound method on an object, pushing the return value onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="methodInfo">The mechod to call.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Callvirt(this ILGenerator il, MethodInfo methodInfo)
        {
            il.Emit(OpCodes.Callvirt, methodInfo);
            return il;
        }

        /// <summary>
        /// Attempts to cast an object passed by reference to the specified class.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="type">The target class.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Castclass(this ILGenerator il, Type type)
        {
            il.Emit(OpCodes.Castclass, type);
            return il;
        }

        /// <summary>
        /// Converts the value on top of the evaluation stack to native int.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Conv_I(this ILGenerator il)
        {
            il.Emit(OpCodes.Conv_I);
            return il;
        }

        /// <summary>
        /// Converts the value on top of the evaluation stack to int8, then extends (pads) it to int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Conv_I1(this ILGenerator il)
        {
            il.Emit(OpCodes.Conv_I1);
            return il;
        }

        /// <summary>
        /// Converts the value on top of the evaluation stack to int16, then extends (pads) it to int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Conv_I2(this ILGenerator il)
        {
            il.Emit(OpCodes.Conv_I2);
            return il;
        }

        /// <summary>
        /// Converts the value on top of the evaluation stack to int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Conv_I4(this ILGenerator il)
        {
            il.Emit(OpCodes.Conv_I4);
            return il;
        }

        /// <summary>
        /// Converts the value on top of the evaluation stack to int64.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Conv_I8(this ILGenerator il)
        {
            il.Emit(OpCodes.Conv_I8);
            return il;
        }

        /// <summary>
        /// Initializes each field of the value type at a specified address to a null reference 
        /// or a 0 of the appropriate primitive type.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="type">The value type.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Initobj(this ILGenerator il, Type type)
        {
            il.Emit(OpCodes.Initobj, type);
            return il;
        }

        /// <summary>
        /// Loads the argument at index 0 onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldarg_0(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_0);
            return il;
        }

        /// <summary>
        /// Loads the argument at index 1 onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldarg_1(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_1);
            return il;
        }

        /// <summary>
        /// Loads the argument at index 2 onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldarg_2(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_2);
            return il;
        }

        /// <summary>
        /// Loads the argument at index 3 onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldarg_3(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_3);
            return il;
        }

        /// <summary>
        /// Loads the argument (referenced by a specified short form index) onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldarg_S(this ILGenerator il, byte index)
        {
            il.Emit(OpCodes.Ldarg_S, index);
            return il;
        }

        /// <summary>
        /// Loads an argument (referenced by a specified index value) onto the stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldarg(this ILGenerator il, short index)
        {
            il.Emit(OpCodes.Ldarg, index);
            return il;
        }

        /// <summary>
        /// Load an argument address, in short form, onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldarga_S(this ILGenerator il, byte index)
        {
            il.Emit(OpCodes.Ldarga_S, index);
            return il;
        }

        /// <summary>
        /// Load an argument address onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldarga(this ILGenerator il, short index)
        {
            il.Emit(OpCodes.Ldarga, index);
            return il;
        }

        /// <summary>
        /// Finds the value of a field in the object whose reference is currently on the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="fieldInfo">The target field.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldfld(this ILGenerator il, FieldInfo fieldInfo)
        {
            il.Emit(OpCodes.Ldfld, fieldInfo);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of -1 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_M1(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_M1);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of 0 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_0(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_0);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of 1 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_1(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_1);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of 2 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_2(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_2);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of 3 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_3(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_3);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of 4 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_4(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_4);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of 5 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_5(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_5);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of 6 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_6(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_6);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of 7 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_7(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_7);
            return il;
        }

        /// <summary>
        /// Pushes the integer value of 8 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_8(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_8);
            return il;
        }

        /// <summary>
        /// Pushes the supplied int8 value onto the evaluation stack as an int32, short form.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="value">The value.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4_S(this ILGenerator il, byte value)
        {
            il.Emit(OpCodes.Ldc_I4_S, value);
            return il;
        }

        /// <summary>
        /// Pushes a supplied value of type int32 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="value">The value.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I4(this ILGenerator il, int value)
        {
            il.Emit(OpCodes.Ldc_I4, value);
            return il;
        }

        /// <summary>
        /// Pushes a supplied value of type int64 onto the evaluation stack as an int64.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="value">The value.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_I8(this ILGenerator il, long value)
        {
            il.Emit(OpCodes.Ldc_I8, value);
            return il;
        }

        /// <summary>
        /// Pushes a supplied value of type float32 onto the evaluation stack as type F (float).
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="value">The value.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_R4(this ILGenerator il, float value)
        {
            il.Emit(OpCodes.Ldc_R4, value);
            return il;
        }

        /// <summary>
        /// Pushes a supplied value of type float64 onto the evaluation stack as type F (float).
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="value">The value.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldc_R8(this ILGenerator il, double value)
        {
            il.Emit(OpCodes.Ldc_R8, value);
            return il;
        }

        /// <summary>
        /// Loads the element containing an object reference at a specified array index onto 
        /// the top of the evaluation stack as type O (object reference).
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldelem_Ref(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldelem_Ref);
            return il;
        }

        /// <summary>
        /// Pushes the number of elements of a zero-based, one-dimensional array onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldlen(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldlen);
            return il;
        }

        /// <summary>
        /// Loads the local variable at index 0 onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloc_0(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldloc_0);
            return il;
        }

        /// <summary>
        /// Loads the local variable at index 1 onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloc_1(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldloc_1);
            return il;
        }

        /// <summary>
        /// Loads the local variable at index 2 onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloc_2(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldloc_2);
            return il;
        }

        /// <summary>
        /// Loads the local variable at index 3 onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloc_3(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldloc_3);
            return il;
        }

        /// <summary>
        /// Loads the local variable at a specific index onto the evaluation stack, short form.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloc_S(this ILGenerator il, byte index)
        {
            il.Emit(OpCodes.Ldloc_S, index);
            return il;
        }

        /// <summary>
        /// Loads the local variable at a specific index onto the evaluation stack, short form.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="localBuilder">The instance of <see cref="LocalBuilder"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloc_S(this ILGenerator il, LocalBuilder localBuilder)
        {
            il.Emit(OpCodes.Ldloc_S, localBuilder);
            return il;
        }

        /// <summary>
        /// Loads the local variable at a specific index onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloc(this ILGenerator il, short index)
        {
            il.Emit(OpCodes.Ldloc, index);
            return il;
        }

        /// <summary>
        /// Loads the local variable at a specific index onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="localBuilder">The instance of <see cref="LocalBuilder"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloc(this ILGenerator il, LocalBuilder localBuilder)
        {
            il.Emit(OpCodes.Ldloc, localBuilder);
            return il;
        }

        /// <summary>
        /// Loads the address of the local variable at a specific index onto
        /// the evaluation stack, short form.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloca_S(this ILGenerator il, byte index)
        {
            il.Emit(OpCodes.Ldloca_S, index);
            return il;
        }

        /// <summary>
        /// Loads the address of the local variable at a specific index onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldloca(this ILGenerator il, short index)
        {
            il.Emit(OpCodes.Ldloca, index);
            return il;
        }

        /// <summary>
        /// Pushes the value of a static field onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="fieldInfo">The static field.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldsfld(this ILGenerator il, FieldInfo fieldInfo)
        {
            il.Emit(OpCodes.Ldsfld, fieldInfo);
            return il;
        }

        /// <summary>
        /// Pushes a new object reference to a string literal stored in the metadata.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="value">The string.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ldstr(this ILGenerator il, string value)
        {
            il.Emit(OpCodes.Ldstr, value);
            return il;
        }

        /// <summary>
        /// Creates a new object or a new instance of a value type, pushing an object reference (type O) 
        /// onto the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="constructorInfo">The constructor of the type.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Newobj(this ILGenerator il, ConstructorInfo constructorInfo)
        {
            il.Emit(OpCodes.Newobj, constructorInfo);
            return il;
        }

        /// <summary>
        /// Returns from the current method, pushing a return value (if present) from 
        /// the callee's evaluation stack onto the caller's evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Ret(this ILGenerator il)
        {
            il.Emit(OpCodes.Ret);
            return il;
        }

        /// <summary>
        /// Replaces the value stored in the field of an object reference or pointer with a new value.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="fieldInfo">The target field.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stfld(this ILGenerator il, FieldInfo fieldInfo)
        {
            il.Emit(OpCodes.Stfld, fieldInfo);
            return il;
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and stores it 
        /// in a the local variable list at index 0.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stloc_0(this ILGenerator il)
        {
            il.Emit(OpCodes.Stloc_0);
            return il;
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and stores it 
        /// in a the local variable list at index 1.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stloc_1(this ILGenerator il)
        {
            il.Emit(OpCodes.Stloc_1);
            return il;
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and stores it 
        /// in a the local variable list at index 2.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stloc_2(this ILGenerator il)
        {
            il.Emit(OpCodes.Stloc_2);
            return il;
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and stores it 
        /// in a the local variable list at index 3.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stloc_3(this ILGenerator il)
        {
            il.Emit(OpCodes.Stloc_3);
            return il;
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and stores it 
        /// in a the local variable list at index (short form).
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stloc_S(this ILGenerator il, byte index)
        {
            il.Emit(OpCodes.Stloc_S, index);
            return il;
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and stores it 
        /// in a the local variable list at index (short form).
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="localBuilder">The instance of <see cref="LocalBuilder"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stloc_S(this ILGenerator il, LocalBuilder localBuilder)
        {
            il.Emit(OpCodes.Stloc_S, localBuilder);
            return il;
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and stores it 
        /// in a the local variable list at a specified index.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stloc(this ILGenerator il, short index)
        {
            il.Emit(OpCodes.Stloc, index);
            return il;
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and stores it 
        /// in a the local variable list at a specified index.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="localBuilder">The instance of <see cref="LocalBuilder"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stloc(this ILGenerator il, LocalBuilder localBuilder)
        {
            il.Emit(OpCodes.Stloc, localBuilder);
            return il;
        }

        /// <summary>
        /// Replaces the value of a static field with a value from the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="fieldInfo">The static field.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Stsfld(this ILGenerator il, FieldInfo fieldInfo)
        {
            il.Emit(OpCodes.Stsfld, fieldInfo);
            return il;
        }

        /// <summary>
        /// Throws the exception object currently on the evaluation stack.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Throw(this ILGenerator il)
        {
            il.Emit(OpCodes.Throw);
            return il;
        }

        /// <summary>
        /// Converts the boxed representation of a value type to its unboxed form.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="type">The value type.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Unbox(this ILGenerator il, Type type)
        {
            il.Emit(OpCodes.Unbox, type);
            return il;
        }

        /// <summary>
        /// Converts the boxed representation of a type specified 
        /// in the instruction to its unboxed form.
        /// </summary>
        /// <param name="il">The instance of <see cref="ILGenerator"/>.</param>
        /// <param name="type">The type.</param>
        /// <returns>The instance of <see cref="ILGenerator"/>.</returns>
        public static ILGenerator Unbox_Any(this ILGenerator il, Type type)
        {
            il.Emit(OpCodes.Unbox_Any, type);
            return il;
        }
    }
}
