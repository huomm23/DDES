using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DDES
{
    public class DEvent : AbstractDMember
    {
        private static readonly Dictionary<EventInfo, DEvent> _Cache = new Dictionary<EventInfo, DEvent>();

        public static DEvent Create(EventInfo ei)
        {
            DEvent de;
            if (_Cache.TryGetValue(ei, out de))
            {
                return de;
            }

            lock (_Cache)
            {
                if (_Cache.TryGetValue(ei, out de))
                {
                    return de;
                }
                else
                {
                    de = new DEvent(ei);
                }
                _Cache[ei] = de;
                return de;
            }
        }

        public EventInfo EventInfo { get; private set; }
        public Type EventHandlerType { get; private set; }
        public bool CanRead { get; private set; }
        public bool CanWrite { get; private set; }
        public bool IsStatic { get; private set; }

        private DEvent(EventInfo ei)
            :base(ei)
        {
            EventInfo = ei;
            EventHandlerType = ei.EventHandlerType;
        }

        public void AddEvent()
        { }

        public void RemoveEvent()
        { }

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

        private static Type GetOwnerType(MemberInfo mi)
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
    }
}
