using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;

namespace WoWTempDBC
{
	public class DBEntry : IDisposable
	{
		public DBHeader Header { get; private set; }
		public DataTable Data { get; set; }
		public bool Changed { get; set; } = false;
		public string FilePath { get; private set; }
		public string FileName => Path.GetFileName(this.FilePath);
		public string SavePath { get; set; }
		public Table TableStructure => Header.TableStructure;
		public string Key { get; private set; }
		public string Tag { get; private set; }


		private int Min = -1;
		private int Max = -1;
		private IEnumerable<int> UnqiueRowIndices;
		private IEnumerable<int> PrimaryKeys;

		public DBEntry(DBHeader DHeader, string DFilepath)
		{
			Header = DHeader;
			FilePath = DFilepath;
			SavePath = DFilepath;
			Header.TableStructure = Database.Definitions.Tables.FirstOrDefault(x => x.Name.Equals(Path.GetFileNameWithoutExtension(DFilepath), StringComparison.CurrentCultureIgnoreCase));

			LoadDefinition();
		}


		/// <summary>
		/// Converts the XML definition to an empty DataTable
		/// </summary>
		public void LoadDefinition()
		{
			if (TableStructure == null)
				return;

			Key = TableStructure.Key.Name;
			Tag = Guid.NewGuid().ToString();

			if (TableStructure.Fields.GroupBy(X => X.Name).Any(Y => Y.Count() > 1))
			{
				Helper.BoxShow(null, $"重复的列名 来自 {FileName}", "错误");
				return;
			}

			LoadTableStructure();
		}

		public void LoadTableStructure()
		{
			Data = new DataTable() { TableName = Tag, CaseSensitive = false, RemotingFormat = SerializationFormat.Binary };

			foreach (var Col in TableStructure.Fields)
			{
				Queue<TextWowEnum> Languages = new Queue<TextWowEnum>(Enum.GetValues(typeof(TextWowEnum)).Cast<TextWowEnum>());

				for (int i = 0; i < Col.ArraySize; i++)
				{
					string ColumnName = Col.Name;

					if (Col.ArraySize > 1)
					{
						ColumnName += "_" + (i + 1);
					}

					Col.InternalName = ColumnName;

					switch (Col.Type.ToLower())
					{
						case "sbyte":
							Data.Columns.Add(ColumnName, typeof(sbyte));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "byte":
							Data.Columns.Add(ColumnName, typeof(byte));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "int32":
						case "int":
							Data.Columns.Add(ColumnName, typeof(int));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "uint32":
						case "uint":
							Data.Columns.Add(ColumnName, typeof(uint));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "int64":
						case "long":
							Data.Columns.Add(ColumnName, typeof(long));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "uint64":
						case "ulong":
							Data.Columns.Add(ColumnName, typeof(ulong));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "single":
						case "float":
							Data.Columns.Add(ColumnName, typeof(float));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "boolean":
						case "bool":
							Data.Columns.Add(ColumnName, typeof(bool));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "string":
							Data.Columns.Add(ColumnName, typeof(string));
							Data.Columns[ColumnName].DefaultValue = string.Empty;
							break;
						case "int16":
						case "short":
							Data.Columns.Add(ColumnName, typeof(short));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "uint16":
						case "ushort":
							Data.Columns.Add(ColumnName, typeof(ushort));
							Data.Columns[ColumnName].DefaultValue = 0;
							break;
						case "loc":
							for (int x = 0; x < 17; x++)
							{
								if (x == 16)
								{
									Data.Columns.Add(Col.Name + "_Mask", typeof(uint));
									Data.Columns[Col.Name + "_Mask"].AllowDBNull = false;
									Data.Columns[Col.Name + "_Mask"].DefaultValue = 0;
								}
								else
								{
									ColumnName = Col.Name + "_" + Languages.Dequeue().ToString();
									Data.Columns.Add(ColumnName, typeof(string));
									Data.Columns[ColumnName].AllowDBNull = false;
									Data.Columns[ColumnName].DefaultValue = string.Empty;
								}
							}
							break;
						default:
							throw new Exception($"未知字段类型 {Col.Type} 来自 {Col.Name}");
					}

					Data.Columns[ColumnName].AllowDBNull = false;
				}
			}

			Data.Columns[Key].DefaultValue = null;
			Data.PrimaryKey = new DataColumn[] { Data.Columns[Key] };
			Data.Columns[Key].AutoIncrement = true;
			Data.Columns[Key].Unique = true;
		}


