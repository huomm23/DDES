using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace DDES
{
    class DDynamicObject
    {
        public object GetValue(object obj, string key)
        {
            if (obj is DynamicObject == false)
                return null;

            object v;
            if ((obj as DynamicObject).TryGetMember(
                     new CustomGetMemberBinder(key), out v))
                return v;

            return null;
        }

        public void SetValue(object obj, string key, object value)
        {
            if (obj is DynamicObject == false)
                return;

            (obj as DynamicObject).TrySetMember(new CustomSetMemberBinder(key), value);
        }

        /// <summary>
        /// 寻址过程中需要用到的动态参数类
        /// </summary>
        public class CustomGetMemberBinder : GetMemberBinder
        {
            public CustomGetMemberBinder(string name)
                : base(name, false) { }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                return target;
            }
        }

        /// <summary>
        /// 寻址过程中需要用到的动态参数类
        /// </summary>
        public class CustomSetMemberBinder : SetMemberBinder
        {
            public CustomSetMemberBinder(string name)
                : base(name, false) { }

            public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
            {
                return target;
            }
        }
    }
}
