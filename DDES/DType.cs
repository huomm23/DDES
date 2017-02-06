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

        private static readonly Dictionary<Type, DType> _Cache = new Dictionary<Type, DType>();

        public Dictionary<string, DProperty> Properties { get; private set; }

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
            foreach (var p in Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (p.GetIndexParameters().Length == 0) //排除索引器
                {
                    if (!Properties.ContainsKey(p.Name))
                    {
                        Properties.Add(p.Name, DProperty.Create(p));
                    }
                }
            }
        }
    }
}
