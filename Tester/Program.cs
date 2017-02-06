using DDES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            User u = new User();
            CodeTimer.Initialize();
            CodeTimer.Time("MethodInfo", 1000000, () => GetName2(u));
            CodeTimer.Time("dynamic", 1000000, () => GetName3(u));
            CodeTimer.Time("fast ref", 1000000, () => GetName(u));
            CodeTimer.Time("direct", 1000000, () => GetName0(u));

        }

        static DProperty prop;

        public static object GetName(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (prop == null)
            {
                prop = DType.Create(obj.GetType()).Properties["Name"];
                if (prop == null) throw new NotSupportedException("对象不包含Name属性");
            }
            return prop.GetValue(obj);
        }

        static PropertyInfo pName;

        public static object GetName2(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (pName == null)
            {
                pName = typeof(User).GetProperty("Name");
            }
            return pName.GetValue(obj, null); //缓存了反射Name属性
        }

        public static object GetName3(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            return ((dynamic)obj).Name;
        }

        public static object GetName0(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            return ((User)obj).Name;
        }
    }


    public class SingleModel
    {
        public string Name { get; set; }
    }
    public class User
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime? Birthday { get; set; }
        public bool Sex { get; set; }
    }

}
