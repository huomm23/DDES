using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DDES
{
    public abstract class AbstractDMember
    {
        public string Name { get; private set; }
        public Type DeclaringType { get; private set; }
        public Type ReflectedType { get; private set; }

        protected AbstractDMember(MemberInfo mi)
        {
            Name = mi.Name;
            DeclaringType = mi.DeclaringType;
            ReflectedType = mi.ReflectedType;
        }
    }
}
