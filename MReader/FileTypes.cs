using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoWTempDBC
{
	public class WDC1 : WDB6
	{
		public int PackedDataOffset;
		public uint RelationshipCount;
		public int OffsetTableOffset;
		public int IndexSize;
		public int ColumnMetadataSize;
		public int SparseDataSize;
		public int PalletDataSize;
		public int RelationshipDataSize;

		public List<ColumnStructureEntry> ColumnMeta;
		public RelationShipData RelationShipData;
		//public Dictionary<int, MinMax> MinMaxValues;

		protected int[] columnSizes;
		protected byte[] recordData;

		#region Read
		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			ReadBaseHeader(ref dbReader, signature);

			TableHash = dbReader.ReadUInt32();
			LayoutHash = dbReader.ReadInt32();
			MinId = dbReader.ReadInt32();
			MaxId = dbReader.ReadInt32();
			Locale = dbReader.ReadInt32();
			CopyTableSize = dbReader.ReadInt32();
			Flags = (HeaderFlags)dbReader.ReadUInt16();
			IdIndex = dbReader.ReadUInt16();
			TotalFieldSize = dbReader.ReadUInt32();

			PackedDataOffset = dbReader.ReadInt32();
			RelationshipCount = dbReader.ReadUInt32();
			OffsetTableOffset = dbReader.ReadInt32();
			IndexSize = dbReader.ReadInt32();
			ColumnMetadataSize = dbReader.ReadInt32();
			SparseDataSize = dbReader.ReadInt32();
			PalletDataSize = dbReader.ReadInt32();
			RelationshipDataSize = dbReader.ReadInt32();

			//Gather field structures
			FieldStructure = new List<FieldStructureEntry>();
			for (int i = 0; i < FieldCount; i++)
			{
				var field = new FieldStructureEntry(dbReader.ReadInt16(), dbReader.ReadUInt16());
				FieldStructure.Add(field);
			}

			recordData = dbReader.ReadBytes((int)(RecordCount * RecordSize));
			Array.Resize(ref recordData, recordData.Length + 8);

			Flags &= ~HeaderFlags.RelationshipData; // appears to be obsolete now
		}

		public new Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = new Dictionary<int, byte[]>();
			List<Tuple<int, short>> offsetmap = new List<Tuple<int, short>>();
			Dictionary<int, OffsetDuplicate> firstindex = new Dictionary<int, OffsetDuplicate>();
			Dictionary<int, int> OffsetDuplicates = new Dictionary<int, int>();
			Dictionary<int, List<int>> Copies = new Dictionary<int, List<int>>();

			int[] m_indexes = null;

			// OffsetTable
			if (HasOffsetTable && OffsetTableOffset > 0)
			{
				dbReader.BaseStream.Position = OffsetTableOffset;
				for (int i = 0; i < (MaxId - MinId + 1); i++)
				{
					int offset = dbReader.ReadInt32();
					short length = dbReader.ReadInt16();

					if (offset == 0 || length == 0)
						continue;

					// special case, may contain duplicates in the offset map that we don't want
					if (CopyTableSize == 0)
					{
						if (!firstindex.ContainsKey(offset))
						{
							firstindex.Add(offset, new OffsetDuplicate(offsetmap.Count, firstindex.Count));
						}
						else
						{
							OffsetDuplicates.Add(MinId + i, firstindex[offset].VisibleIndex);
							continue;
						}
					}

					offsetmap.Add(new Tuple<int, short>(offset, length));
				}
			}

			// IndexTable
			if (HasIndexTable)
			{
				m_indexes = new int[RecordCount];
				for (int i = 0; i < RecordCount; i++)
					m_indexes[i] = dbReader.ReadInt32();
			}

			// Copytable
			if (CopyTableSize > 0)
			{
				long end = dbReader.BaseStream.Position + CopyTableSize;
				while (dbReader.BaseStream.Position < end)
				{
					int id = dbReader.ReadInt32();
					int idcopy = dbReader.ReadInt32();

					if (!Copies.ContainsKey(idcopy))
						Copies.Add(idcopy, new List<int>());

					Copies[idcopy].Add(id);
				}
			}

			// ColumnMeta
			ColumnMeta = new List<ColumnStructureEntry>();
			for (int i = 0; i < FieldCount; i++)
			{
				var column = new ColumnStructureEntry()
				{
					RecordOffset = dbReader.ReadUInt16(),
					Size = dbReader.ReadUInt16(),
					AdditionalDataSize = dbReader.ReadUInt32(), // size of pallet / sparse values
					CompressionType = (CompressionType)dbReader.ReadUInt32(),
					BitOffset = dbReader.ReadInt32(),
					BitWidth = dbReader.ReadInt32(),
					Cardinality = dbReader.ReadInt32()
				};

				// preload arraysizes
				if (column.CompressionType == CompressionType.None)
					column.ArraySize = Math.Max(column.Size / FieldStructure[i].BitCount, 1);
				else if (column.CompressionType == CompressionType.PalletArray)
					column.ArraySize = Math.Max(column.Cardinality, 1);

				ColumnMeta.Add(column);
			}

			// Pallet values
			for (int i = 0; i < ColumnMeta.Count; i++)
			{
				if (ColumnMeta[i].CompressionType == CompressionType.Pallet || ColumnMeta[i].CompressionType == CompressionType.PalletArray)
				{
					int elements = (int)ColumnMeta[i].AdditionalDataSize / 4;
					int cardinality = Math.Max(ColumnMeta[i].Cardinality, 1);

					ColumnMeta[i].PalletValues = new List<byte[]>();
					for (int j = 0; j < elements / cardinality; j++)
						ColumnMeta[i].PalletValues.Add(dbReader.ReadBytes(cardinality * 4));
				}
			}

			// Sparse values
			for (int i = 0; i < ColumnMeta.Count; i++)
			{
				if (ColumnMeta[i].CompressionType == CompressionType.Sparse)
				{
					ColumnMeta[i].SparseValues = new Dictionary<int, byte[]>();
					for (int j = 0; j < ColumnMeta[i].AdditionalDataSize / 8; j++)
						ColumnMeta[i].SparseValues[dbReader.ReadInt32()] = dbReader.ReadBytes(4);
				}
			}

			// Relationships
			if (RelationshipDataSize > 0)
			{
				RelationShipData = new RelationShipData()
				{
					Records = dbReader.ReadUInt32(),
					MinId = dbReader.ReadUInt32(),
					MaxId = dbReader.ReadUInt32(),
					Entries = new Dictionary<uint, byte[]>()
				};

				for (int i = 0; i < RelationShipData.Records; i++)
				{
					byte[] foreignKey = dbReader.ReadBytes(4);
					uint index = dbReader.ReadUInt32();
					// has duplicates just like the copy table does... why?
					if (!RelationShipData.Entries.ContainsKey(index))
						RelationShipData.Entries.Add(index, foreignKey);
				}

				FieldStructure.Add(new FieldStructureEntry(0, 0));
				ColumnMeta.Add(new ColumnStructureEntry());
			}

			// Record Data
			BitStream bitStream = new BitStream(recordData);
			for (int i = 0; i < RecordCount; i++)
			{
				int id = 0;

				if (HasOffsetTable && HasIndexTable)
				{
					id = m_indexes[CopyTable.Count];
					var map = offsetmap[i];

					if (CopyTableSize == 0 && firstindex[map.Item1].HiddenIndex != i) //Ignore duplicates
						continue;

					dbReader.BaseStream.Position = map.Item1;

					byte[] data = dbReader.ReadBytes(map.Item2);

					IEnumerable<byte> recordbytes = BitConverter.GetBytes(id).Concat(data);

					// append relationship id
					if (RelationShipData != null)
					{
						// seen cases of missing indicies 
						if (RelationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
							recordbytes = recordbytes.Concat(foreignData);
						else
							recordbytes = recordbytes.Concat(new byte[4]);
					}

					CopyTable.Add(id, recordbytes.ToArray());

					if (Copies.ContainsKey(id))
					{
						foreach (int copy in Copies[id])
							CopyTable.Add(copy, BitConverter.GetBytes(copy).Concat(data).ToArray());
					}
				}
				else
				{
					bitStream.Seek(i * RecordSize, 0);
					int idOffset = 0;

					List<byte> data = new List<byte>();

					if (HasIndexTable)
					{
						id = m_indexes[i];
						data.AddRange(BitConverter.GetBytes(id));
					}

					int c = HasIndexTable ? 1 : 0;
					for (int f = 0; f < FieldCount; f++)
					{
						int bitOffset = ColumnMeta[f].BitOffset;
						int bitWidth = ColumnMeta[f].BitWidth;
						int cardinality = ColumnMeta[f].Cardinality;
						uint palletIndex;
						int take = columnSizes[c] * ColumnMeta[f].ArraySize;

						switch (ColumnMeta[f].CompressionType)
						{
							case CompressionType.None:
								int bitSize = FieldStructure[f].BitCount;
								if (!HasIndexTable && f == IdIndex)
								{
									idOffset = data.Count;
									id = bitStream.ReadInt32(bitSize); // always read Ids as ints
									data.AddRange(BitConverter.GetBytes(id));
								}
								else
								{
									data.AddRange(bitStream.ReadBytes(bitSize * ColumnMeta[f].ArraySize, false, take));
								}
								break;

							case CompressionType.Immediate:
							case CompressionType.SignedImmediate:
								if (!HasIndexTable && f == IdIndex)
								{
									idOffset = data.Count;
									id = bitStream.ReadInt32(bitWidth); // always read Ids as ints
									data.AddRange(BitConverter.GetBytes(id));
								}
								else
								{
									data.AddRange(bitStream.ReadBytes(bitWidth, false, take));
								}
								break;

							case CompressionType.Sparse:
								if (ColumnMeta[f].SparseValues.TryGetValue(id, out byte[] valBytes))
									data.AddRange(valBytes.Take(take));
								else
									data.AddRange(BitConverter.GetBytes(ColumnMeta[f].BitOffset).Take(take));
								break;

							case CompressionType.Pallet:
							case CompressionType.PalletArray:
								palletIndex = bitStream.ReadUInt32(bitWidth);
								data.AddRange(ColumnMeta[f].PalletValues[(int)palletIndex].Take(take));
								break;

							default:
								throw new Exception($"Unknown compression {ColumnMeta[f].CompressionType}");

						}

						c += ColumnMeta[f].ArraySize;
					}

					// append relationship id
					if (RelationShipData != null)
					{
						// seen cases of missing indicies 
						if (RelationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
							data.AddRange(foreignData);
						else
							data.AddRange(new byte[4]);
					}

					CopyTable.Add(id, data.ToArray());

					if (Copies.ContainsKey(id))
					{
						foreach (int copy in Copies[id])
						{
							byte[] newrecord = CopyTable[id].ToArray();
							Buffer.BlockCopy(BitConverter.GetBytes(copy), 0, newrecord, idOffset, 4);
							CopyTable.Add(copy, newrecord);
						}
					}
				}
			}

			if (HasIndexTable)
			{
				FieldStructure.Insert(0, new FieldStructureEntry(0, 0));
				ColumnMeta.Insert(0, new ColumnStructureEntry());
			}

			offsetmap.Clear();
			firstindex.Clear();
			OffsetDuplicates.Clear();
			Copies.Clear();
			Array.Resize(ref recordData, 0);
			bitStream.Dispose();
			ColumnMeta.ForEach(x => { x.PalletValues?.Clear(); x.SparseValues?.Clear(); });

			InternalRecordSize = (uint)CopyTable.First().Value.Length;

			if (CopyTableSize > 0)
				CopyTable = CopyTable.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

			return CopyTable;
		}

		public override byte[] ReadData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = ReadOffsetData(dbReader, pos);
			OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
			return CopyTable.Values.SelectMany(x => x).ToArray();
		}

		public virtual Dictionary<int, string> ReadStringTable(BinaryReader dbReader)
		{
			long pos = dbReader.BaseStream.Position;
			return new StringTable().Read(dbReader, pos, pos + StringBlockSize);
		}


		public void LoadDefinitionSizes(DBEntry entry)
		{
			if (HasOffsetTable)
				return;

			Dictionary<TypeCode, int> typeLookup = new Dictionary<TypeCode, int>()
			{
				{ TypeCode.Byte, 1 },
				{ TypeCode.SByte, 1 },
				{ TypeCode.UInt16, 2 },
				{ TypeCode.Int16, 2 },
				{ TypeCode.Int32, 4 },
				{ TypeCode.UInt32, 4 },
				{ TypeCode.Int64, 8 },
				{ TypeCode.UInt64, 8 },
				{ TypeCode.String, 4 },
				{ TypeCode.Single, 4 }
			};
			columnSizes = entry.Data.Columns.Cast<DataColumn>().Select(x => typeLookup[Type.GetTypeCode(x.DataType)]).ToArray();
		}

		public void AddRelationshipColumn(DBEntry entry)
		{
			if (RelationShipData == null)
				return;

			if (!entry.Data.Columns.Cast<DataColumn>().Any(x => x.ExtendedProperties.ContainsKey("RELATIONSHIP")))
			{
				DataColumn dataColumn = new DataColumn("RelationshipData", typeof(uint));
				dataColumn.ExtendedProperties.Add("RELATIONSHIP", true);
				entry.Data.Columns.Add(dataColumn);
			}
		}
		#endregion


		#region Write

		public override void WriteHeader(BinaryWriter bw, DBEntry entry)
		{
			Tuple<int, int> minmax = entry.MinMax();
			bw.BaseStream.Position = 0;

			// fix the bitlimits
			RemoveBitLimits();

			WriteBaseHeader(bw, entry);

			bw.Write((int)TableHash);
			bw.Write(LayoutHash);
			bw.Write(minmax.Item1); //MinId
			bw.Write(minmax.Item2); //MaxId
			bw.Write(Locale);
			bw.Write(0); //CopyTableSize
			bw.Write((ushort)Flags); //Flags
			bw.Write(IdIndex); //IdColumn
			bw.Write(TotalFieldSize);

			bw.Write(PackedDataOffset);
			bw.Write(RelationshipCount);
			bw.Write(0);  // OffsetTableOffset
			bw.Write(0);  // IndexSize
			bw.Write(0);  // ColumnMetadataSize
			bw.Write(0);  // SparseDataSize
			bw.Write(0);  // PalletDataSize
			bw.Write(0);  // RelationshipDataSize

			// Write the field_structure bits
			for (int i = 0; i < FieldStructure.Count; i++)
			{
				if (HasIndexTable && i == 0) continue;
				if (RelationShipData != null && i == FieldStructure.Count - 1) continue;

				bw.Write(FieldStructure[i].Bits);
				bw.Write(FieldStructure[i].Offset);
			}

			WriteData(bw, entry);
		}

		/// <summary>
		/// WDC1 writing is entirely different so has been moved to inside the class.
		/// Will work on inheritence when WDC2 comes along - can't wait...
		/// </summary>
		/// <param name="bw"></param>
		/// <param name="entry"></param>

		public virtual void WriteData(BinaryWriter bw, DBEntry entry)
		{
			var offsetMap = new List<Tuple<int, short>>();
			var stringTable = new StringTable(true);
			var IsSparse = HasIndexTable && HasOffsetTable;
			var copyRecords = new Dictionary<int, IEnumerable<int>>();
			var copyIds = new HashSet<int>();

			long pos = bw.BaseStream.Position;

			// get a list of identical records			
			if (CopyTableSize > 0)
			{
				var copyids = Enumerable.Empty<int>();
				var copies = entry.GetCopyRows();
				foreach (var c in copies)
				{
					int id = c.First();
					copyRecords.Add(id, c.Skip(1).ToList());
					copyids = copyids.Concat(copyRecords[id]);
				}

				copyIds = new HashSet<int>(copyids);
			}

			// get relationship data
			DataColumn relationshipColumn = entry.Data.Columns.Cast<DataColumn>().FirstOrDefault(x => x.ExtendedProperties.ContainsKey("RELATIONSHIP"));
			if (relationshipColumn != null)
			{
				int index = entry.Data.Columns.IndexOf(relationshipColumn);

				Dictionary<int, uint> relationData = new Dictionary<int, uint>();
				foreach (DataRow r in entry.Data.Rows)
				{
					int id = r.Field<int>(entry.Key);
					if (!copyIds.Contains(id))
						relationData.Add(id, r.Field<uint>(index));
				}

				RelationShipData = new RelationShipData()
				{
					Records = (uint)relationData.Count,
					MinId = relationData.Values.Min(),
					MaxId = relationData.Values.Max(),
					Entries = relationData.Values.Select((x, i) => new { Index = (uint)i, Id = x }).ToDictionary(x => x.Index, x => BitConverter.GetBytes(x.Id))
				};

				relationData.Clear();
			}

			// temporarily remove the fake records
			if (HasIndexTable)
			{
				FieldStructure.RemoveAt(0);
				ColumnMeta.RemoveAt(0);
			}
			if (RelationShipData != null)
			{
				FieldStructure.RemoveAt(FieldStructure.Count - 1);
				ColumnMeta.RemoveAt(ColumnMeta.Count - 1);
			}

			// remove any existing column values
			ColumnMeta.ForEach(x => { x.PalletValues?.Clear(); x.SparseValues?.Clear(); });

			// RecordData - this can still all be done via one function, except inline strings
			BitStream bitStream = new BitStream(entry.Data.Rows.Count * ColumnMeta.Max(x => x.RecordOffset));
			for (int rowIndex = 0; rowIndex < entry.Data.Rows.Count; rowIndex++)
			{
				Queue<object> rowData = new Queue<object>(entry.Data.Rows[rowIndex].ItemArray);

				int id = entry.Data.Rows[rowIndex].Field<int>(entry.Key);
				bool isCopyRecord = copyIds.Contains(id);

				if (HasIndexTable) // dump the id from the row data
					rowData.Dequeue();

				bitStream.SeekNextOffset(); // each row starts at a 0 bit position

				long offset = pos + bitStream.Offset; // used for offset map calcs

				for (int fieldIndex = 0; fieldIndex < FieldCount; fieldIndex++)
				{
					int bitWidth = ColumnMeta[fieldIndex].BitWidth;
					int bitSize = FieldStructure[fieldIndex].BitCount;
					int arraySize = ColumnMeta[fieldIndex].ArraySize;

					// get the values for the current record, array size may require more than 1
					object[] values = ExtractFields(rowData, stringTable, bitStream, fieldIndex, out bool isString);
					byte[][] data = values.Select(x => (byte[])BitConverter.GetBytes((dynamic)x)).ToArray(); // shameful hack
					if (data.Length == 0)
						continue;

					CompressionType compression = ColumnMeta[fieldIndex].CompressionType;

					if (isCopyRecord && compression != CompressionType.Sparse) // copy records still store the sparse data
						continue;

					switch (compression)
					{
						case CompressionType.None:
							for (int i = 0; i < arraySize; i++)
								bitStream.WriteBits(data[i], bitSize);
							break;

						case CompressionType.Immediate:
						case CompressionType.SignedImmediate:
							bitStream.WriteBits(data[0], bitWidth);
							break;

						case CompressionType.Sparse:
							{
								Array.Resize(ref data[0], 4);
								if (BitConverter.ToInt32(data[0], 0) != ColumnMeta[fieldIndex].BitOffset)
									ColumnMeta[fieldIndex].SparseValues.Add(id, data[0]);
							}
							break;

						case CompressionType.Pallet:
						case CompressionType.PalletArray:
							{
								byte[] combined = data.SelectMany(x => x.Concat(new byte[4]).Take(4)).ToArray(); // enforce int size rule

								int index = ColumnMeta[fieldIndex].PalletValues.FindIndex(x => x.SequenceEqual(combined));
								if (index > -1)
								{
									bitStream.WriteUInt32((uint)index, bitWidth);
								}
								else
								{
									bitStream.WriteUInt32((uint)ColumnMeta[fieldIndex].PalletValues.Count, bitWidth);
									ColumnMeta[fieldIndex].PalletValues.Add(combined);
								}
							}
							break;

						default:
							throw new Exception("Unsupported compression type " + ColumnMeta[fieldIndex].CompressionType);

					}
				}

				if (isCopyRecord)
					continue; // copy records aren't real rows so skip the padding

				bitStream.SeekNextOffset();
				short size = (short)(pos + bitStream.Offset - offset);

				if (IsSparse) // matches itemsparse padding
				{
					int remaining = size % 8 == 0 ? 0 : 8 - (size % 8);
					if (remaining > 0)
					{
						size += (short)remaining;
						bitStream.WriteBytes(new byte[remaining], remaining);
					}

					offsetMap.Add(new Tuple<int, short>((int)offset, size));
				}
				else // needs to be padded to the record size regardless of the byte count - weird eh?
				{
					if (size < RecordSize)
						bitStream.WriteBytes(new byte[RecordSize - size], RecordSize - size);
				}
			}
			bitStream.CopyStreamTo(bw.BaseStream); // write to the filestream
			bitStream.Dispose();

			// OffsetTable / StringTable, either or
			if (IsSparse)
			{
				// OffsetTable
				OffsetTableOffset = (int)bw.BaseStream.Position;
				WriteOffsetMap(bw, entry, offsetMap);
				offsetMap.Clear();
			}
			else
			{
				// StringTable
				StringBlockSize = (uint)stringTable.Size;
				stringTable.CopyTo(bw.BaseStream);
				stringTable.Dispose();
			}

			// IndexTable
			if (HasIndexTable)
			{
				pos = bw.BaseStream.Position;
				WriteIndexTable(bw, entry);
				IndexSize = (int)(bw.BaseStream.Position - pos);
			}

			// Copytable
			if (CopyTableSize > 0)
			{
				pos = bw.BaseStream.Position;
				foreach (var c in copyRecords)
				{
					foreach (var v in c.Value)
					{
						bw.Write(v);
						bw.Write(c.Key);
					}
				}
				CopyTableSize = (int)(bw.BaseStream.Position - pos);
				copyRecords.Clear();
				copyIds.Clear();
			}

			// ColumnMeta
			pos = bw.BaseStream.Position;
			foreach (var meta in ColumnMeta)
			{
				bw.Write(meta.RecordOffset);
				bw.Write(meta.Size);

				if (meta.SparseValues != null)
					bw.Write((uint)meta.SparseValues.Count * 8); // (k<4>, v<4>)
				else if (meta.PalletValues != null)
					bw.Write((uint)meta.PalletValues.Sum(x => x.Length));
				else
					bw.WriteUInt32(0);

				bw.Write((uint)meta.CompressionType);
				bw.Write(meta.BitOffset);
				bw.Write(meta.BitWidth);
				bw.Write(meta.Cardinality);
			}
			ColumnMetadataSize = (int)(bw.BaseStream.Position - pos);

			// Pallet values
			pos = bw.BaseStream.Position;
			foreach (var meta in ColumnMeta)
			{
				if (meta.CompressionType == CompressionType.Pallet || meta.CompressionType == CompressionType.PalletArray)
					bw.WriteArray(meta.PalletValues.SelectMany(x => x).ToArray());
			}
			PalletDataSize = (int)(bw.BaseStream.Position - pos);

			// Sparse values
			pos = bw.BaseStream.Position;
			foreach (var meta in ColumnMeta)
			{
				if (meta.CompressionType == CompressionType.Sparse)
				{
					foreach (var sparse in meta.SparseValues)
					{
						bw.Write(sparse.Key);
						bw.WriteArray(sparse.Value);
					}
				}
			}
			SparseDataSize = (int)(bw.BaseStream.Position - pos);

			// Relationships
			pos = bw.BaseStream.Position;
			if (RelationShipData != null)
			{
				bw.Write(RelationShipData.Records);
				bw.Write(RelationShipData.MinId);
				bw.Write(RelationShipData.MaxId);

				foreach (var relation in RelationShipData.Entries)
				{
					bw.Write(relation.Value);
					bw.Write(relation.Key);
				}
			}
			RelationshipDataSize = (int)(bw.BaseStream.Position - pos);

			// update header fields
			bw.BaseStream.Position = 16;
			bw.Write(StringBlockSize);
			bw.BaseStream.Position = 40;
			bw.Write(CopyTableSize);
			bw.BaseStream.Position = 60;
			bw.Write(OffsetTableOffset);
			bw.Write(IndexSize);
			bw.Write(ColumnMetadataSize);
			bw.Write(SparseDataSize);
			bw.Write(PalletDataSize);
			bw.Write(RelationshipDataSize);

			// reset indextable stuff
			if (HasIndexTable)
			{
				FieldStructure.Insert(0, new FieldStructureEntry(0, 0));
				ColumnMeta.Insert(0, new ColumnStructureEntry());
			}
			if (RelationShipData != null)
			{
				FieldStructure.Add(new FieldStructureEntry(0, 0));
				ColumnMeta.Add(new ColumnStructureEntry());
			}
		}

		protected object[] ExtractFields(Queue<object> rowData, StringTable stringTable, BitStream bitStream, int fieldIndex, out bool isString)
		{
			object[] values = Enumerable.Range(0, ColumnMeta[fieldIndex].ArraySize).Select(x => rowData.Dequeue()).ToArray();
			isString = false;

			// deal with strings
			if (values.Any(x => x.GetType() == typeof(string)))
			{
				isString = true;

				if (HasIndexTable && HasOffsetTable)
				{
					foreach (var s in values)
						bitStream.WriteCString((string)s);

					return new object[0];
				}
				else
				{
					for (int i = 0; i < values.Length; i++)
						values[i] = stringTable.Write((string)values[i], false, false);
				}
			}

			return values;
		}

		#endregion


		protected void RemoveBitLimits()
		{
			if (HasOffsetTable)
				return;

			int c = HasIndexTable ? 1 : 0;
			int cm = ColumnMeta.Count - (RelationShipData != null ? 1 : 0);

			var skipType = new HashSet<CompressionType>(new[] { CompressionType.None, CompressionType.Sparse });

			for (int i = c; i < cm; i++)
			{
				var col = ColumnMeta[i];
				var type = col.CompressionType;
				int oldsize = col.BitWidth;
				ushort newsize = (ushort)(columnSizes[c] * 8);

				c += col.ArraySize;

				if (skipType.Contains(col.CompressionType) || newsize == oldsize)
					continue;

				col.BitWidth = col.Size = newsize;

				for (int x = i + 1; x < cm; x++)
				{
					if (skipType.Contains(ColumnMeta[x].CompressionType))
						continue;

					ColumnMeta[x].RecordOffset += (ushort)(newsize - oldsize);
					ColumnMeta[x].BitOffset = ColumnMeta[x].RecordOffset - (PackedDataOffset * 8);
				}
			}

			RecordSize = (uint)((ColumnMeta.Sum(x => x.Size) + 7) / 8);
		}
	}


	class WDC2 : WDC1
	{

		public int SectionCount; // always 1
		public int Unknown1; // always 0
		public int Unknown2; // always 0
		public int RecordDataOffset;
		public int RecordDataRowCount;
		public int RecordDataStringSize;

		protected int stringTableOffset;
		protected List<int> recordOffsets;
		protected List<int> columnOffsets;

		#region Read
		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			ReadBaseHeader(ref dbReader, signature);

			TableHash = dbReader.ReadUInt32();
			LayoutHash = dbReader.ReadInt32();
			MinId = dbReader.ReadInt32();
			MaxId = dbReader.ReadInt32();
			Locale = dbReader.ReadInt32();
			Flags = (HeaderFlags)dbReader.ReadUInt16();
			IdIndex = dbReader.ReadUInt16();
			TotalFieldSize = dbReader.ReadUInt32();
			PackedDataOffset = dbReader.ReadInt32();

			RelationshipCount = dbReader.ReadUInt32();
			ColumnMetadataSize = dbReader.ReadInt32();
			SparseDataSize = dbReader.ReadInt32();
			PalletDataSize = dbReader.ReadInt32();

			SectionCount = dbReader.ReadInt32();

			// TODO convert to array when the time comes
			Unknown1 = dbReader.ReadInt32();
			Unknown2 = dbReader.ReadInt32();
			RecordDataOffset = dbReader.ReadInt32();
			RecordDataRowCount = dbReader.ReadInt32();
			RecordDataStringSize = dbReader.ReadInt32();
			CopyTableSize = dbReader.ReadInt32();
			OffsetTableOffset = dbReader.ReadInt32();
			IndexSize = dbReader.ReadInt32();
			RelationshipDataSize = dbReader.ReadInt32();

			if (RecordCount == 0 || FieldCount == 0)
				return;

			//Gather field structures
			FieldStructure = new List<FieldStructureEntry>();
			for (int i = 0; i < FieldCount; i++)
			{
				var field = new FieldStructureEntry(dbReader.ReadInt16(), dbReader.ReadUInt16());
				FieldStructure.Add(field);
			}

			// ColumnMeta
			ColumnMeta = new List<ColumnStructureEntry>();
			for (int i = 0; i < FieldCount; i++)
			{
				var column = new ColumnStructureEntry()
				{
					RecordOffset = dbReader.ReadUInt16(),
					Size = dbReader.ReadUInt16(),
					AdditionalDataSize = dbReader.ReadUInt32(), // size of pallet / sparse values
					CompressionType = (CompressionType)dbReader.ReadUInt32(),
					BitOffset = dbReader.ReadInt32(),
					BitWidth = dbReader.ReadInt32(),
					Cardinality = dbReader.ReadInt32()
				};

				// preload arraysizes
				if (column.CompressionType == CompressionType.None)
					column.ArraySize = Math.Max(column.Size / FieldStructure[i].BitCount, 1);
				else if (column.CompressionType == CompressionType.PalletArray)
					column.ArraySize = Math.Max(column.Cardinality, 1);

				ColumnMeta.Add(column);
			}

			// Pallet values
			for (int i = 0; i < ColumnMeta.Count; i++)
			{
				if (ColumnMeta[i].CompressionType == CompressionType.Pallet || ColumnMeta[i].CompressionType == CompressionType.PalletArray)
				{
					int elements = (int)ColumnMeta[i].AdditionalDataSize / 4;
					int cardinality = Math.Max(ColumnMeta[i].Cardinality, 1);

					ColumnMeta[i].PalletValues = new List<byte[]>();
					for (int j = 0; j < elements / cardinality; j++)
						ColumnMeta[i].PalletValues.Add(dbReader.ReadBytes(cardinality * 4));
				}
			}

			// Sparse values
			for (int i = 0; i < ColumnMeta.Count; i++)
			{
				if (ColumnMeta[i].CompressionType == CompressionType.Sparse)
				{
					ColumnMeta[i].SparseValues = new Dictionary<int, byte[]>();
					for (int j = 0; j < ColumnMeta[i].AdditionalDataSize / 8; j++)
						ColumnMeta[i].SparseValues[dbReader.ReadInt32()] = dbReader.ReadBytes(4);
				}
			}

			// RecordData
			recordData = dbReader.ReadBytes((int)(RecordCount * RecordSize));
			Array.Resize(ref recordData, recordData.Length + 8);

			Flags &= ~HeaderFlags.RelationshipData; // appears to be obsolete now
		}

		public new Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = new Dictionary<int, byte[]>();
			List<Tuple<int, short>> offsetmap = new List<Tuple<int, short>>();
			Dictionary<int, OffsetDuplicate> firstindex = new Dictionary<int, OffsetDuplicate>();
			Dictionary<int, List<int>> Copies = new Dictionary<int, List<int>>();

			columnOffsets = new List<int>();
			recordOffsets = new List<int>();
			int[] m_indexes = null;

			// OffsetTable
			if (HasOffsetTable && OffsetTableOffset > 0)
			{
				dbReader.BaseStream.Position = OffsetTableOffset;
				for (int i = 0; i < (MaxId - MinId + 1); i++)
				{
					int offset = dbReader.ReadInt32();
					short length = dbReader.ReadInt16();

					if (offset == 0 || length == 0)
						continue;

					// special case, may contain duplicates in the offset map that we don't want
					if (CopyTableSize == 0)
					{
						if (!firstindex.ContainsKey(offset))
							firstindex.Add(offset, new OffsetDuplicate(offsetmap.Count, firstindex.Count));
						else
							continue;
					}

					offsetmap.Add(new Tuple<int, short>(offset, length));
				}
			}

			// IndexTable
			if (HasIndexTable)
			{
				m_indexes = new int[RecordCount];
				for (int i = 0; i < RecordCount; i++)
					m_indexes[i] = dbReader.ReadInt32();
			}

			// Copytable
			if (CopyTableSize > 0)
			{
				long end = dbReader.BaseStream.Position + CopyTableSize;
				while (dbReader.BaseStream.Position < end)
				{
					int id = dbReader.ReadInt32();
					int idcopy = dbReader.ReadInt32();

					if (!Copies.ContainsKey(idcopy))
						Copies.Add(idcopy, new List<int>());

					Copies[idcopy].Add(id);
				}
			}

			// Relationships
			if (RelationshipDataSize > 0)
			{
				RelationShipData = new RelationShipData()
				{
					Records = dbReader.ReadUInt32(),
					MinId = dbReader.ReadUInt32(),
					MaxId = dbReader.ReadUInt32(),
					Entries = new Dictionary<uint, byte[]>()
				};

				for (int i = 0; i < RelationShipData.Records; i++)
				{
					byte[] foreignKey = dbReader.ReadBytes(4);
					uint index = dbReader.ReadUInt32();
					// has duplicates just like the copy table does... why?
					if (!RelationShipData.Entries.ContainsKey(index))
						RelationShipData.Entries.Add(index, foreignKey);
				}

				FieldStructure.Add(new FieldStructureEntry(0, 0));
				ColumnMeta.Add(new ColumnStructureEntry());
			}

			// Record Data
			BitStream bitStream = new BitStream(recordData);
			for (int i = 0; i < RecordCount; i++)
			{
				int id = 0;

				if (HasOffsetTable && HasIndexTable)
				{
					id = m_indexes[CopyTable.Count];
					var map = offsetmap[i];

					if (CopyTableSize == 0 && firstindex[map.Item1].HiddenIndex != i) //Ignore duplicates
						continue;

					dbReader.BaseStream.Position = map.Item1;

					byte[] data = dbReader.ReadBytes(map.Item2);

					IEnumerable<byte> recordbytes = BitConverter.GetBytes(id).Concat(data);

					// append relationship id
					if (RelationShipData != null)
					{
						// seen cases of missing indicies 
						if (RelationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
							recordbytes = recordbytes.Concat(foreignData);
						else
							recordbytes = recordbytes.Concat(new byte[4]);
					}

					CopyTable.Add(id, recordbytes.ToArray());

					if (Copies.ContainsKey(id))
					{
						foreach (int copy in Copies[id])
							CopyTable.Add(copy, BitConverter.GetBytes(copy).Concat(data).ToArray());
					}
				}
				else
				{
					bitStream.Seek(i * RecordSize, 0);
					int idOffset = 0;

					if (StringBlockSize > 0)
						recordOffsets.Add((int)bitStream.Offset);

					List<byte> data = new List<byte>();

					if (HasIndexTable)
					{
						id = m_indexes[i];
						data.AddRange(BitConverter.GetBytes(id));
					}

					int c = HasIndexTable ? 1 : 0;
					for (int f = 0; f < FieldCount; f++)
					{
						int bitOffset = ColumnMeta[f].BitOffset;
						int bitWidth = ColumnMeta[f].BitWidth;
						int cardinality = ColumnMeta[f].Cardinality;
						uint palletIndex;
						int take = columnSizes[c] * ColumnMeta[f].ArraySize;

						switch (ColumnMeta[f].CompressionType)
						{
							case CompressionType.None:
								int bitSize = FieldStructure[f].BitCount;
								if (!HasIndexTable && f == IdIndex)
								{
									idOffset = data.Count;
									id = bitStream.ReadInt32(bitSize); // always read Ids as ints
									data.AddRange(BitConverter.GetBytes(id));
								}
								else
								{
									for (int x = 0; x < ColumnMeta[f].ArraySize; x++)
									{
										if (i == 0)
											columnOffsets.Add((int)(bitStream.Offset + (bitStream.BitPosition >> 3)));

										data.AddRange(bitStream.ReadBytes(bitSize, false, columnSizes[c]));
									}
								}
								break;

							case CompressionType.Immediate:
							case CompressionType.SignedImmediate:
								if (!HasIndexTable && f == IdIndex)
								{
									idOffset = data.Count;
									id = bitStream.ReadInt32(bitWidth); // always read Ids as ints
									data.AddRange(BitConverter.GetBytes(id));
								}
								else
								{
									if (i == 0)
										columnOffsets.Add((int)(bitStream.Offset + (bitStream.BitPosition >> 3)));

									data.AddRange(bitStream.ReadBytes(bitWidth, false, take));
								}
								break;

							case CompressionType.Sparse:

								if (i == 0)
									columnOffsets.Add((int)(bitStream.Offset + (bitStream.BitPosition >> 3)));

								if (ColumnMeta[f].SparseValues.TryGetValue(id, out byte[] valBytes))
									data.AddRange(valBytes.Take(take));
								else
									data.AddRange(BitConverter.GetBytes(ColumnMeta[f].BitOffset).Take(take));
								break;

							case CompressionType.Pallet:
							case CompressionType.PalletArray:

								if (i == 0)
									columnOffsets.Add((int)(bitStream.Offset + (bitStream.BitPosition >> 3)));

								palletIndex = bitStream.ReadUInt32(bitWidth);
								data.AddRange(ColumnMeta[f].PalletValues[(int)palletIndex].Take(take));
								break;

							default:
								throw new Exception($"Unknown compression {ColumnMeta[f].CompressionType}");

						}

						c += ColumnMeta[f].ArraySize;
					}

					// append relationship id
					if (RelationShipData != null)
					{
						// seen cases of missing indicies 
						if (RelationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
							data.AddRange(foreignData);
						else
							data.AddRange(new byte[4]);
					}

					CopyTable.Add(id, data.ToArray());

					if (Copies.ContainsKey(id))
					{
						foreach (int copy in Copies[id])
						{
							byte[] newrecord = CopyTable[id].ToArray();
							Buffer.BlockCopy(BitConverter.GetBytes(copy), 0, newrecord, idOffset, 4);
							CopyTable.Add(copy, newrecord);

							if (StringBlockSize > 0)
								recordOffsets.Add(recordOffsets.Last());
						}
					}
				}
			}

			if (HasIndexTable)
			{
				FieldStructure.Insert(0, new FieldStructureEntry(0, 0));
				ColumnMeta.Insert(0, new ColumnStructureEntry());
			}

			offsetmap.Clear();
			firstindex.Clear();
			OffsetDuplicates.Clear();
			Copies.Clear();
			Array.Resize(ref recordData, 0);
			bitStream.Dispose();
			ColumnMeta.ForEach(x => { x.PalletValues?.Clear(); x.SparseValues?.Clear(); });

			InternalRecordSize = (uint)CopyTable.First().Value.Length;

			if (CopyTableSize > 0)
			{
				var sort = CopyTable.Select((x, i) => new { CT = x, Off = recordOffsets[i] }).OrderBy(x => x.CT.Key);
				recordOffsets = sort.Select(x => x.Off).ToList();
				CopyTable = sort.ToDictionary(x => x.CT.Key, x => x.CT.Value);
			}

			return CopyTable;
		}

		public override byte[] ReadData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = ReadOffsetData(dbReader, pos);
			OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
			return CopyTable.Values.SelectMany(x => x).ToArray();
		}

		public override Dictionary<int, string> ReadStringTable(BinaryReader dbReader)
		{
			stringTableOffset = (int)dbReader.BaseStream.Position;
			return new StringTable().Read(dbReader, stringTableOffset, stringTableOffset + StringBlockSize, true);
		}

		public override int GetStringOffset(BinaryReader dbReader, int j, uint i)
		{
			if (HasIndexTable)
				j--;

			return dbReader.ReadInt32() + RecordDataOffset + columnOffsets[j] + recordOffsets[(int)i];
		}

		#endregion

		#region Write

		public override void WriteHeader(BinaryWriter bw, DBEntry entry)
		{
			Tuple<int, int> minmax = entry.MinMax();
			bw.BaseStream.Position = 0;

			// fix the bitlimits
			RemoveBitLimits();

			WriteBaseHeader(bw, entry);

			bw.Write((int)TableHash);
			bw.Write(LayoutHash);
			bw.Write(minmax.Item1); //MinId
			bw.Write(minmax.Item2); //MaxId
			bw.Write(Locale);
			bw.Write((ushort)Flags); //Flags
			bw.Write(IdIndex); //IdColumn
			bw.Write(TotalFieldSize);
			bw.Write(PackedDataOffset);
			bw.Write(RelationshipCount);

			bw.Write(0);  // ColumnMetadataSize
			bw.Write(0);  // SparseDataSize
			bw.Write(0);  // PalletDataSize
			bw.Write(SectionCount);
			bw.Write(Unknown1);
			bw.Write(Unknown2);

			bw.Write(0);  // RecordDataOffset					
			if (entry.Header.CopyTableSize > 0) // RecordDataRowCount
				bw.Write(entry.GetUniqueRows().Count());
			else
				bw.Write(entry.Data.Rows.Count);

			bw.Write(0);  //RecordDataStringSize
			bw.Write(0); //CopyTableSize
			bw.Write(0); //OffsetTableOffset
			bw.Write(0); //IndexSize
			bw.Write(0); //RelationshipDataSize


			// Write the field_structure bits
			for (int i = 0; i < FieldStructure.Count; i++)
			{
				if (HasIndexTable && i == 0) continue;
				if (RelationShipData != null && i == FieldStructure.Count - 1) continue;

				bw.Write(FieldStructure[i].Bits);
				bw.Write(FieldStructure[i].Offset);
			}

			WriteData(bw, entry);
		}

		/// <summary>
		/// WDC1 writing is entirely different so has been moved to inside the class.
		/// Will work on inheritence when WDC2 comes along - can't wait...
		/// </summary>
		/// <param name="bw"></param>
		/// <param name="entry"></param>

		public override void WriteData(BinaryWriter bw, DBEntry entry)
		{
			var offsetMap = new List<Tuple<int, short>>();
			var stringTable = new StringTable(true);
			var IsSparse = HasIndexTable && HasOffsetTable;
			var copyRecords = new Dictionary<int, IEnumerable<int>>();
			var copyIds = new HashSet<int>();

			Dictionary<Tuple<long, int>, int> stringLookup = new Dictionary<Tuple<long, int>, int>();

			long pos = bw.BaseStream.Position;

			// get a list of identical records			
			if (CopyTableSize > 0)
			{
				var copyids = Enumerable.Empty<int>();
				var copies = entry.GetCopyRows();
				foreach (var c in copies)
				{
					int id = c.First();
					copyRecords.Add(id, c.Skip(1).ToList());
					copyids = copyids.Concat(copyRecords[id]);
				}

				copyIds = new HashSet<int>(copyids);
			}

			// get relationship data
			DataColumn relationshipColumn = entry.Data.Columns.Cast<DataColumn>().FirstOrDefault(x => x.ExtendedProperties.ContainsKey("RELATIONSHIP"));
			if (relationshipColumn != null)
			{
				int index = entry.Data.Columns.IndexOf(relationshipColumn);

				Dictionary<int, uint> relationData = new Dictionary<int, uint>();
				foreach (DataRow r in entry.Data.Rows)
				{
					int id = r.Field<int>(entry.Key);
					if (!copyIds.Contains(id))
						relationData.Add(id, r.Field<uint>(index));
				}

				RelationShipData = new RelationShipData()
				{
					Records = (uint)relationData.Count,
					MinId = relationData.Values.Min(),
					MaxId = relationData.Values.Max(),
					Entries = relationData.Values.Select((x, i) => new { Index = (uint)i, Id = x }).ToDictionary(x => x.Index, x => BitConverter.GetBytes(x.Id))
				};

				relationData.Clear();
			}

			// temporarily remove the fake records
			if (HasIndexTable)
			{
				FieldStructure.RemoveAt(0);
				ColumnMeta.RemoveAt(0);
			}
			if (RelationShipData != null)
			{
				FieldStructure.RemoveAt(FieldStructure.Count - 1);
				ColumnMeta.RemoveAt(ColumnMeta.Count - 1);
			}

			// remove any existing column values
			ColumnMeta.ForEach(x => { x.PalletValues?.Clear(); x.SparseValues?.Clear(); });

			// RecordData - this can still all be done via one function, except inline strings
			BitStream bitStream = new BitStream(entry.Data.Rows.Count * ColumnMeta.Max(x => x.RecordOffset));
			for (int rowIndex = 0; rowIndex < entry.Data.Rows.Count; rowIndex++)
			{
				Queue<object> rowData = new Queue<object>(entry.Data.Rows[rowIndex].ItemArray);

				int id = entry.Data.Rows[rowIndex].Field<int>(entry.Key);
				bool isCopyRecord = copyIds.Contains(id);

				if (HasIndexTable) // dump the id from the row data
					rowData.Dequeue();

				bitStream.SeekNextOffset(); // each row starts at a 0 bit position

				long bitOffset = bitStream.Offset; // used for offset map calcs

				for (int fieldIndex = 0; fieldIndex < FieldCount; fieldIndex++)
				{
					int bitWidth = ColumnMeta[fieldIndex].BitWidth;
					int bitSize = FieldStructure[fieldIndex].BitCount;
					int arraySize = ColumnMeta[fieldIndex].ArraySize;

					// get the values for the current record, array size may require more than 1
					object[] values = ExtractFields(rowData, stringTable, bitStream, fieldIndex, out bool isString);
					byte[][] data = values.Select(x => (byte[])BitConverter.GetBytes((dynamic)x)).ToArray(); // shameful hack
					if (data.Length == 0)
						continue;

					CompressionType compression = ColumnMeta[fieldIndex].CompressionType;
					if (isCopyRecord && compression != CompressionType.Sparse) // copy records still store the sparse data
						continue;

					switch (compression)
					{
						case CompressionType.None:
							for (int i = 0; i < arraySize; i++)
							{
								if (isString)
									stringLookup.Add(new Tuple<long, int>(bitStream.Offset, bitStream.BitPosition), (int)values[i]);

								bitStream.WriteBits(data[i], bitSize);
							}
							break;

						case CompressionType.Immediate:
						case CompressionType.SignedImmediate:
							bitStream.WriteBits(data[0], bitWidth);
							break;

						case CompressionType.Sparse:
							{
								Array.Resize(ref data[0], 4);
								if (BitConverter.ToInt32(data[0], 0) != ColumnMeta[fieldIndex].BitOffset)
									ColumnMeta[fieldIndex].SparseValues.Add(id, data[0]);
							}
							break;

						case CompressionType.Pallet:
						case CompressionType.PalletArray:
							{
								byte[] combined = data.SelectMany(x => x.Concat(new byte[4]).Take(4)).ToArray(); // enforce int size rule

								int index = ColumnMeta[fieldIndex].PalletValues.FindIndex(x => x.SequenceEqual(combined));
								if (index > -1)
								{
									bitStream.WriteUInt32((uint)index, bitWidth);
								}
								else
								{
									bitStream.WriteUInt32((uint)ColumnMeta[fieldIndex].PalletValues.Count, bitWidth);
									ColumnMeta[fieldIndex].PalletValues.Add(combined);
								}
							}
							break;

						default:
							throw new Exception("Unsupported compression type " + ColumnMeta[fieldIndex].CompressionType);

					}
				}

				if (isCopyRecord)
					continue; // copy records aren't real rows so skip the padding

				bitStream.SeekNextOffset();
				short size = (short)(bitStream.Length - bitOffset);

				if (IsSparse) // matches itemsparse padding
				{
					int remaining = size % 8 == 0 ? 0 : 8 - (size % 8);
					if (remaining > 0)
					{
						size += (short)remaining;
						bitStream.WriteBytes(new byte[remaining], remaining);
					}

					offsetMap.Add(new Tuple<int, short>((int)bitOffset, size));
				}
				else // needs to be padded to the record size regardless of the byte count - weird eh?
				{
					if (size < RecordSize)
						bitStream.WriteBytes(new byte[RecordSize - size], RecordSize - size);
				}
			}

			// ColumnMeta
			pos = bw.BaseStream.Position;
			foreach (var meta in ColumnMeta)
			{
				bw.Write(meta.RecordOffset);
				bw.Write(meta.Size);

				if (meta.SparseValues != null)
					bw.Write((uint)meta.SparseValues.Count * 8); // (k<4>, v<4>)
				else if (meta.PalletValues != null)
					bw.Write((uint)meta.PalletValues.Sum(x => x.Length));
				else
					bw.WriteUInt32(0);

				bw.Write((uint)meta.CompressionType);
				bw.Write(meta.BitOffset);
				bw.Write(meta.BitWidth);
				bw.Write(meta.Cardinality);
			}
			ColumnMetadataSize = (int)(bw.BaseStream.Position - pos);

			// Pallet values
			pos = bw.BaseStream.Position;
			foreach (var meta in ColumnMeta)
			{
				if (meta.CompressionType == CompressionType.Pallet || meta.CompressionType == CompressionType.PalletArray)
					bw.WriteArray(meta.PalletValues.SelectMany(x => x).ToArray());
			}
			PalletDataSize = (int)(bw.BaseStream.Position - pos);

			// Sparse values
			pos = bw.BaseStream.Position;
			foreach (var meta in ColumnMeta)
			{
				if (meta.CompressionType == CompressionType.Sparse)
				{
					foreach (var sparse in meta.SparseValues)
					{
						bw.Write(sparse.Key);
						bw.WriteArray(sparse.Value);
					}
				}
			}
			SparseDataSize = (int)(bw.BaseStream.Position - pos);

			// set record data offset
			RecordDataOffset = (int)bw.BaseStream.Position;

			// write string offsets
			if (stringLookup.Count > 0)
			{
				foreach (var lk in stringLookup)
				{
					bitStream.Seek(lk.Key.Item1, lk.Key.Item2);
					bitStream.WriteInt32((int)(lk.Value + bitStream.Length - lk.Key.Item1 - (lk.Key.Item2 >> 3)));
				}
			}

			// push bitstream to 
			bitStream.CopyStreamTo(bw.BaseStream);
			bitStream.Dispose();

			// OffsetTable / StringTable, either or
			if (IsSparse)
			{
				// OffsetTable
				OffsetTableOffset = (int)bw.BaseStream.Position;
				WriteOffsetMap(bw, entry, offsetMap, RecordDataOffset);
				offsetMap.Clear();
			}
			else
			{
				// StringTable
				StringBlockSize = (uint)stringTable.Size;
				stringTable.CopyTo(bw.BaseStream);
				stringTable.Dispose();
			}

			// IndexTable
			if (HasIndexTable)
			{
				pos = bw.BaseStream.Position;
				WriteIndexTable(bw, entry);
				IndexSize = (int)(bw.BaseStream.Position - pos);
			}

			// Copytable
			if (CopyTableSize > 0)
			{
				pos = bw.BaseStream.Position;
				foreach (var c in copyRecords)
				{
					foreach (var v in c.Value)
					{
						bw.Write(v);
						bw.Write(c.Key);
					}
				}
				CopyTableSize = (int)(bw.BaseStream.Position - pos);
				copyRecords.Clear();
				copyIds.Clear();
			}

			// Relationships
			pos = bw.BaseStream.Position;
			if (RelationShipData != null)
			{
				bw.Write(RelationShipData.Records);
				bw.Write(RelationShipData.MinId);
				bw.Write(RelationShipData.MaxId);

				foreach (var relation in RelationShipData.Entries)
				{
					bw.Write(relation.Value);
					bw.Write(relation.Key);
				}
			}
			RelationshipDataSize = (int)(bw.BaseStream.Position - pos);

			// update header fields
			bw.BaseStream.Position = 16;
			bw.Write(StringBlockSize);

			bw.BaseStream.Position = 56;
			bw.Write(ColumnMetadataSize);
			bw.Write(SparseDataSize);
			bw.Write(PalletDataSize);

			bw.BaseStream.Position = 80;
			bw.Write(RecordDataOffset);

			bw.BaseStream.Position = 88;
			bw.Write(StringBlockSize); // record_data_stringtable
			bw.Write(CopyTableSize);
			bw.Write(OffsetTableOffset);
			bw.Write(IndexSize);
			bw.Write(RelationshipDataSize);

			// reset indextable stuff
			if (HasIndexTable)
			{
				FieldStructure.Insert(0, new FieldStructureEntry(0, 0));
				ColumnMeta.Insert(0, new ColumnStructureEntry());
			}
			if (RelationShipData != null)
			{
				FieldStructure.Add(new FieldStructureEntry(0, 0));
				ColumnMeta.Add(new ColumnStructureEntry());
			}
		}

		#endregion


		public override void Clear()
		{
			recordOffsets.Clear();
			columnOffsets.Clear();
		}
	}

	class WDC3 : WDC2
	{
		public int Unknown3;

		#region Read
		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			ReadBaseHeader(ref dbReader, signature);

			TableHash = dbReader.ReadUInt32();
			LayoutHash = dbReader.ReadInt32();
			MinId = dbReader.ReadInt32();
			MaxId = dbReader.ReadInt32();
			Locale = dbReader.ReadInt32();
			Flags = (HeaderFlags)dbReader.ReadUInt16();
			IdIndex = dbReader.ReadUInt16();
			TotalFieldSize = dbReader.ReadUInt32();
			PackedDataOffset = dbReader.ReadInt32();

			RelationshipCount = dbReader.ReadUInt32();
			ColumnMetadataSize = dbReader.ReadInt32();
			SparseDataSize = dbReader.ReadInt32();
			PalletDataSize = dbReader.ReadInt32();

			SectionCount = dbReader.ReadInt32();

			// TODO convert to array when the time comes
			Unknown1 = dbReader.ReadInt32();
			Unknown2 = dbReader.ReadInt32();
			RecordDataOffset = dbReader.ReadInt32(); // record_count
			RecordDataRowCount = dbReader.ReadInt32(); // string_table_size
			RecordDataStringSize = dbReader.ReadInt32(); // offset_records_end
			CopyTableSize = dbReader.ReadInt32(); // id_list_size
			OffsetTableOffset = dbReader.ReadInt32(); // relationship_data_size
			IndexSize = dbReader.ReadInt32(); // offset_map_id_count
			RelationshipDataSize = dbReader.ReadInt32(); //copy_table_count

			if (RecordCount == 0 || FieldCount == 0)
				return;

			//Gather field structures
			FieldStructure = new List<FieldStructureEntry>();
			for (int i = 0; i < TotalFieldSize; i++)
			{
				var field = new FieldStructureEntry(dbReader.ReadInt16(), dbReader.ReadUInt16());
				FieldStructure.Add(field);
			}

			Unknown3 = dbReader.ReadInt32();

			// ColumnMeta
			ColumnMeta = new List<ColumnStructureEntry>();
			for (int i = 0; i < FieldCount; i++)
			{
				var column = new ColumnStructureEntry()
				{
					RecordOffset = dbReader.ReadUInt16(),
					Size = dbReader.ReadUInt16(),
					AdditionalDataSize = dbReader.ReadUInt32(), // size of pallet / sparse values
					CompressionType = (CompressionType)dbReader.ReadUInt32(),
					BitOffset = dbReader.ReadInt32(),
					BitWidth = dbReader.ReadInt32(),
					Cardinality = dbReader.ReadInt32()
				};

				// preload arraysizes
				if (column.CompressionType == CompressionType.None)
					column.ArraySize = Math.Max(column.Size / FieldStructure[i].BitCount, 1);
				else if (column.CompressionType == CompressionType.PalletArray)
					column.ArraySize = Math.Max(column.Cardinality, 1);

				ColumnMeta.Add(column);
			}

			// Pallet values
			for (int i = 0; i < ColumnMeta.Count; i++)
			{
				if (ColumnMeta[i].CompressionType == CompressionType.Pallet || ColumnMeta[i].CompressionType == CompressionType.PalletArray)
				{
					int elements = (int)ColumnMeta[i].AdditionalDataSize / 4;
					int cardinality = Math.Max(ColumnMeta[i].Cardinality, 1);

					ColumnMeta[i].PalletValues = new List<byte[]>();
					for (int j = 0; j < elements / cardinality; j++)
						ColumnMeta[i].PalletValues.Add(dbReader.ReadBytes(cardinality * 4));
				}
			}

			// Sparse values
			for (int i = 0; i < ColumnMeta.Count; i++)
			{
				if (ColumnMeta[i].CompressionType == CompressionType.Sparse)
				{
					ColumnMeta[i].SparseValues = new Dictionary<int, byte[]>();
					for (int j = 0; j < ColumnMeta[i].AdditionalDataSize / 8; j++)
						ColumnMeta[i].SparseValues[dbReader.ReadInt32()] = dbReader.ReadBytes(4);
				}
			}

			// RecordData
			recordData = dbReader.ReadBytes((int)(RecordCount * RecordSize));
			Array.Resize(ref recordData, recordData.Length + 8);

			Flags &= ~HeaderFlags.RelationshipData; // appears to be obsolete now
		}

		public new Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = new Dictionary<int, byte[]>();
			List<Tuple<int, short>> offsetmap = new List<Tuple<int, short>>();
			Dictionary<int, OffsetDuplicate> firstindex = new Dictionary<int, OffsetDuplicate>();
			Dictionary<int, List<int>> Copies = new Dictionary<int, List<int>>();

			columnOffsets = new List<int>();
			recordOffsets = new List<int>();
			int[] m_indexes = null;

			// OffsetTable
			if (HasOffsetTable && OffsetTableOffset > 0)
			{
				dbReader.BaseStream.Position = OffsetTableOffset;
				for (int i = 0; i < (MaxId - MinId + 1); i++)
				{
					int offset = dbReader.ReadInt32();
					short length = dbReader.ReadInt16();

					if (offset == 0 || length == 0)
						continue;

					// special case, may contain duplicates in the offset map that we don't want
					if (CopyTableSize == 0)
					{
						if (!firstindex.ContainsKey(offset))
							firstindex.Add(offset, new OffsetDuplicate(offsetmap.Count, firstindex.Count));
						else
							continue;
					}

					offsetmap.Add(new Tuple<int, short>(offset, length));
				}
			}

			// IndexTable
			if (HasIndexTable)
			{
				m_indexes = new int[RecordCount];
				for (int i = 0; i < RecordCount; i++)
					m_indexes[i] = dbReader.ReadInt32();
			}

			// Copytable
			if (CopyTableSize > 0)
			{
				long end = dbReader.BaseStream.Position + CopyTableSize;
				while (dbReader.BaseStream.Position < end)
				{
					int id = dbReader.ReadInt32();
					int idcopy = dbReader.ReadInt32();

					if (!Copies.ContainsKey(idcopy))
						Copies.Add(idcopy, new List<int>());

					Copies[idcopy].Add(id);
				}
			}

			// Relationships
			if (RelationshipDataSize > 0)
			{
				RelationShipData = new RelationShipData()
				{
					Records = dbReader.ReadUInt32(),
					MinId = dbReader.ReadUInt32(),
					MaxId = dbReader.ReadUInt32(),
					Entries = new Dictionary<uint, byte[]>()
				};

				for (int i = 0; i < RelationShipData.Records; i++)
				{
					byte[] foreignKey = dbReader.ReadBytes(4);
					uint index = dbReader.ReadUInt32();
					// has duplicates just like the copy table does... why?
					if (!RelationShipData.Entries.ContainsKey(index))
						RelationShipData.Entries.Add(index, foreignKey);
				}

				FieldStructure.Add(new FieldStructureEntry(0, 0));
				ColumnMeta.Add(new ColumnStructureEntry());
			}

			// Record Data
			BitStream bitStream = new BitStream(recordData);
			for (int i = 0; i < RecordCount; i++)
			{
				int id = 0;

				if (HasOffsetTable && HasIndexTable)
				{
					id = m_indexes[CopyTable.Count];
					var map = offsetmap[i];

					if (CopyTableSize == 0 && firstindex[map.Item1].HiddenIndex != i) //Ignore duplicates
						continue;

					dbReader.BaseStream.Position = map.Item1;

					byte[] data = dbReader.ReadBytes(map.Item2);

					IEnumerable<byte> recordbytes = BitConverter.GetBytes(id).Concat(data);

					// append relationship id
					if (RelationShipData != null)
					{
						// seen cases of missing indicies 
						if (RelationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
							recordbytes = recordbytes.Concat(foreignData);
						else
							recordbytes = recordbytes.Concat(new byte[4]);
					}

					CopyTable.Add(id, recordbytes.ToArray());

					if (Copies.ContainsKey(id))
					{
						foreach (int copy in Copies[id])
							CopyTable.Add(copy, BitConverter.GetBytes(copy).Concat(data).ToArray());
					}
				}
				else
				{
					bitStream.Seek(i * RecordSize, 0);
					int idOffset = 0;

					if (StringBlockSize > 0)
						recordOffsets.Add((int)bitStream.Offset);

					List<byte> data = new List<byte>();

					if (HasIndexTable)
					{
						id = m_indexes[i];
						data.AddRange(BitConverter.GetBytes(id));
					}

					int c = HasIndexTable ? 1 : 0;
					for (int f = 0; f < FieldCount; f++)
					{
						int bitOffset = ColumnMeta[f].BitOffset;
						int bitWidth = ColumnMeta[f].BitWidth;
						int cardinality = ColumnMeta[f].Cardinality;
						uint palletIndex;
						int take = columnSizes[c] * ColumnMeta[f].ArraySize;

						switch (ColumnMeta[f].CompressionType)
						{
							case CompressionType.None:
								int bitSize = FieldStructure[f].BitCount;
								if (!HasIndexTable && f == IdIndex)
								{
									idOffset = data.Count;
									id = bitStream.ReadInt32(bitSize); // always read Ids as ints
									data.AddRange(BitConverter.GetBytes(id));
								}
								else
								{
									for (int x = 0; x < ColumnMeta[f].ArraySize; x++)
									{
										if (i == 0)
											columnOffsets.Add((int)(bitStream.Offset + (bitStream.BitPosition >> 3)));

										data.AddRange(bitStream.ReadBytes(bitSize, false, columnSizes[c]));
									}
								}
								break;

							case CompressionType.Immediate:
							case CompressionType.SignedImmediate:
								if (!HasIndexTable && f == IdIndex)
								{
									idOffset = data.Count;
									id = bitStream.ReadInt32(bitWidth); // always read Ids as ints
									data.AddRange(BitConverter.GetBytes(id));
								}
								else
								{
									if (i == 0)
										columnOffsets.Add((int)(bitStream.Offset + (bitStream.BitPosition >> 3)));

									data.AddRange(bitStream.ReadBytes(bitWidth, false, take));
								}
								break;

							case CompressionType.Sparse:

								if (i == 0)
									columnOffsets.Add((int)(bitStream.Offset + (bitStream.BitPosition >> 3)));

								if (ColumnMeta[f].SparseValues.TryGetValue(id, out byte[] valBytes))
									data.AddRange(valBytes.Take(take));
								else
									data.AddRange(BitConverter.GetBytes(ColumnMeta[f].BitOffset).Take(take));
								break;

							case CompressionType.Pallet:
							case CompressionType.PalletArray:

								if (i == 0)
									columnOffsets.Add((int)(bitStream.Offset + (bitStream.BitPosition >> 3)));

								palletIndex = bitStream.ReadUInt32(bitWidth);
								data.AddRange(ColumnMeta[f].PalletValues[(int)palletIndex].Take(take));
								break;

							default:
								throw new Exception($"Unknown compression {ColumnMeta[f].CompressionType}");

						}

						c += ColumnMeta[f].ArraySize;
					}

					// append relationship id
					if (RelationShipData != null)
					{
						// seen cases of missing indicies 
						if (RelationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
							data.AddRange(foreignData);
						else
							data.AddRange(new byte[4]);
					}

					CopyTable.Add(id, data.ToArray());

					if (Copies.ContainsKey(id))
					{
						foreach (int copy in Copies[id])
						{
							byte[] newrecord = CopyTable[id].ToArray();
							Buffer.BlockCopy(BitConverter.GetBytes(copy), 0, newrecord, idOffset, 4);
							CopyTable.Add(copy, newrecord);

							if (StringBlockSize > 0)
								recordOffsets.Add(recordOffsets.Last());
						}
					}
				}
			}

			if (HasIndexTable)
			{
				FieldStructure.Insert(0, new FieldStructureEntry(0, 0));
				ColumnMeta.Insert(0, new ColumnStructureEntry());
			}

			offsetmap.Clear();
			firstindex.Clear();
			OffsetDuplicates.Clear();
			Copies.Clear();
			Array.Resize(ref recordData, 0);
			bitStream.Dispose();
			ColumnMeta.ForEach(x => { x.PalletValues?.Clear(); x.SparseValues?.Clear(); });

			InternalRecordSize = (uint)CopyTable.First().Value.Length;

			if (CopyTableSize > 0)
			{
				var sort = CopyTable.Select((x, i) => new { CT = x, Off = recordOffsets[i] }).OrderBy(x => x.CT.Key);
				recordOffsets = sort.Select(x => x.Off).ToList();
				CopyTable = sort.ToDictionary(x => x.CT.Key, x => x.CT.Value);
			}

			return CopyTable;
		}

		public override byte[] ReadData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = ReadOffsetData(dbReader, pos);
			OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
			return CopyTable.Values.SelectMany(x => x).ToArray();
		}

		public override Dictionary<int, string> ReadStringTable(BinaryReader dbReader)
		{
			stringTableOffset = (int)dbReader.BaseStream.Position;
			return new StringTable().Read(dbReader, stringTableOffset, stringTableOffset + StringBlockSize, true);
		}

		public override int GetStringOffset(BinaryReader dbReader, int j, uint i)
		{
			if (HasIndexTable)
				j--;

			return dbReader.ReadInt32() + RecordDataOffset + columnOffsets[j] + recordOffsets[(int)i];
		}

		#endregion

		#region Write

		public override void WriteHeader(BinaryWriter bw, DBEntry entry)
		{
			Tuple<int, int> minmax = entry.MinMax();
			bw.BaseStream.Position = 0;

			// fix the bitlimits
			// RemoveBitLimits(); // This line kills RecordSize

			WriteBaseHeader(bw, entry);

			bw.Write((int)TableHash);
			bw.Write(LayoutHash);
			bw.Write(minmax.Item1); //MinId
			bw.Write(minmax.Item2); //MaxId
			bw.Write(Locale);
			bw.Write((ushort)Flags); //Flags
			bw.Write(IdIndex); //IdColumn
			bw.Write(TotalFieldSize);
			bw.Write(PackedDataOffset);
			bw.Write(RelationshipCount);

			bw.Write(0);  // ColumnMetadataSize
			bw.Write(0);  // SparseDataSize
			bw.Write(0);  // PalletDataSize
			bw.Write(SectionCount);
			bw.Write(Unknown1);
			bw.Write(Unknown2);

			bw.Write(0);  // RecordDataOffset					
			if (entry.Header.CopyTableSize > 0) // RecordDataRowCount
				bw.Write(entry.GetUniqueRows().Count());
			else
				bw.Write(entry.Data.Rows.Count);

			bw.Write(0);  //RecordDataStringSize
			bw.Write(0); //CopyTableSize
			bw.Write(0); //OffsetTableOffset
			bw.Write(0); //IndexSize
			bw.Write(0); //RelationshipDataSize


			// Write the field_structure bits
			for (int i = 0; i < FieldStructure.Count; i++)
			{
				if (HasIndexTable && i == 0) continue;
				if (RelationShipData != null && i == FieldStructure.Count - 1) continue;

				bw.Write(FieldStructure[i].Bits);
				bw.Write(FieldStructure[i].Offset);
			}

			bw.Write(Unknown3);

			WriteData(bw, entry);
		}

		/// <summary>
		/// WDC1 writing is entirely different so has been moved to inside the class.
		/// Will work on inheritence when WDC2 comes along - can't wait...
		/// </summary>
		/// <param name="bw"></param>
		/// <param name="entry"></param>

		public override void WriteData(BinaryWriter bw, DBEntry entry)
		{
			var offsetMap = new List<Tuple<int, short>>();
			var stringTable = new StringTable(true);
			var IsSparse = HasIndexTable && HasOffsetTable;
			var copyRecords = new Dictionary<int, IEnumerable<int>>();
			var copyIds = new HashSet<int>();

			Dictionary<Tuple<long, int>, int> stringLookup = new Dictionary<Tuple<long, int>, int>();

			long pos = bw.BaseStream.Position;

			// get a list of identical records			
			if (CopyTableSize > 0)
			{
				var copyids = Enumerable.Empty<int>();
				var copies = entry.GetCopyRows();
				foreach (var c in copies)
				{
					int id = c.First();
					copyRecords.Add(id, c.Skip(1).ToList());
					copyids = copyids.Concat(copyRecords[id]);
				}

				copyIds = new HashSet<int>(copyids);
			}

			// get relationship data
			DataColumn relationshipColumn = entry.Data.Columns.Cast<DataColumn>().FirstOrDefault(x => x.ExtendedProperties.ContainsKey("RELATIONSHIP"));
			if (relationshipColumn != null)
			{
				int index = entry.Data.Columns.IndexOf(relationshipColumn);

				Dictionary<int, uint> relationData = new Dictionary<int, uint>();
				foreach (DataRow r in entry.Data.Rows)
				{
					int id = r.Field<int>(entry.Key);
					if (!copyIds.Contains(id))
						relationData.Add(id, r.Field<uint>(index));
				}

				RelationShipData = new RelationShipData()
				{
					Records = (uint)relationData.Count,
					MinId = relationData.Values.Min(),
					MaxId = relationData.Values.Max(),
					Entries = relationData.Values.Select((x, i) => new { Index = (uint)i, Id = x }).ToDictionary(x => x.Index, x => BitConverter.GetBytes(x.Id))
				};

				relationData.Clear();
			}

			// temporarily remove the fake records
			if (HasIndexTable)
			{
				FieldStructure.RemoveAt(0);
				ColumnMeta.RemoveAt(0);
			}
			if (RelationShipData != null)
			{
				FieldStructure.RemoveAt(FieldStructure.Count - 1);
				ColumnMeta.RemoveAt(ColumnMeta.Count - 1);
			}

			// remove any existing column values
			ColumnMeta.ForEach(x => { x.PalletValues?.Clear(); x.SparseValues?.Clear(); });

			// RecordData - this can still all be done via one function, except inline strings
			BitStream bitStream = new BitStream(entry.Data.Rows.Count * ColumnMeta.Max(x => x.RecordOffset));
			for (int rowIndex = 0; rowIndex < entry.Data.Rows.Count; rowIndex++)
			{
				Queue<object> rowData = new Queue<object>(entry.Data.Rows[rowIndex].ItemArray);

				int id = entry.Data.Rows[rowIndex].Field<int>(entry.Key);
				bool isCopyRecord = copyIds.Contains(id);

				if (HasIndexTable) // dump the id from the row data
					rowData.Dequeue();

				bitStream.SeekNextOffset(); // each row starts at a 0 bit position

				long bitOffset = bitStream.Offset; // used for offset map calcs

				for (int fieldIndex = 0; fieldIndex < FieldCount; fieldIndex++)
				{
					int bitWidth = ColumnMeta[fieldIndex].BitWidth;
					int bitSize = FieldStructure[fieldIndex].BitCount;
					int arraySize = ColumnMeta[fieldIndex].ArraySize;

					// get the values for the current record, array size may require more than 1
					object[] values = ExtractFields(rowData, stringTable, bitStream, fieldIndex, out bool isString);
					byte[][] data = values.Select(x => (byte[])BitConverter.GetBytes((dynamic)x)).ToArray(); // shameful hack
					if (data.Length == 0)
						continue;

					CompressionType compression = ColumnMeta[fieldIndex].CompressionType;
					if (isCopyRecord && compression != CompressionType.Sparse) // copy records still store the sparse data
						continue;

					switch (compression)
					{
						case CompressionType.None:
							for (int i = 0; i < arraySize; i++)
							{
								if (isString)
									stringLookup.Add(new Tuple<long, int>(bitStream.Offset, bitStream.BitPosition), (int)values[i]);

								bitStream.WriteBits(data[i], bitSize);
							}
							break;

						case CompressionType.Immediate:
						case CompressionType.SignedImmediate:
							bitStream.WriteBits(data[0], bitWidth);
							break;

						case CompressionType.Sparse:
							{
								Array.Resize(ref data[0], 4);
								if (BitConverter.ToInt32(data[0], 0) != ColumnMeta[fieldIndex].BitOffset)
									ColumnMeta[fieldIndex].SparseValues.Add(id, data[0]);
							}
							break;

						case CompressionType.Pallet:
						case CompressionType.PalletArray:
							{
								byte[] combined = data.SelectMany(x => x.Concat(new byte[4]).Take(4)).ToArray(); // enforce int size rule

								int index = ColumnMeta[fieldIndex].PalletValues.FindIndex(x => x.SequenceEqual(combined));
								if (index > -1)
								{
									bitStream.WriteUInt32((uint)index, bitWidth);
								}
								else
								{
									bitStream.WriteUInt32((uint)ColumnMeta[fieldIndex].PalletValues.Count, bitWidth);
									ColumnMeta[fieldIndex].PalletValues.Add(combined);
								}
							}
							break;

						default:
							throw new Exception("Unsupported compression type " + ColumnMeta[fieldIndex].CompressionType);

					}
				}

				if (isCopyRecord)
					continue; // copy records aren't real rows so skip the padding

				bitStream.SeekNextOffset();
				short size = (short)(bitStream.Length - bitOffset);

				if (IsSparse) // matches itemsparse padding
				{
					int remaining = size % 8 == 0 ? 0 : 8 - (size % 8);
					if (remaining > 0)
					{
						size += (short)remaining;
						bitStream.WriteBytes(new byte[remaining], remaining);
					}

					offsetMap.Add(new Tuple<int, short>((int)bitOffset, size));
				}
				else // needs to be padded to the record size regardless of the byte count - weird eh?
				{
					if (size < RecordSize)
						bitStream.WriteBytes(new byte[RecordSize - size], RecordSize - size);
				}
			}

			// ColumnMeta
			pos = bw.BaseStream.Position;
			foreach (var meta in ColumnMeta)
			{
				bw.Write(meta.RecordOffset);
				bw.Write(meta.Size);

				if (meta.SparseValues != null)
					bw.Write((uint)meta.SparseValues.Count * 8); // (k<4>, v<4>)
				else if (meta.PalletValues != null)
					bw.Write((uint)meta.PalletValues.Sum(x => x.Length));
				else
					bw.WriteUInt32(0);

				bw.Write((uint)meta.CompressionType);
				bw.Write(meta.BitOffset);
				bw.Write(meta.BitWidth);
				bw.Write(meta.Cardinality);
			}
			ColumnMetadataSize = (int)(bw.BaseStream.Position - pos);

			// Pallet values
			pos = bw.BaseStream.Position;
			foreach (var meta in ColumnMeta)
			{
				if (meta.CompressionType == CompressionType.Pallet || meta.CompressionType == CompressionType.PalletArray)
					bw.WriteArray(meta.PalletValues.SelectMany(x => x).ToArray());
			}
			PalletDataSize = (int)(bw.BaseStream.Position - pos);

			// Sparse values
			pos = bw.BaseStream.Position;
			foreach (var meta in ColumnMeta)
			{
				if (meta.CompressionType == CompressionType.Sparse)
				{
					foreach (var sparse in meta.SparseValues)
					{
						bw.Write(sparse.Key);
						bw.WriteArray(sparse.Value);
					}
				}
			}
			SparseDataSize = (int)(bw.BaseStream.Position - pos);

			// set record data offset
			RecordDataOffset = (int)bw.BaseStream.Position;

			// write string offsets
			if (stringLookup.Count > 0)
			{
				foreach (var lk in stringLookup)
				{
					bitStream.Seek(lk.Key.Item1, lk.Key.Item2);
					bitStream.WriteInt32((int)(lk.Value + bitStream.Length - lk.Key.Item1 - (lk.Key.Item2 >> 3)));
				}
			}

			// push bitstream to 
			bitStream.CopyStreamTo(bw.BaseStream);
			bitStream.Dispose();

			// OffsetTable / StringTable, either or
			if (IsSparse)
			{
				// OffsetTable
				OffsetTableOffset = (int)bw.BaseStream.Position;
				WriteOffsetMap(bw, entry, offsetMap, RecordDataOffset);
				offsetMap.Clear();
			}
			else
			{
				// StringTable
				StringBlockSize = (uint)stringTable.Size;
				stringTable.CopyTo(bw.BaseStream);
				stringTable.Dispose();
			}

			// IndexTable
			if (HasIndexTable)
			{
				pos = bw.BaseStream.Position;
				WriteIndexTable(bw, entry);
				IndexSize = (int)(bw.BaseStream.Position - pos);
			}

			// Copytable
			if (CopyTableSize > 0)
			{
				pos = bw.BaseStream.Position;
				foreach (var c in copyRecords)
				{
					foreach (var v in c.Value)
					{
						bw.Write(v);
						bw.Write(c.Key);
					}
				}
				CopyTableSize = (int)(bw.BaseStream.Position - pos);
				copyRecords.Clear();
				copyIds.Clear();
			}

			// Relationships
			pos = bw.BaseStream.Position;
			if (RelationShipData != null)
			{
				bw.Write(RelationShipData.Records);
				bw.Write(RelationShipData.MinId);
				bw.Write(RelationShipData.MaxId);

				foreach (var relation in RelationShipData.Entries)
				{
					bw.Write(relation.Value);
					bw.Write(relation.Key);
				}
			}
			RelationshipDataSize = (int)(bw.BaseStream.Position - pos);

			// update header fields
			bw.BaseStream.Position = 16;
			bw.Write(StringBlockSize);

			bw.BaseStream.Position = 56;
			bw.Write(ColumnMetadataSize);
			bw.Write(SparseDataSize);
			bw.Write(PalletDataSize);

			bw.BaseStream.Position = 80;
			bw.Write(RecordDataOffset);

			bw.BaseStream.Position = 88;
			bw.Write(StringBlockSize); // record_data_stringtable
			bw.Write(CopyTableSize);
			bw.Write(OffsetTableOffset);
			bw.Write(0);// here was IndexSize, but in original file it is 0 ...
			bw.Write(RelationshipDataSize);

			// reset indextable stuff
			if (HasIndexTable)
			{
				FieldStructure.Insert(0, new FieldStructureEntry(0, 0));
				ColumnMeta.Insert(0, new ColumnStructureEntry());
			}
			if (RelationShipData != null)
			{
				FieldStructure.Add(new FieldStructureEntry(0, 0));
				ColumnMeta.Add(new ColumnStructureEntry());
			}
		}

		#endregion


		public override void Clear()
		{
			recordOffsets.Clear();
			columnOffsets.Clear();
		}
	}

	public class WDB : DBHeader
	{
		public new string Locale { get; set; }
		public int RecordVersion { get; set; }
		public int CacheVersion { get; set; }
		public int Build { get; set; }
		public override bool CheckRecordCount => false;

		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			this.Signature = signature;
			Build = dbReader.ReadInt32();

			if (Build >= 4500) // 1.6.0
				Locale = dbReader.ReadString(4).DoReverse();

			RecordSize = dbReader.ReadUInt32();
			RecordVersion = dbReader.ReadInt32();

			if (Build >= 9506) // 3.0.8
				CacheVersion = dbReader.ReadInt32();
		}

		public byte[] ReadData(BinaryReader dbReader)
		{
			List<byte> data = new List<byte>();

			//Stored as Index, Size then Data
			while (dbReader.BaseStream.Position != dbReader.BaseStream.Length)
			{
				int index = dbReader.ReadInt32();
				if (index == 0 && dbReader.BaseStream.Position == dbReader.BaseStream.Length)
					break;

				int size = dbReader.ReadInt32();
				if (index == 0 && size == 0 && dbReader.BaseStream.Position == dbReader.BaseStream.Length)
					break;

				data.AddRange(BitConverter.GetBytes(index));
				data.AddRange(dbReader.ReadBytes(size));

				RecordCount++;
			}

			return data.ToArray();
		}
	}

	public class WDB2 : DBHeader
	{
		public int Build { get; set; }
		public int TimeStamp { get; set; }
		public int[] IndexMap { get; set; } //Maps index to row for all indicies between min and max
		public short[] StringLengths { get; set; } //Length of each string including the 0 byte character

		public override bool ExtendedStringTable => Build > 18273; //WoD has two null bytes

		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			base.ReadHeader(ref dbReader, signature);

			TableHash = dbReader.ReadUInt32();
			Build = dbReader.ReadInt32();
			TimeStamp = dbReader.ReadInt32();
			MinId = dbReader.ReadInt32();
			MaxId = dbReader.ReadInt32();
			Locale = dbReader.ReadInt32();
			CopyTableSize = dbReader.ReadInt32();

			if (MaxId != 0 && Build > 12880)
			{
				int diff = MaxId - MinId + 1; //Calculate the array sizes
				IndexMap = new int[diff];
				StringLengths = new short[diff];

				//Populate the arrays
				for (int i = 0; i < diff; i++)
					IndexMap[i] = dbReader.ReadInt32();

				for (int i = 0; i < diff; i++)
					StringLengths[i] = dbReader.ReadInt16();
			}
		}

		public override void WriteHeader(BinaryWriter bw, DBEntry entry)
		{
			base.WriteHeader(bw, entry);

			Tuple<int, int> minmax = entry.MinMax();
			if (MaxId == 0) //Irrelevant if header doesn't use this
				minmax = new Tuple<int, int>(0, 0);

			bw.Write(TableHash);
			bw.Write(Build);
			bw.Write(TimeStamp);

			bw.Write(minmax.Item1);
			bw.Write(minmax.Item2);

			bw.Write(Locale);
			bw.Write(CopyTableSize);

			if (MaxId != 0 && Build > 12880)
			{
				List<int> IndiciesTable = new List<int>();
				List<short> StringLengthTable = new List<short>();

				Dictionary<int, short> stringlengths = entry.GetStringLengths();
				int x = 0;
				for (int i = minmax.Item1; i <= minmax.Item2; i++)
				{
					if (stringlengths.ContainsKey(i))
					{
						StringLengthTable.Add(stringlengths[i]);
						IndiciesTable.Add(++x);
					}
					else
					{
						IndiciesTable.Add(0);
						StringLengthTable.Add(0);
					}
				}

				//Write the data
				bw.WriteArray(IndiciesTable.ToArray());
				bw.WriteArray(StringLengthTable.ToArray());
			}
		}
	}

	public class WDB5 : DBHeader
	{
		public override bool ExtendedStringTable => true;

		public override bool HasOffsetTable => Flags.HasFlag(HeaderFlags.OffsetMap);
		public override bool HasIndexTable => Flags.HasFlag(HeaderFlags.IndexMap);
		public override bool HasRelationshipData => Flags.HasFlag(HeaderFlags.RelationshipData);

		#region Read
		public void ReadHeader(BinaryReader dbReader, string signature)
		{
			ReadHeader(ref dbReader, signature);
		}

		public void ReadBaseHeader(ref BinaryReader dbReader, string signature)
		{
			base.ReadHeader(ref dbReader, signature);
		}

		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			base.ReadHeader(ref dbReader, signature);

			TableHash = dbReader.ReadUInt32();
			LayoutHash = dbReader.ReadInt32();
			MinId = dbReader.ReadInt32();
			MaxId = dbReader.ReadInt32();
			Locale = dbReader.ReadInt32();
			CopyTableSize = dbReader.ReadInt32();
			Flags = (HeaderFlags)dbReader.ReadUInt16();
			IdIndex = dbReader.ReadUInt16();

			if (Flags.HasFlag(HeaderFlags.IndexMap))
				IdIndex = 0; //Ignored if Index Table

			//Gather field structures
			FieldStructure = new List<FieldStructureEntry>();
			for (int i = 0; i < FieldCount; i++)
			{
				var field = new FieldStructureEntry(dbReader.ReadInt16(), (ushort)(dbReader.ReadUInt16() + (HasIndexTable ? 4 : 0)));
				FieldStructure.Add(field);

				if (i > 0)
					FieldStructure[i - 1].SetLength(field);
			}

			if (HasIndexTable)
			{
				FieldCount++;
				FieldStructure.Insert(0, new FieldStructureEntry(0, 0));

				if (FieldCount > 1)
					FieldStructure[1].SetLength(FieldStructure[0]);
			}
		}

		public Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = new Dictionary<int, byte[]>();
			List<Tuple<int, short>> offsetmap = new List<Tuple<int, short>>();
			Dictionary<int, OffsetDuplicate> firstindex = new Dictionary<int, OffsetDuplicate>();

			long copyTablePos = dbReader.BaseStream.Length - CopyTableSize;
			long indexTablePos = copyTablePos - (HasIndexTable ? RecordCount * 4 : 0);
			int[] m_indexes = null;

			//Offset Map
			if (HasOffsetTable)
			{
				// Records table
				dbReader.Scrub(StringBlockSize);

				for (int i = 0; i < (MaxId - MinId + 1); i++)
				{
					int offset = dbReader.ReadInt32();
					short length = dbReader.ReadInt16();

					if (offset == 0 || length == 0) continue;

					//Special case, may contain duplicates in the offset map that we don't want
					if (CopyTableSize == 0)
					{
						if (!firstindex.ContainsKey(offset))
							firstindex.Add(offset, new OffsetDuplicate(offsetmap.Count, firstindex.Count));
						else
							OffsetDuplicates.Add(MinId + i, firstindex[offset].VisibleIndex);
					}

					offsetmap.Add(new Tuple<int, short>(offset, length));
				}
			}

			if (HasRelationshipData)
				dbReader.BaseStream.Position += (MaxId - MinId + 1) * 4;

			//Index table
			if (HasIndexTable)
			{
				//Offset map alone reads straight into this others may not
				if (!HasOffsetTable || HasRelationshipData)
					dbReader.Scrub(indexTablePos);

				m_indexes = new int[RecordCount];
				for (int i = 0; i < RecordCount; i++)
					m_indexes[i] = dbReader.ReadInt32();
			}

			//Extract record data
			for (int i = 0; i < Math.Max(RecordCount, offsetmap.Count); i++)
			{
				if (HasOffsetTable)
				{
					int id = m_indexes[CopyTable.Count];
					var map = offsetmap[i];

					if (CopyTableSize == 0 && firstindex[map.Item1].HiddenIndex != i) //Ignore duplicates
						continue;

					dbReader.Scrub(map.Item1);

					IEnumerable<byte> recordbytes = BitConverter.GetBytes(id).Concat(dbReader.ReadBytes(map.Item2));
					CopyTable.Add(id, recordbytes.ToArray());
				}
				else
				{
					dbReader.Scrub(pos + i * RecordSize);
					byte[] recordbytes = dbReader.ReadBytes((int)RecordSize);

					if (HasIndexTable)
					{
						IEnumerable<byte> newrecordbytes = BitConverter.GetBytes(m_indexes[i]).Concat(recordbytes);
						CopyTable.Add(m_indexes[i], newrecordbytes.ToArray());
					}
					else
					{
						int bytecount = FieldStructure[IdIndex].ByteCount;
						int offset = FieldStructure[IdIndex].Offset;

						int id = 0;
						for (int j = 0; j < bytecount; j++)
							id |= (recordbytes[offset + j] << (j * 8));

						CopyTable.Add(id, recordbytes);
					}
				}
			}

			//CopyTable
			if (CopyTableSize != 0 && copyTablePos != dbReader.BaseStream.Length)
			{
				dbReader.Scrub(copyTablePos);
				while (dbReader.BaseStream.Position != dbReader.BaseStream.Length)
				{
					int id = dbReader.ReadInt32();
					int idcopy = dbReader.ReadInt32();

					byte[] copyRow = CopyTable[idcopy];
					byte[] newRow = new byte[copyRow.Length];
					Array.Copy(copyRow, newRow, newRow.Length);
					Array.Copy(BitConverter.GetBytes(id), newRow, sizeof(int));

					CopyTable.Add(id, newRow);
				}
			}

			return CopyTable;
		}

		public override byte[] ReadData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = ReadOffsetData(dbReader, pos);
			OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
			return CopyTable.Values.SelectMany(x => x).ToArray();
		}

		internal struct OffsetDuplicate
		{
			public int HiddenIndex { get; set; }
			public int VisibleIndex { get; set; }

			public OffsetDuplicate(int hidden, int visible)
			{
				this.HiddenIndex = hidden;
				this.VisibleIndex = visible;
			}
		}
		#endregion

		#region Write
		public virtual void WriteBaseHeader(BinaryWriter bw, DBEntry entry)
		{
			base.WriteHeader(bw, entry);
		}

		public override void WriteHeader(BinaryWriter bw, DBEntry entry)
		{
			Tuple<int, int> minmax = entry.MinMax();
			bw.BaseStream.Position = 0;

			base.WriteHeader(bw, entry);

			bw.Write((int)TableHash);
			bw.Write(LayoutHash);
			bw.Write(minmax.Item1); //MinId
			bw.Write(minmax.Item2); //MaxId
			bw.Write(Locale);
			bw.Write(0); //CopyTableSize
			bw.Write((ushort)Flags); //Flags
			bw.Write(IdIndex); //IdColumn

			//Write the field_structure bits
			for (int i = 0; i < FieldStructure.Count; i++)
			{
				if (HasIndexTable && i == 0) continue;

				bw.Write(FieldStructure[i].Bits);
				bw.Write(HasIndexTable ? (ushort)(FieldStructure[i].Offset - 4) : FieldStructure[i].Offset);
			}
		}

		public override void WriteOffsetMap(BinaryWriter bw, DBEntry entry, List<Tuple<int, short>> OffsetMap, int record_offset = 0)
		{
			var minmax = entry.MinMax();
			var ids = new HashSet<int>(entry.GetPrimaryKeys());
			var duplicates = entry.Header.OffsetDuplicates;

			int m = 0;
			for (int x = minmax.Item1; x <= minmax.Item2; x++)
			{
				if (ids.Contains(x)) //Insert the offset map
				{
					var kvp = OffsetMap[m];
					bw.Write(kvp.Item1 + record_offset);
					bw.Write(kvp.Item2);
					m++;
				}
				else if (duplicates.ContainsKey(x)) //Reinsert our duplicates
				{
					var hiddenkvp = OffsetMap[duplicates[x]];
					bw.Write(hiddenkvp.Item1 + record_offset);
					bw.Write(hiddenkvp.Item2);
				}
				else
				{
					bw.BaseStream.Position += sizeof(int) + sizeof(short); //0 fill
				}
			}

			ids.Clear();
		}

		public override void WriteIndexTable(BinaryWriter bw, DBEntry entry)
		{
			int m = 0;
			int[] ids;
			int index = entry.Data.Columns.IndexOf(entry.Key);

			if (!HasOffsetTable && entry.Header.CopyTableSize > 0)
				ids = entry.GetUniqueRows().Select(x => x.Field<int>(index)).ToArray();
			else
				ids = entry.GetPrimaryKeys().ToArray();

			if (entry.Header.HasRelationshipData)
			{
				//TODO figure out if it is always the 2nd column
				ushort[] secondids = entry.Data.Rows.Cast<DataRow>().Select(x => x.Field<ushort>(2)).ToArray();

				//Write all of the secondary ids
				foreach (ushort id in secondids)
				{
					//Populate missing secondary ids with 0
					if (m > 0 && (ids[m] - ids[m - 1]) > 1)
						bw.BaseStream.Position += sizeof(int) * (ids[m] - ids[m - 1] - 1);

					bw.Write((int)id);
					m++;
				}
			}

			//Write all the IDs
			bw.WriteArray(ids);
		}

		public virtual void WriteCopyTable(BinaryWriter bw, DBEntry entry)
		{
			if (HasOffsetTable || CopyTableSize == 0)
				return;

			int index = entry.Data.Columns.IndexOf(entry.Key);
			var copyRows = entry.GetCopyRows();
			if (copyRows.Count() > 0)
			{
				int size = 0;
				foreach (var copies in copyRows)
				{
					int keyindex = copies.First();
					foreach (var c in copies.Skip(1))
					{
						bw.Write(c);
						bw.Write(keyindex);
						size += sizeof(int) + sizeof(int);
					}
				}

				//Set CopyTableSize
				long pos = bw.BaseStream.Position;
				bw.Scrub(0x28);
				bw.Write(size);
				bw.Scrub(pos);
			}
		}

		public override void WriteRecordPadding(BinaryWriter bw, DBEntry entry, long offset)
		{
			if (IsTypeOf<WDB6>() && HasOffsetTable && HasIndexTable)
				bw.BaseStream.Position += 2;
			else if (!IsTypeOf<WDB6>() && HasOffsetTable)
				bw.BaseStream.Position += 2; //Offset map always has 2 bytes padding
			else
				base.WriteRecordPadding(bw, entry, offset); //Scrub to the end of the record if necessary
		}
		#endregion
	}

	public class WDB6 : WDB5
	{
		public static readonly short[] CommonDataBits = new short[] { 0, 16, 24, 0, 0 };

		public static readonly Dictionary<TypeCode, byte> CommonDataTypes = new Dictionary<TypeCode, byte>()
		{
			{ TypeCode.String, 0 },
			{ TypeCode.Int16, 1 },
			{ TypeCode.UInt16, 1 },
			{ TypeCode.Byte, 2 },
			{ TypeCode.SByte, 2 },
			{ TypeCode.Single, 3 },
			{ TypeCode.Int32, 4 },
			{ TypeCode.UInt32, 4 },
		};

		#region Read
		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			ReadBaseHeader(ref dbReader, signature);

			TableHash = dbReader.ReadUInt32();
			LayoutHash = dbReader.ReadInt32();
			MinId = dbReader.ReadInt32();
			MaxId = dbReader.ReadInt32();
			Locale = dbReader.ReadInt32();
			CopyTableSize = dbReader.ReadInt32();
			Flags = (HeaderFlags)dbReader.ReadUInt16();
			IdIndex = dbReader.ReadUInt16();
			TotalFieldSize = dbReader.ReadUInt32();
			CommonDataTableSize = dbReader.ReadUInt32();

			if (HasIndexTable)
				IdIndex = 0; //Ignored if Index Table    

			InternalRecordSize = RecordSize; //RecordSize header field is not right anymore

			//Gather field structures
			FieldStructure = new List<FieldStructureEntry>();
			for (int i = 0; i < FieldCount; i++)
			{
				var field = new FieldStructureEntry(dbReader.ReadInt16(), (ushort)(dbReader.ReadUInt16() + (HasIndexTable ? 4 : 0)));
				FieldStructure.Add(field);

				if (i > 0)
					FieldStructure[i - 1].SetLength(field);
			}

			if (HasIndexTable)
			{
				InternalRecordSize += 4;
				FieldCount++;
				FieldStructure.Insert(0, new FieldStructureEntry(0, 0));

				if (FieldCount > 1)
					FieldStructure[1].SetLength(FieldStructure[0]);
			}
		}

		public new Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = new Dictionary<int, byte[]>();
			List<Tuple<int, short>> offsetmap = new List<Tuple<int, short>>();
			Dictionary<int, OffsetDuplicate> firstindex = new Dictionary<int, OffsetDuplicate>();

			long commonDataTablePos = dbReader.BaseStream.Length - CommonDataTableSize;
			long copyTablePos = commonDataTablePos - CopyTableSize;
			long indexTablePos = copyTablePos - (HasIndexTable ? RecordCount * 4 : 0);
			int[] m_indexes = null;

			//Offset Map
			if (HasOffsetTable)
			{
				// Records table
				dbReader.Scrub(StringBlockSize);

				for (int i = 0; i < (MaxId - MinId + 1); i++)
				{
					int offset = dbReader.ReadInt32();
					short length = dbReader.ReadInt16();

					if (offset == 0 || length == 0) continue;

					//Special case, may contain duplicates in the offset map that we don't want
					if (CopyTableSize == 0)
					{
						if (!firstindex.ContainsKey(offset))
							firstindex.Add(offset, new OffsetDuplicate(offsetmap.Count, firstindex.Count));
						else
							OffsetDuplicates.Add(MinId + i, firstindex[offset].VisibleIndex);
					}

					offsetmap.Add(new Tuple<int, short>(offset, length));
				}
			}

			if (HasRelationshipData)
				dbReader.BaseStream.Position += (MaxId - MinId + 1) * 4;

			//Index table
			if (HasIndexTable)
			{
				//Offset map alone reads straight into this others may not
				if (!HasOffsetTable || HasRelationshipData)
					dbReader.Scrub(indexTablePos);

				m_indexes = new int[RecordCount];
				for (int i = 0; i < RecordCount; i++)
					m_indexes[i] = dbReader.ReadInt32();
			}

			//Extract record data
			for (int i = 0; i < Math.Max(RecordCount, offsetmap.Count); i++)
			{
				if (HasOffsetTable && m_indexes != null)
				{
					int id = m_indexes[Math.Min(CopyTable.Count, m_indexes.Length - 1)];
					var map = offsetmap[i];

					if (CopyTableSize == 0 && firstindex[map.Item1].HiddenIndex != i) //Ignore duplicates
						continue;

					dbReader.Scrub(map.Item1);

					IEnumerable<byte> recordbytes = BitConverter.GetBytes(id)
													.Concat(dbReader.ReadBytes(map.Item2));

					CopyTable.Add(id, recordbytes.ToArray());
				}
				else
				{
					dbReader.Scrub(pos + i * RecordSize);
					byte[] recordbytes = dbReader.ReadBytes((int)RecordSize).ToArray();

					if (HasIndexTable)
					{
						IEnumerable<byte> newrecordbytes = BitConverter.GetBytes(m_indexes[i]).Concat(recordbytes);
						CopyTable.Add(m_indexes[i], newrecordbytes.ToArray());
					}
					else
					{
						int bytecount = FieldStructure[IdIndex].ByteCount;
						int offset = FieldStructure[IdIndex].Offset;

						int id = 0;
						for (int j = 0; j < bytecount; j++)
							id |= (recordbytes[offset + j] << (j * 8));

						CopyTable.Add(id, recordbytes);
					}
				}
			}

			//CopyTable
			if (CopyTableSize != 0 && copyTablePos != dbReader.BaseStream.Length)
			{
				dbReader.Scrub(copyTablePos);
				while (dbReader.BaseStream.Position != dbReader.BaseStream.Length)
				{
					int id = dbReader.ReadInt32();
					int idcopy = dbReader.ReadInt32();

					byte[] copyRow = CopyTable[idcopy];
					byte[] newRow = new byte[copyRow.Length];
					Array.Copy(copyRow, newRow, newRow.Length);
					Array.Copy(BitConverter.GetBytes(id), newRow, sizeof(int));

					CopyTable.Add(id, newRow);
				}
			}

			//CommonDataTable
			if (CommonDataTableSize > 0)
			{
				dbReader.Scrub(commonDataTablePos);
				int columncount = dbReader.ReadInt32();

				var commondatalookup = new Dictionary<int, byte[]>[columncount];

				//Initial Data extraction
				for (int i = 0; i < columncount; i++)
				{
					int count = dbReader.ReadInt32();
					byte type = dbReader.ReadByte();
					short bit = CommonDataBits[type];
					int size = (32 - bit) >> 3;

					commondatalookup[i] = new Dictionary<int, byte[]>();

					//New field not defined in header
					if (i > FieldStructure.Count - 1)
					{
						var offset = (ushort)((FieldStructure.Count == 0 ? 0 : FieldStructure[i - 1].Offset + FieldStructure[i - 1].ByteCount));
						FieldStructure.Add(new FieldStructureEntry(bit, offset, type));

						if (FieldStructure.Count > 1)
							FieldStructure[i - 1].SetLength(FieldStructure[i]);
					}

					for (int x = 0; x < count; x++)
					{
						commondatalookup[i].Add(dbReader.ReadInt32(), dbReader.ReadBytes(size));

						if (TableStructure == null)
							dbReader.ReadBytes(4 - size);
					}
				}

				var ids = CopyTable.Keys.ToArray();
				foreach (var id in ids)
				{
					for (int i = 0; i < commondatalookup.Length; i++)
					{
						if (!FieldStructure[i].CommonDataColumn)
							continue;

						var col = commondatalookup[i];
						var defaultValue = TableStructure?.Fields?[i]?.DefaultValue;
						defaultValue = string.IsNullOrEmpty(defaultValue) ? "0" : defaultValue;

						var field = FieldStructure[i];
						var zeroData = new byte[field.ByteCount];
						if (defaultValue != "0")
						{
							switch (field.CommonDataType)
							{
								case 1:
									zeroData = BitConverter.GetBytes(ushort.Parse(defaultValue));
									break;
								case 2:
									zeroData = new[] { byte.Parse(defaultValue) };
									break;
								case 3:
									zeroData = BitConverter.GetBytes(float.Parse(defaultValue));
									break;
								case 4:
									zeroData = BitConverter.GetBytes(int.Parse(defaultValue));
									break;
							}
						}

						byte[] currentData = CopyTable[id];
						byte[] data = col.ContainsKey(id) ? col[id] : zeroData;
						Array.Resize(ref currentData, currentData.Length + data.Length);
						Array.Copy(data, 0, currentData, field.Offset, data.Length);
						CopyTable[id] = currentData;
					}
				}

				commondatalookup = null;
				InternalRecordSize = (uint)CopyTable.Values.First().Length;
			}

			return CopyTable;
		}

		public override byte[] ReadData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = ReadOffsetData(dbReader, pos);
			OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
			return CopyTable.Values.SelectMany(x => x).ToArray();
		}
		#endregion

		#region Write
		public override void WriteHeader(BinaryWriter bw, DBEntry entry)
		{
			Tuple<int, int> minmax = entry.MinMax();
			bw.BaseStream.Position = 0;

			WriteBaseHeader(bw, entry);

			bw.Write((int)TableHash);
			bw.Write(LayoutHash);
			bw.Write(minmax.Item1); //MinId
			bw.Write(minmax.Item2); //MaxId
			bw.Write(Locale);
			bw.Write(0); //CopyTableSize
			bw.Write((ushort)Flags); //Flags
			bw.Write(IdIndex); //IdColumn
			bw.Write(TotalFieldSize);
			bw.Write(0); //CommonDataTableSize

			//Write the field_structure bits
			for (int i = 0; i < FieldStructure.Count; i++)
			{
				if (HasIndexTable && i == 0) continue;
				if (FieldStructure[i].CommonDataColumn) continue;

				bw.Write(FieldStructure[i].Bits);
				bw.Write(HasIndexTable ? (ushort)(FieldStructure[i].Offset - 4) : FieldStructure[i].Offset);
			}
		}

		public virtual void WriteCommonDataTable(BinaryWriter bw, DBEntry entry)
		{
			if (CommonDataTableSize == 0)
				return;

			long start = bw.BaseStream.Position; //Current position
			var rows = entry.Data.Rows.Cast<DataRow>();

			bw.WriteUInt32((uint)FieldStructure.Count); //Field count

			for (int i = 0; i < FieldStructure.Count; i++)
			{
				var field = TableStructure.Fields[i].InternalName;
				var defaultValue = TableStructure.Fields[i].DefaultValue;
				var typeCode = Type.GetTypeCode(entry.Data.Columns[field].DataType);
				var pk = entry.Data.PrimaryKey[0];

				var numberDefault = string.IsNullOrEmpty(defaultValue) ? "0" : defaultValue;
				Dictionary<int, byte[]> data = new Dictionary<int, byte[]>();
				int padding = 0;

				//Only get data if CommonDataTable
				if (FieldStructure[i].CommonDataColumn)
				{
					switch (typeCode)
					{
						case TypeCode.String:
							data = rows.Where(x => (string)x[field] != defaultValue).ToDictionary(x => (int)x[pk], y => Encoding.UTF8.GetBytes((string)y[field]));
							break;
						case TypeCode.UInt16:
							data = rows.Where(x => (ushort)x[field] != ushort.Parse(numberDefault)).ToDictionary(x => (int)x[pk], y => BitConverter.GetBytes((ushort)y[field]));
							padding = 2;
							break;
						case TypeCode.Int16:
							data = rows.Where(x => (short)x[field] != short.Parse(numberDefault)).ToDictionary(x => (int)x[pk], y => BitConverter.GetBytes((short)y[field]));
							padding = 2;
							break;
						case TypeCode.Single:
							data = rows.Where(x => (float)x[field] != float.Parse(numberDefault)).ToDictionary(x => (int)x[pk], y => BitConverter.GetBytes((float)y[field]));
							break;
						case TypeCode.Int32:
							data = rows.Where(x => (int)x[field] != int.Parse(numberDefault)).ToDictionary(x => (int)x[pk], y => BitConverter.GetBytes((int)y[field]));
							break;
						case TypeCode.UInt32:
							data = rows.Where(x => (uint)x[field] != uint.Parse(numberDefault)).ToDictionary(x => (int)x[pk], y => BitConverter.GetBytes((uint)y[field]));
							break;
						case TypeCode.Byte:
							data = rows.Where(x => (byte)x[field] != byte.Parse(numberDefault)).ToDictionary(x => (int)x[pk], y => new byte[] { (byte)y[field] });
							padding = 3;
							break;
						default:
							continue;
					}
				}

				bw.WriteInt32(data.Count); //Count
				bw.Write(CommonDataTypes[typeCode]); //Type code
				foreach (var d in data)
				{
					bw.WriteInt32(d.Key); //Id
					bw.Write(d.Value); //Value

					if (TableStructure == null && padding > 0)
						bw.BaseStream.Position += padding;
				}
			}

			//Set CommonDataTableSize
			long pos = bw.BaseStream.Position;
			bw.Scrub(0x34);
			bw.WriteInt32((int)(pos - start));
			bw.Scrub(pos);
		}
		#endregion
	}

	public class WDBC : DBHeader
	{
		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			base.ReadHeader(ref dbReader, signature);
		}
	}

	public class WCH5 : DBHeader
	{
		public uint Build { get; set; }
		public uint TimeStamp { get; set; }
		public override bool ExtendedStringTable => true;

		public string FileName { get; set; }
		public override bool HasOffsetTable => Flags.HasFlag(HeaderFlags.OffsetMap);
		public override bool HasIndexTable => Flags.HasFlag(HeaderFlags.IndexMap);
		public override bool HasRelationshipData => Flags.HasFlag(HeaderFlags.RelationshipData);

		protected WDB5 WDB5CounterPart;
		protected int OffsetMapOffset = 0x30;

		public WCH5()
		{
			HeaderSize = 0x30;
		}

		public WCH5(string filename)
		{
			HeaderSize = 0x30;
			this.FileName = filename;
		}

		#region Read
		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			string _filename = Path.GetFileNameWithoutExtension(FileName).ToLower();
			WDB5CounterPart = Database.Entries
							.FirstOrDefault(x => x.Header.IsTypeOf<WDB5>() && Path.GetFileNameWithoutExtension(x.FileName).ToLower() == _filename)?
							.Header as WDB5;

			if (WDB5CounterPart == null)
				throw new Exception("You must have the DB2 counterpart open first to be able to read this file.");

			Flags = WDB5CounterPart.Flags;
			IdIndex = WDB5CounterPart.IdIndex;
			FieldStructure = WDB5CounterPart.FieldStructure;

			if (HasOffsetTable)
				Flags = HeaderFlags.OffsetMap;

			base.ReadHeader(ref dbReader, signature);
			TableHash = dbReader.ReadUInt32();
			LayoutHash = dbReader.ReadInt32();
			Build = dbReader.ReadUInt32();
			TimeStamp = dbReader.ReadUInt32();
			MinId = dbReader.ReadInt32();
			MaxId = dbReader.ReadInt32();
			Locale = dbReader.ReadInt32();
		}

		public Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = new Dictionary<int, byte[]>();
			List<OffsetEntry> offsetmap = new List<OffsetEntry>();

			long indexTablePos = dbReader.BaseStream.Length - (HasIndexTable ? RecordCount * 4 : 0);
			int[] m_indexes = null;

			//Offset Map - Contains the index, offset and length so the index table is not used
			if (HasOffsetTable)
			{
				// Records table
				if (StringBlockSize > 0)
					dbReader.Scrub(StringBlockSize);

				for (int i = 0; i < RecordCount; i++)
				{
					int id = dbReader.ReadInt32();
					int offset = dbReader.ReadInt32();
					short length = dbReader.ReadInt16();

					if (offset == 0 || length == 0) continue;

					offsetmap.Add(new OffsetEntry(id, offset, length));
				}
			}

			//Index table
			if (HasIndexTable)
			{
				if (!HasOffsetTable || HasRelationshipData)
					dbReader.Scrub(indexTablePos);

				m_indexes = new int[RecordCount];
				for (int i = 0; i < RecordCount; i++)
					m_indexes[i] = dbReader.ReadInt32();
			}

			//Extract record data
			for (int i = 0; i < Math.Max(RecordCount, offsetmap.Count); i++)
			{
				if (HasOffsetTable)
				{
					var map = offsetmap[i];
					dbReader.Scrub(map.Offset);

					IEnumerable<byte> recordbytes = BitConverter.GetBytes(map.Id).Concat(dbReader.ReadBytes(map.Length));
					CopyTable.Add(map.Id, recordbytes.ToArray());
				}
				else
				{
					dbReader.Scrub(pos + i * RecordSize);
					byte[] recordbytes = dbReader.ReadBytes((int)RecordSize);

					if (HasIndexTable)
					{
						IEnumerable<byte> newrecordbytes = BitConverter.GetBytes(m_indexes[i]).Concat(recordbytes);
						CopyTable.Add(m_indexes[i], newrecordbytes.ToArray());
					}
					else
					{
						int bytecount = FieldStructure[IdIndex].ByteCount;
						int offset = FieldStructure[IdIndex].Offset;

						int id = 0;
						for (int j = 0; j < bytecount; j++)
							id |= (recordbytes[offset + j] << (j * 8));

						CopyTable.Add(id, recordbytes);
					}
				}
			}

			return CopyTable;
		}

		public override byte[] ReadData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = ReadOffsetData(dbReader, pos);
			OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
			return CopyTable.Values.SelectMany(x => x).ToArray();
		}

		internal struct OffsetEntry
		{
			public int Id { get; set; }
			public int Offset { get; set; }
			public short Length { get; set; }

			public OffsetEntry(int id, int offset, short length)
			{
				this.Id = id;
				this.Offset = offset;
				this.Length = length;
			}
		}
		#endregion

		#region Write
		public override void WriteHeader(BinaryWriter bw, DBEntry entry)
		{
			Tuple<int, int> minmax = entry.MinMax();
			bw.BaseStream.Position = 0;

			base.WriteHeader(bw, entry);

			bw.Write(TableHash);
			bw.Write(LayoutHash);
			bw.Write(Build);
			bw.Write(TimeStamp);
			bw.Write(minmax.Item1); //MinId
			bw.Write(minmax.Item2); //MaxId
			bw.Write(Locale);

			//WCH5 has the offsetmap BEFORE the data, create placeholder data
			if (HasOffsetTable)
			{
				OffsetMapOffset = (int)bw.BaseStream.Position;
				bw.BaseStream.Position += entry.GetPrimaryKeys().Count() * (sizeof(int) + sizeof(int) + sizeof(short));
			}

		}

		public override void WriteOffsetMap(BinaryWriter bw, DBEntry entry, List<Tuple<int, short>> OffsetMap, int record_offset = 0)
		{
			bw.Scrub(OffsetMapOffset); //Scrub to after header

			//Write the offset map
			var ids = entry.GetPrimaryKeys().ToList();
			for (int x = 0; x < ids.Count; x++)
			{
				var kvp = OffsetMap[x];
				bw.Write(ids[x]);
				bw.Write(kvp.Item1);
				bw.Write(kvp.Item2);
			}
			ids.Clear();

			//Clear string table size
			long pos = bw.BaseStream.Position;
			bw.Scrub(entry.Header.StringTableOffset);
			bw.Write(0);
			bw.Scrub(pos);
		}

		public override void WriteIndexTable(BinaryWriter bw, DBEntry entry)
		{
			int m = 0;
			int[] ids = entry.GetPrimaryKeys().ToArray();

			if (entry.Header.HasRelationshipData)
			{
				ushort[] secondids = entry.Data.Rows.Cast<DataRow>().Select(x => x.Field<ushort>(2)).ToArray();

				//Write all of the secondary ids
				foreach (ushort id in secondids)
				{
					//Populate missing secondary ids with 0
					if (m > 0 && (ids[m] - ids[m - 1]) > 1)
						bw.BaseStream.Position += sizeof(int) * (ids[m] - ids[m - 1] - 1);

					bw.Write((int)id);
					m++;
				}
			}

			//Write all the primary IDs
			bw.WriteArray(ids);
		}

		public override void WriteRecordPadding(BinaryWriter bw, DBEntry entry, long offset) { }
		#endregion
	}

	class WCH7 : WCH5
	{
		public int[] WCH7Table { get; private set; } = new int[0];

		public WCH7()
		{
			StringTableOffset = 0x14;
			HeaderSize = 0x34;
		}

		public WCH7(string filename)
		{
			StringTableOffset = 0x14;
			HeaderSize = 0x34;
			this.FileName = filename;
		}

		public override byte[] ReadData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = ReadOffsetData(dbReader, pos);
			OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
			return CopyTable.Values.SelectMany(x => x).ToArray();
		}

		public new Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = new Dictionary<int, byte[]>();
			List<OffsetEntry> offsetmap = new List<OffsetEntry>();

			long indexTablePos = dbReader.BaseStream.Length - (HasIndexTable ? RecordCount * 4 : 0);
			long wch7TablePos = indexTablePos - (UnknownWCH7 * 4);
			int[] m_indexes = null;


			//Offset table - Contains the index, offset and length meaning the index table is not used
			if (HasOffsetTable)
			{
				// Records table
				if (StringBlockSize > 0)
					dbReader.Scrub(StringBlockSize);

				for (int i = 0; i < RecordCount; i++)
				{
					int id = dbReader.ReadInt32();
					int offset = dbReader.ReadInt32();
					short length = dbReader.ReadInt16();

					if (offset == 0 || length == 0) continue;

					offsetmap.Add(new OffsetEntry(id, offset, length));
				}
			}

			//New WCH7 table
			if (UnknownWCH7 > 0)
			{
				WCH7Table = new int[UnknownWCH7];
				dbReader.Scrub(wch7TablePos);

				for (int i = 0; i < UnknownWCH7; i++)
					WCH7Table[i] = dbReader.ReadInt32();
			}

			//Index table
			if (HasIndexTable)
			{
				if (!HasOffsetTable || HasRelationshipData)
					dbReader.Scrub(indexTablePos);

				m_indexes = new int[RecordCount];
				for (int i = 0; i < RecordCount; i++)
					m_indexes[i] = dbReader.ReadInt32();
			}

			//Extract record data
			for (int i = 0; i < Math.Max(RecordCount, offsetmap.Count); i++)
			{
				if (HasOffsetTable)
				{
					var map = offsetmap[i];
					dbReader.Scrub(map.Offset);

					IEnumerable<byte> recordbytes = BitConverter.GetBytes(map.Id).Concat(dbReader.ReadBytes(map.Length));
					CopyTable.Add(map.Id, recordbytes.ToArray());
				}
				else
				{
					dbReader.Scrub(pos + i * RecordSize);
					byte[] recordbytes = dbReader.ReadBytes((int)RecordSize);

					if (HasIndexTable)
					{
						IEnumerable<byte> newrecordbytes = BitConverter.GetBytes(m_indexes[i]).Concat(recordbytes);
						CopyTable.Add(m_indexes[i], newrecordbytes.ToArray());
					}
					else
					{
						int bytecount = FieldStructure[IdIndex].ByteCount;
						int offset = FieldStructure[IdIndex].Offset;

						int id = 0;
						for (int j = 0; j < bytecount; j++)
							id |= (recordbytes[offset + j] << (j * 8));

						CopyTable.Add(id, recordbytes);
					}
				}
			}

			return CopyTable;
		}

		public override void WriteRecordPadding(BinaryWriter bw, DBEntry entry, long offset)
		{
			if (bw.BaseStream.Position - offset < RecordSize)
				bw.BaseStream.Position += (RecordSize - (bw.BaseStream.Position - offset));
		}
	}

	class WCH8 : WCH7
	{
		public WCH8()
		{
			StringTableOffset = 0x14;
			HeaderSize = 0x34;
		}

		public WCH8(string filename)
		{
			StringTableOffset = 0x14;
			HeaderSize = 0x34;
			this.FileName = filename;
		}

		public override void WriteRecordPadding(BinaryWriter bw, DBEntry entry, long offset)
		{
			if (!HasOffsetTable && bw.BaseStream.Position - offset < RecordSize)
				bw.BaseStream.Position += (RecordSize - (bw.BaseStream.Position - offset));
		}
	}

	public class HTFX : DBHeader
	{
		public int Build { get; set; }
		public byte[] Hashes { get; set; }
		public List<HotfixEntry> Entries { get; private set; } = new List<HotfixEntry>();

		public override bool CheckRecordCount => false;
		public override bool CheckRecordSize => false;
		public override bool CheckTableStructure => false;

		public WDB6 WDB6CounterPart { get; private set; }

		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			this.Signature = signature;
			Locale = dbReader.ReadInt32();
			Build = dbReader.ReadInt32();

			string tempHeader = dbReader.ReadString(4);
			dbReader.BaseStream.Position -= 4;

			if (tempHeader != "XFTH")
				Hashes = dbReader.ReadBytes(32);

			while (dbReader.BaseStream.Position < dbReader.BaseStream.Length)
				Entries.Add(new HotfixEntry(dbReader));

			Entries.RemoveAll(x => x.IsValid != 1); //Remove old hotfix entries
		}

		public bool HasEntry(DBHeader counterpart) => Entries.Any(x => (x.Locale == counterpart.Locale || x.Locale == 0) && x.TableHash == counterpart.TableHash && x.IsValid == 1);

		public bool Read(DBHeader counterpart, DBEntry dbentry)
		{
			WDB6CounterPart = counterpart as WDB6;
			if (WDB6CounterPart == null)
				return false;

			var entries = Entries.Where(x => (x.Locale == counterpart.Locale || x.Locale == 0) && x.TableHash == counterpart.TableHash);
			if (entries.Any())
			{
				OffsetLengths = entries.Select(x => (int)x.Size + 4).ToArray();
				TableStructure = WDB6CounterPart.TableStructure;
				Flags = WDB6CounterPart.Flags;
				FieldStructure = WDB6CounterPart.FieldStructure;
				RecordCount = (uint)entries.Count();

				dbentry.LoadTableStructure();

				IEnumerable<byte> Data = new byte[0];
				foreach (var e in entries)
					Data = Data.Concat(BitConverter.GetBytes(e.RowId)).Concat(e.Data);

				using (MemoryStream ms = new MemoryStream(Data.ToArray()))
				using (BinaryReader br = new BinaryReader(ms))
					new DBReader().ReadIntoTable(ref dbentry, br, new Dictionary<int, string>());

				return true;
			}

			return false;
		}
	}

	public class HotfixEntry
	{
		public uint Signature;
		public uint Locale;
		public uint PushId;
		public uint Size;
		public uint TableHash;
		public int RowId;
		public byte IsValid;
		public byte[] Padding;
		public byte[] Data;

		public HotfixEntry(BinaryReader br)
		{
			Signature = br.ReadUInt32();
			Locale = br.ReadUInt32();
			PushId = br.ReadUInt32();
			Size = br.ReadUInt32();
			TableHash = br.ReadUInt32();
			RowId = br.ReadInt32();
			IsValid = br.ReadByte();
			Padding = br.ReadBytes(3);

			Data = br.ReadBytes((int)Size);
		}
	}


	public class MinMax
	{
		public object MinVal;
		public object MaxVal;
		public bool Signed;
		public bool IsSingle;
	}
}
