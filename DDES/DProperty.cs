using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DDES
{
    /// <summary>
    /// 属性要解决的问题：
    /// 空值类型问题
    /// 匿名属性问题
    /// 所引器干扰问题
    /// 事件干扰问题
    /// </summary>
    public class DProperty : AbstractDMember
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

        private Func<object, object> _Getter;
        private Action<object, object> _Setter;

        public PropertyInfo PropertyInfo { get; private set; }
        public Type PropertyType { get; private set; }
        public bool CanRead { get; private set; }
        public bool CanWrite { get; private set; }
        public bool IsPublic { get; private set; }
        public bool IsStatic { get; private set; }

        public bool IsNullable { get; private set; } // 是否是可空值类型
        public Type NullableType { get; private set; } // 可空值类型

        private DProperty(PropertyInfo pi)
            : base(pi)
        {
            PropertyInfo = pi;
            PropertyType = pi.PropertyType;
            CanRead = pi.CanRead;
            CanWrite = pi.CanWrite;
            IsPublic = pi.PropertyType.IsPublic;
            var nullable = System.Nullable.GetUnderlyingType(pi.PropertyType);
            if (nullable != null)
            {
                IsNullable = true;
                NullableType = nullable;
            }

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

            _Getter = EmitService.CreateGetter(pi);
            _Setter = EmitService.CreateSetter(pi);
        }

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
                return _Getter(obj);
            }
            catch (Exception ex)
            {
                var message = $"{PropertyInfo.ToString()}.{Name}属性取值失败";
                Trace.WriteLine(ex, message);
                throw new TargetInvocationException(message + ",原因见内部异常", ex);
            }
        }
    }
}
