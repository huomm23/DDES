using DDES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tester
{
    public class PropertyEntity
    {
        public PropertyEntity()
        {
            this.AnonymousProperty = new { A = 123 };
        }
        public bool BoolProperty { get; set; }
        public int IntProperty { get; set; }
        public double DoubleProperty { get; set; }
        public string StringProperty { get; set; }
        public object ObjectProperty { get; set; }
        public bool? BoolNullableProperty { get; set; }


        public bool[] BoolArrayProperty { get; set; }
        public int[] IntArrayProperty { get; set; }
        public double[] DoubleArrayProperty { get; set; }
        public string[] StringArrayProperty { get; set; }
        public object[] ObjectArrayProperty { get; set; }
        public bool?[] BoolNullableArrayProperty { get; set; }

        // 匿名属性
        public object AnonymousProperty { get; set; }
    }


    public static class Test_Property
    {
        public static bool Test()
        {
            try
            {
                var obj = new PropertyEntity();

                var ret = true;
                var boolProp = DType.Create(obj.GetType()).Properties["BoolProperty"];
                var boolNullableProp = DType.Create(obj.GetType()).Properties["BoolNullableProperty"];
                var boolArrayProp = DType.Create(obj.GetType()).Properties["BoolArrayProperty"];
                var AnonymousProp = DType.Create(obj.GetType()).Properties["AnonymousProperty"];

                ///////////////////////////////////////
                var target = boolProp;
                target.SetValue(obj, true);
                var v = target.GetValue(obj);
                ret &= (bool)v == true;

                target.SetValue(obj, false);
                v = target.GetValue(obj);
                ret &= (bool)v == false;

                /////////////////////////////////////////////
                target = boolNullableProp;

                target.SetValue(obj, false);
                v = target.GetValue(obj);
                ret &= (bool)v == false;

                target.SetValue(obj, true);
                v = target.GetValue(obj);
                ret &= (bool)v == true;

                //////////////////////////////////////////////////
                target = boolArrayProp;

                target.SetValue(obj, new bool[] { true, false });
                v = target.GetValue(obj);
                ret &= ((bool[])v)[0] == true;
                ret &= ((bool[])v)[1] == false;

                //////////////////////////////////////////////
                target = AnonymousProp;
                target.SetValue(obj, new { A = 1234 });
                v = target.GetValue(obj);
                ret &= ((dynamic)v).A == 1234;

                /////////////////////////////////////////
                obj.AnonymousProperty = new { A = 7890 };
                var Anonymous_A_Prop = DType.Create(obj.AnonymousProperty.GetType()).Properties["A"];

                target = Anonymous_A_Prop;
                v = target.GetValue(obj.AnonymousProperty);
                ret &= ((int)v) == 7890;

                return ret;

            }
            catch(Exception ex)
            {
                return false;
            }
        }
    }
}