		/// <summary>
		/// Generates a Bit map for all columns as the Blizzard one combines array columns
		/// </summary>
		/// <returns></returns>
		public FieldStructureEntry[] GetBits()
		{
			if (!Header.IsTypeOf<WDB5>())
				return new FieldStructureEntry[Data.Columns.Count];

			List<FieldStructureEntry> Bits = new List<FieldStructureEntry>();
			if (Header is WDC1 AtHeader)
			{
				var Fields = AtHeader.ColumnMeta;
				for (int i = 0; i < Fields.Count; i++)
				{
					short BitCount = (short)(Header.FieldStructure[i].BitCount == 64 ? Header.FieldStructure[i].BitCount : 0);
					for (int x = 0; x < Fields[i].ArraySize; x++)
						Bits.Add(new FieldStructureEntry(BitCount, 0));
				}
			}
			else
			{
				var Fields = Header.FieldStructure;
				for (int i = 0; i < TableStructure.Fields.Count; i++)
				{
					Field F = TableStructure.Fields[i];
					for (int x = 0; x < F.ArraySize; x++)
						Bits.Add(new FieldStructureEntry((Fields[i]?.Bits ?? 0), 0, (Fields[i]?.CommonDataType ?? 0xFF)));
				}
			}

			return Bits.ToArray();
		}

		public int[] GetPadding()
		{
			int[] Padding = new int[Data.Columns.Count];

			Dictionary<Type, int> ByteCounts = new Dictionary<Type, int>()
			{
				{ typeof(byte), 1 },
				{ typeof(short), 2 },
				{ typeof(ushort), 2 },
			};

			if (Header is WDC1 AtHeader)
			{

				int N = 0;

				foreach (var Field in AtHeader.ColumnMeta)
				{
					Type Typ = Data.Columns[N].DataType;
					bool IsNeeded = Field.CompressionType >= CompressionType.Sparse;

					if (ByteCounts.ContainsKey(Typ) && IsNeeded)
					{
						for (int x = 0; x < Field.ArraySize; x++)
							Padding[N++] = 4 - ByteCounts[Typ];
					}
					else
					{
						N += Field.ArraySize;
					}
				}
			}

			return Padding;
		}

		public void UpdateColumnTypes()
		{
			if (!Header.IsTypeOf<WDB6>())
				return;

			var Fields = ((WDB6)Header).FieldStructure;
			int N = 0;
			for (int i = 0; i < TableStructure.Fields.Count; i++)
			{
				int ArraySize = TableStructure.Fields[i].ArraySize;

				if (!Fields[i].CommonDataColumn)
				{
					N += ArraySize;
					continue;
				}

				Type ColumnType;
				switch (Fields[i].CommonDataType)
				{
					case 0:
						ColumnType = typeof(string);
						break;
					case 1:
						ColumnType = typeof(ushort);
						break;
					case 2:
						ColumnType = typeof(byte);
						break;
					case 3:
						ColumnType = typeof(float);
						break;
					case 4:
						ColumnType = typeof(int);
						break;
					default:
						N += ArraySize;
						continue;
				}

				for (int x = 0; x < ArraySize; x++)
				{
					Data.Columns[N].DataType = ColumnType;
					N++;
				}
			}
		}


		#region Special Data
		/// <summary>
		/// Gets the Min and Max ids
		/// </summary>
		/// <returns></returns>
		public Tuple<int, int> MinMax()
		{
			if (Min == -1 || Max == -1)
			{
				Min = int.MaxValue;
				Max = int.MinValue;
				foreach (DataRow dr in Data.Rows)
				{
					int Val = dr.Field<int>(Key);
					Min = Math.Min(Min, Val);
					Max = Math.Max(Max, Val);
				}
			}

			return new Tuple<int, int>(Min, Max);
		}

		/// <summary>
		/// Gets a list of Ids
		/// </summary>
		/// <returns></returns>
		public IEnumerable<int> GetPrimaryKeys()
		{
			if (PrimaryKeys == null)
				PrimaryKeys = Data.AsEnumerable().Select(x => x.Field<int>(Key));

			return PrimaryKeys;
		}

		/// <summary>
		/// Produces a list of unique rows (excludes key values)
		/// </summary>
		/// <returns></returns>
		public IEnumerable<DataRow> GetUniqueRows()
		{
			if (UnqiueRowIndices == null)
			{
				var Temp = Data.Copy();
				Temp.PrimaryKey = null;
				Temp.Columns.Remove(Key);

				var Comp = new ORowComparer();
				UnqiueRowIndices = Temp.AsEnumerable()
								 .Select((t, i) => new ORow(i, t.ItemArray))
								 .Distinct(Comp)
								 .Select(x => x.Index);
			}

			foreach (var u in UnqiueRowIndices)
				yield return Data.Rows[u];
		}

