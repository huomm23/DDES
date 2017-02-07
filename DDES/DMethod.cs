using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DDES
{
    public class DMethod : AbstractDMember
    {
        private static readonly Dictionary<MethodInfo, DMethod> _Cache = new Dictionary<MethodInfo, DMethod>();

        public static DMethod Create(MethodInfo mi)
        {
            DMethod dm;
            if (_Cache.TryGetValue(mi, out dm))
            {
                return dm;
            }

            lock (_Cache)
            {
                if (_Cache.TryGetValue(mi, out dm))
                {
                    return dm;
                }
                else
                {
                    dm = new DMethod(mi);
                }
                _Cache[mi] = dm;
                return dm;
            }
        }
        private Func<object, object, object[]> _Caller;

        public MethodInfo MethodInfo { get; private set; }
        public Type PropertyType { get; private set; }
        public bool CanRead { get; private set; }
        public bool CanWrite { get; private set; }
        public bool IsPublic { get; private set; }
        public bool IsStatic { get; private set; }

        private DMethod(MethodInfo mi)
            : base(mi)
        {
            MethodInfo = mi;
            IsPublic = mi.IsPublic;
            IsStatic = mi.IsStatic;
            _Caller = EmitService.CreateCaller(mi);
        }


        public object Call(object obj, object[] parameters)
        {
            return _Caller(obj, parameters);
        }
    }
}
