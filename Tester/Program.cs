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
        public class IT
        {
            public string N { get; set; }
        }
        public class T : IT
        {
            public string M { get; set; }
        }
        static void Main(string[] args)
        {
            var t = new { A = 213 };
            var pi = t.GetType().GetProperty("A");
            User u = new User();
            CodeTimer.Initialize();
            //DProperty prop = DType.Create(u.GetType()).Properties["Name"];

            CodeTimer.Time("MethodInfo", 1000000, () => GetName2(u));
            CodeTimer.Time("dynamic", 1000000, () => GetName3(u));
            CodeTimer.Time("fastprop", 1000000, () => GetName(u));
            CodeTimer.Time("directprop", 1000000, () => GetName0(u));
            CodeTimer.Time("fastindex", 1000000, () => GetIndex(u));
            CodeTimer.Time("directindex", 1000000, () => GetIndex0(u));

        }

        static DProperty prop;
        static DProperty index;

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

        public static object GetIndex(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (index == null)
            {
                index = DType.Create(obj.GetType()).Properties["Item"];
                if (index == null) throw new NotSupportedException("对象不包含Name属性");
            }
            return index.GetValue(obj, new object[] { 0 });
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

        public static object GetIndex0(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            return ((User)obj)[0];
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
        public string this[int index]
        {
            get
            {
                if (dict.ContainsKey(index))
                    return dict[index];

                return null;
            }
            set { }
        }
        private Dictionary<int, string> dict = new Dictionary<int, string>();
    }

}
