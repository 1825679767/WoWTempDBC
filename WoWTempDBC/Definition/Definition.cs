using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WoWTempDBC
{
    public enum TextWowEnum
    {
        enUS,
        enGB,
        koKR,
        frFR,
        deDE,
        enCN,
        zhCN,
        enTW,
        zhTW,
        esES,
        esMX,
        ruRU,
        ptPT,
        ptBR,
        itIT,
        Unk,
    }

    public enum HeaderFlags : short
    {
        None = 0x0,
        OffsetMap = 0x1,
        RelationshipData = 0x2,
        IndexMap = 0x4,
        Unknown = 0x8,
        Compressed = 0x10,
    }

    public enum CompressionType
    {
        None = 0,
        Immediate = 1,
        Sparse = 2,
        Pallet = 3,
        PalletArray = 4,
        SignedImmediate = 5
    }

    public class ORowComparer : IEqualityComparer<ORow>
    {
        public bool Equals(ORow X, ORow Y)
        {
            var Xa = X.Array;
            var Ya = Y.Array;

            for (int i = 0; i < Xa.Length; i++)
                if (!Xa[i].Equals(Ya[i]))
                    return false;

            return true;
        }

        public int GetHashCode(ORow Obj)
        {
            unchecked
            {
                var A = Obj.Array;
                int Hash = (int)2166136261;
                for (int i = 0; i < A.Length; i++)
                    Hash = (Hash * 16777619) ^ A[i].GetHashCode();
                return Hash;
            }
        }
    }

    public class OArrayComparer : IEqualityComparer<object[]>
    {
        public bool Equals(object[] X, object[] Y)
        {
            for (int i = 0; i < X.Length; i++)
                if (!X[i].Equals(Y[i]))
                    return false;

            return true;
        }

        public int GetHashCode(object[] Obj)
        {
            unchecked
            {
                int Hash = (int)2166136261;
                for (int i = 0; i < Obj.Length; i++)
                    Hash = (Hash * 16777619) ^ Obj[i].GetHashCode();
                return Hash;
            }
        }
    }

    public class ORow
    {
        public int Index;
        public object[] Array;

        public ORow(int index, object[] array)
        {
            Index = index;
            Array = array;
        }
    }

    public static class Es
    {
        /// <summary>
        /// 转SQL类型 DataRow
        /// </summary>
        public static string ToDataSQL(this DataRow Row)
        {
            StringBuilder SBuilder = new StringBuilder();
            DataColumnCollection Cols = Row.Table.Columns;
            CultureInfo Ci = CultureInfo.CreateSpecificCulture("en-US");

            for (int i = 0; i < Cols.Count; i++)
            {
                if (Cols[i].DataType == typeof(string))
                {
                    string Val = Row[i].ToString().Replace(@"'", @"\'").Replace(@"""", @"\""").Replace(@"\", @"\\");
                    SBuilder.Append("\"" + Val + "\",");
                }
                else if (Cols[i].DataType == typeof(float))
                {
                    SBuilder.Append(((float)Row[i]).ToString(Ci) + ",");
                }
                else
                {
                    SBuilder.Append(Row[i] + ",");
                }

            }

            return SBuilder.ToString().TrimEnd(',');
        }

        /// <summary>
        /// 反转
        /// </summary>
        public static string DoReverse(this string Str)
        {
            return new string(Str.ToCharArray().Reverse().ToArray());
        }

        /// <summary>
        /// 检查是否占用
        /// </summary>
        public static bool IsOccupied(string FilePath)
        {
            FileStream Stream = null;
            try
            {
                Stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch
            {
                return true;
            }
            finally
            {
                if (Stream != null)
                {
                    Stream.Close();
                }
            }
        }

        /// <summary>
        /// 转SQL类型 DataColumnCollection
        /// </summary>
        public static string ToTableColumnSQL(this DataColumnCollection Cols, string PrimaryKey = "")
        {
            StringBuilder SBuilder = new StringBuilder();
            foreach (DataColumn Col in Cols)
            {
                switch (Col.DataType.Name)
                {
                    case "SByte":
                        SBuilder.Append($" `{Col.ColumnName}` TINYINT NOT NULL DEFAULT '0',");
                        break;
                    case "Byte":
                    case "Boolean":
                        SBuilder.Append($" `{Col.ColumnName}` TINYINT UNSIGNED NOT NULL DEFAULT '0',");
                        break;
                    case "Int16":
                        SBuilder.Append($" `{Col.ColumnName}` SMALLINT NOT NULL DEFAULT '0',");
                        break;
                    case "UInt16":
                        SBuilder.Append($" `{Col.ColumnName}` SMALLINT UNSIGNED NOT NULL DEFAULT '0',");
                        break;
                    case "Int32":
                        SBuilder.Append($" `{Col.ColumnName}` INT NOT NULL DEFAULT '0',");
                        break;
                    case "UInt32":
                        SBuilder.Append($" `{Col.ColumnName}` INT UNSIGNED NOT NULL DEFAULT '0',");
                        break;
                    case "Int64":
                        SBuilder.Append($" `{Col.ColumnName}` BIGINT NOT NULL DEFAULT '0',");
                        break;
                    case "UInt64":
                        SBuilder.Append($" `{Col.ColumnName}` BIGINT UNSIGNED NOT NULL DEFAULT '0',");
                        break;
                    case "Single":
                    case "Float":
                        SBuilder.Append($" `{Col.ColumnName}` FLOAT NOT NULL DEFAULT '0',");
                        break;
                    case "String":
                        SBuilder.Append($" `{Col.ColumnName}` VARCHAR(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT '',");
                        break;
                    default:
                        throw new Exception($"Unknown data type {Col.ColumnName} : {Col.DataType.Name}");
                }
            }

            if (!string.IsNullOrWhiteSpace(PrimaryKey))
                SBuilder.Append($" PRIMARY KEY (`{PrimaryKey}`)");

            return SBuilder.ToString().TrimEnd(',');
        }
    }

    [Serializable]
    public class Definition
    {
        [XmlElement("Table")]
        public HashSet<Table> Tables { get; set; } = new HashSet<Table>();

        public bool LoadDefinition(string XMLPath)
        {
            try
            {
                XmlSerializer Deser = new XmlSerializer(typeof(Definition));
                using (var FStream = new FileStream(XMLPath, FileMode.Open, FileAccess.Read))
                {
                    Definition Def = (Definition)Deser.Deserialize(FStream);
                    var NewTables = Def.Tables.Where(X => Tables.Count(Y => X.Name == Y.Name) == 0).ToList();
                    NewTables.ForEach(X => X.Load());
                    Tables.UnionWith(NewTables.Where(X => X.Key != null));
                    return true;
                }
            }
            catch { return false; }
        }
    }

    [Serializable]
    public class Table
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlElement("Field")]
        public List<Field> Fields { get; set; }
        [XmlIgnore]
        public Field Key { get; private set; }
        public void Load()
        {
            Key = Fields.FirstOrDefault(x => x.IsIndex);
        }
    }

    [Serializable]
    public class Field
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Type { get; set; }
        [XmlAttribute, DefaultValue(1)]
        public int ArraySize { get; set; } = 1;
        [XmlAttribute, DefaultValue(false)]
        public bool IsIndex { get; set; } = false;
        [XmlAttribute, DefaultValue("")]
        public string DefaultValue { get; set; } = "";
        [XmlIgnore]
        public string InternalName { get; set; }
    }
}
