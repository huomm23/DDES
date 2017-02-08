using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DDES
{
    public class DType
    {
        public readonly Type Type;
        public static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        private static readonly Dictionary<Type, DType> _Cache = new Dictionary<Type, DType>();

        public Dictionary<string, DProperty> Properties { get; private set; }
        public Dictionary<PropertyInfo, DIndex> Indexes { get; private set; }
        public Dictionary<string, DEvent> Events { get; private set; }
        public Dictionary<MethodInfo, DMethod> Methods { get; private set; }

        public static DType Create(Type type)
        {

            DType dt = null;
            if (_Cache.TryGetValue(type, out dt))
            {
                return dt;
            }

            lock (_Cache)
            {
                if (_Cache.TryGetValue(type, out dt))
                {
                    return dt;
                }
                else
                {
                    dt = new DType(type);
                }
                _Cache[type] = dt;
                return dt;
            }
        }

        private DType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            Type = type;
            Properties = new Dictionary<string, DProperty>();
            Indexes = new Dictionary<PropertyInfo, DIndex>();
            Events = new Dictionary<string, DEvent>();
            Methods = new Dictionary<MethodInfo, DMethod>();

            var ps = Type.GetProperties(Flags);
            foreach (var p in ps)
            {
                if (p.GetIndexParameters().Length > 0)  // 所引起
                {
                    if (Indexes.ContainsKey(p) == false)
                    {
                        Indexes.Add(p, DIndex.Create(p));
                    }
                }
                else
                {
                    if (Properties.ContainsKey(p.Name) == false)
                    {
                        Properties.Add(p.Name, DProperty.Create(p));
                    }
                }
            }

            var es = Type.GetEvents(Flags);
            foreach (var e in es)
            {
                if (Events.ContainsKey(e.Name) == false)
                {
                    Events.Add(e.Name, DEvent.Create(e));
                }
            }

            var ms = type.GetMethods(Flags);
            foreach (var m in ms)
            {
                if (Methods.ContainsKey(m) == false)
                {
                    Methods.Add(m, DMethod.Create(m));
                }
            }
        }

        public DProperty FindProperty(string name)
        {
            DProperty v;
            if (Properties.TryGetValue(name, out v))
                return v;

            return null; ;
        }

        public DEvent FindEvent(string name)
        {
            DEvent v;
            if (Events.TryGetValue(name, out v))
                return v;

            return null; ;
        }

        public DIndex FindIndex(string name, Type[] paramTypes)
        {
            DIndex v = null;
            foreach (var item in Indexes)
            {
                if (item.Key.Name.Equals(name) == false)
                    continue;

                var ps = item.Key.GetIndexParameters();
                if (ps.Length != paramTypes.Length)
                    continue;

                var mathType = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (paramTypes[i].Equals(ps[i].ParameterType) == false)
                    {
                        mathType = false;
                        break;
                    }
                }

                if (mathType == false)
                    continue;

                v = item.Value;
            }

            return v;
        }

        public DMethod FindMethod(string name, Type[] paramTypes)
        {
            DMethod v = null;
            foreach (var item in Methods)
            {
                if (item.Key.Name.Equals(name) == false)
                    continue;

                var ps = item.Key.GetParameters();
                if (ps.Length != paramTypes.Length)
                    continue;

                var mathType = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (paramTypes[i].Equals(ps[i].ParameterType) == false)
                    {
                        mathType = false;
                        break;
                    }
                }

                if (mathType == false)
                    continue;

                v = item.Value;
            }

            return v;
        }
    }
}
