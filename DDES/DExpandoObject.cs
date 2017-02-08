using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace DDES
{
    public class DExpandoObject
    {
        private IDictionary<string, object> items = null;

        /// <summary>
        /// 获取对应键值的对象
        /// </summary>
        /// <param name="value"></param>
        /// <returns>是否获取成功</returns>
        public Object GetValue(Object obj, string key)
        {
            if (obj is ExpandoObject == false)
                return null;

            return (obj as IDictionary<string, object>)[key];
        }

        /// <summary>
        /// 设置对应键值的对象
        /// </summary>
        /// <param name="value"></param>
        /// <returns>是否设置成功</returns>
        public void SetValue(object obj, string key, object value)
        {
            if (obj is ExpandoObject == false)
                return;

            (obj as IDictionary<string, object>)[key] = value;
        }
    }
}
