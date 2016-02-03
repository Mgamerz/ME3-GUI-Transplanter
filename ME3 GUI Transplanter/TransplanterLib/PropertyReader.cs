using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TransplanterLib
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct ObjectProp
    {
        private string _name;
        private int _nameindex;
        [DesignOnly(true)]
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int nameindex
        {
            get { return _nameindex; }
            set { _nameindex = value; }
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct NameProp
    {
        private string _name;
        private int _nameindex;
        [DesignOnly(true)]
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int nameindex
        {
            get { return _nameindex; }
            set { _nameindex = value; }
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct StructProp
    {
        private string _name;
        private int _nameindex;
        private int[] _data;
        [DesignOnly(true)]
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int[] data
        {
            get { return _data; }
            set { _data = value; }
        }

        public int nameindex
        {
            get { return _nameindex; }
            set { _nameindex = value; }
        }
    }

    public struct NameReference
    {
        public int index;
        public int count;
        public String Name;
    }

    public static class PropertyReader
    {
        public enum PropertyType
        {
            Unknown = -1,
            None = 0,
            StructProperty = 1,
            IntProperty = 2,
            FloatProperty = 3,
            ObjectProperty = 4,
            NameProperty = 5,
            BoolProperty = 6,
            ByteProperty = 7,
            ArrayProperty = 8,
            StrProperty = 9,
            StringRefProperty = 10,
            DelegateProperty = 11
        }

        public class Property
        {
            public int Name;
            public PropertyType TypeVal;
            public int Size;
            public int i;
            public int offsetval;
            public int offend;
            public PropertyValue Value;
            public byte[] raw;
            //types
            //0 = None
            //1 = StructProperty
            //2 = IntProperty
            //3 = FloatProperty
            //4 = ObjectProperty
            //5 = NameProperty
            //6 = BoolProperty
            //7 = ByteProperty
            //8 = ArrayProperty
            //9 = StrProperty
            //10= StringRefProperty
        }

        public struct PropertyValue
        {
            public int len;
            public string StringValue;
            public int IntValue;
            public NameReference NameValue;
            public List<PropertyValue> Array;
        }

        public static List<Property> getPropList(PCCObject pcc, byte[] raw)
        {
            //Application.DoEvents();
            int start = detectStart(pcc, raw);
            return ReadProp(pcc, raw, start);
        }

        public static string TypeToString(int type)
        {
            switch (type)
            {
                case 1: return "Struct Property";
                case 2: return "Integer Property";
                case 3: return "Float Property";
                case 4: return "Object Property";
                case 5: return "Name Property";
                case 6: return "Bool Property";
                case 7: return "Byte Property";
                case 8: return "Array Property";
                case 9: return "String Property";
                case 10: return "String Ref Property";
                default: return "Unknown/None";
            }
        }

        public static string TypeToShortString(int type)
        {
            switch (type)
            {
                case 1: return "Struct";
                case 2: return "Integer";
                case 3: return "Float";
                case 4: return "Object";
                case 5: return "Name";
                case 6: return "Bool";
                case 7: return "Byte";
                case 8: return "Array";
                case 9: return "String";
                case 10: return "String Ref";
                default: return "Unknown/None";
            }
        }

        public static string PropertyToText(Property p, PCCObject pcc)
        {
            string name = pcc.Names[p.Name];
            string type = TypeToShortString((int)p.TypeVal);
            string size = p.Value.len.ToString();
            string value = "";

            string s = "";
            s = "Name: " + pcc.Names[p.Name];
            s += " | Type: " + TypeToString((int)p.TypeVal);
            s += " | Size: " + p.Value.len.ToString();
            switch (p.TypeVal)
            {
                case PropertyType.StructProperty:
                    s += " \"" + pcc.getNameEntry(p.Value.IntValue) + "\" with " + p.Value.Array.Count.ToString() + " bytes";
                    value = pcc.getNameEntry(p.Value.IntValue) + " ("+p.Value.Array.Count.ToString() + " bytes)";
                    break;
                case PropertyType.IntProperty:
                case PropertyType.ObjectProperty:
                case PropertyType.BoolProperty:
                case PropertyType.StringRefProperty:
                    s += " | Value: " + p.Value.IntValue.ToString();
                    value = p.Value.IntValue.ToString();
                    break;
                case PropertyType.FloatProperty:
                    byte[] buff = BitConverter.GetBytes(p.Value.IntValue);
                    float f = BitConverter.ToSingle(buff, 0);
                    s += " | Value: " + f.ToString();
                    value = f.ToString();
                    break;
                case PropertyType.NameProperty:
                    s += " " + pcc.Names[p.Value.IntValue];
                    value = pcc.Names[p.Value.IntValue];
                    break;
                case PropertyType.ByteProperty:
                    s += " | Value: \"" + p.Value.StringValue + "\" with \"" + pcc.getNameEntry(p.Value.IntValue) + "\"";
                    value = "\"" + p.Value.StringValue + "\" = \"" + pcc.getNameEntry(p.Value.IntValue) + "\"";
                    break;
                case PropertyType.ArrayProperty:
                    s += " | bytes"; //Value: " + p.Value.Array.Count.ToString() + " Elements";
                    value = p.Value.Array.Count.ToString() + " items";
                    break;
                case PropertyType.StrProperty:
                    if (p.Value.StringValue.Length == 0)
                        break;
                    value = p.Value.StringValue.Substring(0, p.Value.StringValue.Length - 1);
                    s += " | Value: " + p.Value.StringValue.Substring(0, p.Value.StringValue.Length - 1);
                    break;
            }
            return String.Format("|{0,40}|{1,15}|{2,10}|{3,30}|", name, type, size, value);
        }

        public class CustomProperty
        {
            private string sName = string.Empty;
            private string sCat = string.Empty;
            private bool bReadOnly = false;
            private bool bVisible = true;
            private object objValue = null;
            private PropertyType propertytype;


            public CustomProperty(string sName, string Category, object value, PropertyType type, bool bReadOnly, bool bVisible)
            {
                this.sName = sName;
                this.sCat = Category;
                this.objValue = value;
                this.propertytype = type;
                this.bReadOnly = bReadOnly;
                this.bVisible = bVisible;
            }

            private PropertyType type;
            public PropertyType Type
            {
                get { return type; }
            }

            public bool ReadOnly
            {
                get
                {
                    return bReadOnly;
                }
            }

            public string Name
            {
                get
                {
                    return sName;
                }
            }

            public string Category
            {
                get
                {
                    return sCat;
                }
            }

            public bool Visible
            {
                get
                {
                    return bVisible;
                }
            }

            public object Value
            {
                get
                {
                    return objValue;
                }
                set
                {
                    objValue = value;
                }
            }

        }

        public static CustomProperty PropertyToGrid(Property p, PCCObject pcc)
        {
            string cat = p.TypeVal.ToString();
            CustomProperty pg;
            switch (p.TypeVal)
            {
                case PropertyType.BoolProperty:
                    pg = new CustomProperty(pcc.Names[p.Name], cat, (p.Value.IntValue == 1), PropertyType.BoolProperty, false, true);
                    break;
                case PropertyType.FloatProperty:
                    byte[] buff = BitConverter.GetBytes(p.Value.IntValue);
                    float f = BitConverter.ToSingle(buff, 0);
                    pg = new CustomProperty(pcc.Names[p.Name], cat, f, PropertyType.FloatProperty, false, true);
                    break;
                case PropertyType.ByteProperty:
                case PropertyType.NameProperty:
                    NameProp pp = new NameProp();
                    pp.name = pcc.getNameEntry(p.Value.IntValue);
                    pp.nameindex = p.Value.IntValue;
                    pg = new CustomProperty(pcc.Names[p.Name], cat, pp, PropertyType.NameProperty, false, true);
                    break;
                case PropertyType.ObjectProperty:
                    ObjectProp ppo = new ObjectProp();
                    ppo.name = pcc.getObjectName(p.Value.IntValue);
                    ppo.nameindex = p.Value.IntValue;
                    pg = new CustomProperty(pcc.Names[p.Name], cat, ppo, PropertyType.ObjectProperty, false, true);
                    break;
                case PropertyType.StructProperty:
                    StructProp ppp = new StructProp();
                    ppp.name = pcc.getNameEntry(p.Value.IntValue);
                    ppp.nameindex = p.Value.IntValue;
                    byte[] buf = new byte[p.Value.Array.Count()];
                    for (int i = 0; i < p.Value.Array.Count(); i++)
                        buf[i] = (byte)p.Value.Array[i].IntValue;
                    List<int> buf2 = new List<int>();
                    for (int i = 0; i < p.Value.Array.Count() / 4; i++)
                        buf2.Add(BitConverter.ToInt32(buf, i * 4));
                    ppp.data = buf2.ToArray();
                    pg = new CustomProperty(pcc.Names[p.Name], cat, ppp, PropertyType.StructProperty, false, true);
                    break;
                default:
                    pg = new CustomProperty(pcc.Names[p.Name], cat, p.Value.IntValue, PropertyType.IntProperty, false, true);
                    break;
            }
            return pg;
        }

        public static List<Property> ReadProp(PCCObject pcc, byte[] raw, int start)
        {
            Property p;
            PropertyValue v;
            int sname;
            List<Property> result = new List<Property>();
            int pos = start;
            if (raw.Length - pos < 8)
                return result;
            int name = (int)BitConverter.ToInt64(raw, pos);
            if (!pcc.isName(name))
                return result;
            string t = pcc.Names[name];
            if (pcc.Names[name] == "None")
            {
                //p = new Property();
                //p.Name = name;
                //p.TypeVal = PropertyType.None;
                //p.i = 0;
                //p.offsetval = pos;
                //p.Size = 8;
                //p.Value = new PropertyValue();
                //p.raw = BitConverter.GetBytes((Int64)name);
                //p.offend = pos + 8;
                //result.Add(p);
                return result;
            }
            int type = (int)BitConverter.ToInt64(raw, pos + 8);
            int size = BitConverter.ToInt32(raw, pos + 16);
            int idx = BitConverter.ToInt32(raw, pos + 20);
            if (!pcc.isName(type) || size < 0 || size >= raw.Length)
                return result;
            string tp = pcc.Names[type];
            switch (tp)
            {

                case "DelegateProperty":
                    p = new Property();
                    p.Name = name;
                    p.TypeVal = PropertyType.DelegateProperty;
                    p.i = 0;
                    p.offsetval = pos + 24;
                    v = new PropertyValue();
                    v.IntValue = BitConverter.ToInt32(raw, pos + 28);
                    v.len = size;
                    v.Array = new List<PropertyValue>();
                    pos += 24;
                    for (int i = 0; i < size; i++)
                    {
                        PropertyValue v2 = new PropertyValue();
                        if (pos < raw.Length)
                            v2.IntValue = raw[pos];
                        v.Array.Add(v2);
                        pos++;
                    }
                    p.Value = v;
                    break;
                case "ArrayProperty":
                    int count = (int)BitConverter.ToInt64(raw, pos + 24);
                    p = new Property();
                    p.Name = name;
                    p.TypeVal = PropertyType.ArrayProperty;
                    p.i = 0;
                    p.offsetval = pos + 24;
                    v = new PropertyValue();
                    v.IntValue = type;
                    v.len = size - 4;
                    count = v.len;//TODO can be other objects too
                    v.Array = new List<PropertyValue>();
                    pos += 28;
                    for (int i = 0; i < count; i++)
                    {
                        PropertyValue v2 = new PropertyValue();
                        if (pos < raw.Length)
                            v2.IntValue = raw[pos];
                        v.Array.Add(v2);
                        pos++;
                    }
                    p.Value = v;
                    break;
                case "StrProperty":
                    count = (int)BitConverter.ToInt64(raw, pos + 24);
                    p = new Property();
                    p.Name = name;
                    p.TypeVal = PropertyType.StrProperty;
                    p.i = 0;
                    p.offsetval = pos + 24;
                    count *= -1;
                    v = new PropertyValue();
                    v.IntValue = type;
                    v.len = count;
                    pos += 28;
                    string s = "";
                    for (int i = 0; i < count; i++)
                    {
                        s += (char)raw[pos];
                        pos += 2;
                    }
                    v.StringValue = s;
                    p.Value = v;
                    break;
                case "StructProperty":
                    sname = (int)BitConverter.ToInt64(raw, pos + 24);
                    p = new Property();
                    p.Name = name;
                    p.TypeVal = PropertyType.StructProperty;
                    p.i = 0;
                    p.offsetval = pos + 24;
                    v = new PropertyValue();
                    v.IntValue = sname;
                    v.len = size;
                    v.Array = new List<PropertyValue>();
                    pos += 32;
                    for (int i = 0; i < size; i++)
                    {
                        PropertyValue v2 = new PropertyValue();
                        if (pos < raw.Length)
                            v2.IntValue = raw[pos];
                        v.Array.Add(v2);
                        pos++;
                    }
                    p.Value = v;
                    break;
                case "ByteProperty":
                    sname = (int)BitConverter.ToInt64(raw, pos + 24);
                    p = new Property();
                    p.Name = name;
                    p.TypeVal = PropertyType.ByteProperty;
                    p.i = 0;
                    p.offsetval = pos + 32;
                    v = new PropertyValue();
                    v.StringValue = pcc.getNameEntry(sname);
                    v.len = size;
                    pos += 32;
                    v.IntValue = (int)BitConverter.ToInt64(raw, pos);
                    pos += size;
                    p.Value = v;
                    break;
                default:
                    p = new Property();
                    p.Name = name;
                    p.TypeVal = getType(pcc, type);
                    p.i = 0;
                    p.offsetval = pos + 24;
                    p.Value = ReadValue(pcc, raw, pos + 24, type);
                    pos += p.Value.len + 24;
                    break;
            }
            p.raw = new byte[pos - start];
            p.offend = pos;
            if (pos < raw.Length)
                for (int i = 0; i < pos - start; i++)
                    p.raw[i] = raw[start + i];
            result.Add(p);
            if (pos != start) result.AddRange(ReadProp(pcc, raw, pos));
            return result;
        }

        private static PropertyType getType(PCCObject pcc, int type)
        {
            switch (pcc.getNameEntry(type))
            {
                case "None": return PropertyType.None;
                case "StructProperty": return PropertyType.StructProperty;
                case "IntProperty": return PropertyType.IntProperty;
                case "FloatProperty": return PropertyType.FloatProperty;
                case "ObjectProperty": return PropertyType.ObjectProperty;
                case "NameProperty": return PropertyType.NameProperty;
                case "BoolProperty": return PropertyType.BoolProperty;
                case "ByteProperty": return PropertyType.ByteProperty;
                case "ArrayProperty": return PropertyType.ArrayProperty;
                case "DelegateProperty": return PropertyType.DelegateProperty;
                case "StrProperty": return PropertyType.StrProperty;
                case "StringRefProperty": return PropertyType.StringRefProperty;
                default:
                    return PropertyType.Unknown;
            }
        }

        private static PropertyValue ReadValue(PCCObject pcc, byte[] raw, int start, int type)
        {
            PropertyValue v = new PropertyValue();
            switch (pcc.Names[type])
            {
                case "IntProperty":
                case "FloatProperty":
                case "ObjectProperty":
                case "StringRefProperty":
                    v.IntValue = BitConverter.ToInt32(raw, start);
                    v.len = 4;
                    break;
                case "NameProperty":
                    v.IntValue = BitConverter.ToInt32(raw, start);
                    var nameRef = new NameReference();
                    nameRef.index = v.IntValue;
                    nameRef.count = BitConverter.ToInt32(raw, start + 4);
                    nameRef.Name = pcc.getNameEntry(nameRef.index);
                    if (nameRef.count > 0)
                        nameRef.Name += "_" + (nameRef.count - 1);
                    v.NameValue = nameRef;
                    v.len = 8;
                    break;
                case "BoolProperty":
                    if (start < raw.Length)
                        v.IntValue = raw[start];
                    v.len = 1;
                    break;
            }
            return v;
        }

        public static int detectStart(PCCObject pcc, byte[] raw)
        {
            int result = 8;
            int test1 = BitConverter.ToInt32(raw, 4);
            if (test1 < 0)
                result = 30;
            else
            {
                int test2 = BitConverter.ToInt32(raw, 8);
                if (pcc.isName(test1) && test2 == 0)
                    result = 4;
                if (pcc.isName(test1) && pcc.isName(test2) && test2 != 0)
                    result = 8;
            }
            return result;
        }
        public static int detectStart(PCCObject pcc, byte[] raw, long flags)
        {
            if ((flags & (long)UnrealFlags.EObjectFlags.HasStack) != 0)
            {
                return 30;
            }
            int result = 8;
            int test1 = BitConverter.ToInt32(raw, 4);
            int test2 = BitConverter.ToInt32(raw, 8);
            if (pcc.isName(test1) && test2 == 0)
                result = 4;
            if (pcc.isName(test1) && pcc.isName(test2) && test2 != 0)
                result = 8;
            return result;
        }
    }
}