		/// <summary>
		/// Generates a map of unqiue rows and grouped count
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IEnumerable<int>> GetCopyRows()
		{
			var Pks = GetPrimaryKeys().ToArray();

			var Temp = Data.Copy();
			Temp.PrimaryKey = null;
			Temp.Columns.Remove(Key);

			var Comp = new OArrayComparer();
			return Temp.AsEnumerable()
					   .Select((Name, Index) => new { Name.ItemArray, Index })
					   .GroupBy(x => x.ItemArray, Comp)
					   .Select(xg => xg.Select(x => Pks[x.Index]))
					   .Where(x => x.Count() > 1);
		}

		/// <summary>
		/// Extracts the id and the total length of strings for each row
		/// </summary>
		/// <returns></returns>
		public Dictionary<int, short> GetStringLengths()
		{
			Dictionary<int, short> Result = new Dictionary<int, short>();
			IEnumerable<string> Cols = Data.Columns.Cast<DataColumn>()
											  .Where(X => X.DataType == typeof(string))
											  .Select(X => X.ColumnName);

			foreach (DataRow row in Data.Rows)
			{
				short Total = 0;
				foreach (string C in Cols)
				{
					short Len = (short)Encoding.UTF8.GetByteCount(row[C].ToString());
					Total += (short)(Len > 0 ? Len + 1 : 0);
				}
				Result.Add(row.Field<int>(Key), Total);
			}

			return Result;
		}

		public void ResetTemp()
		{
			Min = -1;
			Max = -1;
			UnqiueRowIndices = null;
			PrimaryKeys = null;
		}
		#endregion


		/// <summary>
		/// 转存到SQL
		/// </summary>
		public void ToSQLTable(string ConStringDatas)
		{
			MySqlConnection DataConn = new MySqlConnection(ConStringDatas);
			DataConn.Open();
			if (DataConn.State == ConnectionState.Open)
            {
				Data.DefaultView.Sort = Data.Columns[0].ColumnName + " ASC";
				string TableName = Helper.CustomTitle + TableStructure.Name + ".dbc";
				string CsvName = Path.Combine(Helper.TempFolder, TableName + ".csv");
				StringBuilder SBuilder = new StringBuilder();
				SBuilder.AppendLine($"DROP TABLE IF EXISTS `{TableName}`; ");
				SBuilder.AppendLine($"CREATE TABLE `{TableName}` ({Data.Columns.ToTableColumnSQL(Key)}) ENGINE=InnoDB DEFAULT CHARACTER SET = utf8 COLLATE = utf8_general_ci; ");

				using (StreamWriter Csv = new StreamWriter(CsvName, false, Encoding.UTF8))
					Csv.Write(ToCSV());

				using (MySqlCommand Command = new MySqlCommand(SBuilder.ToString(), DataConn))
					Command.ExecuteNonQuery();

				new MySqlBulkLoader(DataConn)
				{
					TableName = $"`{TableName}`",
					FieldTerminator = ",",
					LineTerminator = "\r\n",
					NumberOfLinesToSkip = 1,
					FileName = CsvName,
					FieldQuotationCharacter = '"',
					CharacterSet = "UTF8"
				}.Load();

				try { File.Delete(CsvName); }
				catch { }

				DataConn.Close();
			}
		}

		public string ToCSV()
		{
			Func<string, string> EncodeCsv = S => { return string.Concat("\"", S.Replace(Environment.NewLine, string.Empty).Replace("\"", "\"\""), "\""); };

			StringBuilder SBuilder = new StringBuilder();
			IEnumerable<string> ColumnNames = Data.Columns.Cast<DataColumn>().Select(Column => EncodeCsv(Column.ColumnName));
			SBuilder.AppendLine(string.Join(",", ColumnNames));

			foreach (DataRow Row in Data.Rows)
			{
				IEnumerable<string> Fields = Row.ItemArray.Select(field => EncodeCsv(field.ToString()));
				SBuilder.AppendLine(string.Join(",", Fields));
			}

			return SBuilder.ToString();
		}

		public void Dispose()
		{
			Data?.Dispose();
			Data = null;
		}

		/// <summary>
		/// 转存到SQL
		/// </summary>
		public void SQLToDBC(string ConStringDatas)
		{
			MySqlConnection DataConn = new MySqlConnection(ConStringDatas);
			DataConn.Open();
			if (DataConn.State == ConnectionState.Open)
			{
				Data.Dispose();
				Data = null;
				string TableName = Helper.CustomTitle + TableStructure.Name + ".dbc";
				MySqlDataAdapter Adapter = new MySqlDataAdapter($"SELECT * FROM `{TableName}`;", DataConn);
				Data = new DataTable();
				Adapter.Fill(Data);
				Adapter.Dispose();

				Data.DefaultView.Sort = Data.Columns[0].ColumnName + " ASC";

				DataConn.Close();
			}
		}
	}
}
