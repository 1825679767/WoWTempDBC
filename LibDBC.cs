using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Collections;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace WoWTempDBC
{
    class LibDBC
    {
        public static DataTable GetData(string DBCFilePath)
        {
            DataTable DtTable = new DataTable();
            if (!File.Exists(DBCFilePath))
                throw new FileNotFoundException("文件不存在 " + DBCFilePath);

            FileStream FStream = new FileStream(DBCFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader BR = new BinaryReader(FStream);
            if (BR.ReadUInt32() != 0x43424457)
                throw new Exception("该文件不是有效的DBC文件");

            int RowCount, ColCount, RowLen, TextLen;
            RowCount = BR.ReadInt32();
            ColCount = BR.ReadInt32();
            RowLen = BR.ReadInt32();
            TextLen = BR.ReadInt32();
            for (int i = 0; i < ColCount; i++)
                DtTable.Columns.Add((i + 1).ToString(), typeof(object));

            BR.BaseStream.Position = BR.BaseStream.Length - TextLen;
            byte[] TextData = BR.ReadBytes(TextLen);
            BR.BaseStream.Position = 20;
            for (int N = 0; N < RowCount; N++)
            {
                byte[] RowData = BR.ReadBytes(RowLen);
                object[] Cells = new object[ColCount];
                for (int k = 0; k < ColCount; k++)
                {
                    byte[] CellData = new byte[4];
                    for (int i = 0; i < 4; i++)
                    {
                        int Pos = 4 * k + i;
                        if (Pos >= RowData.Length)
                            break;
                        CellData[i] = RowData[Pos];
                    }
                    Cells[k] = BytesToint(CellData);
                }
                DtTable.Rows.Add(Cells);
            }
            FStream.Close();

            Dictionary<int, string> DicTextData = new Dictionary<int, string>();
            ArrayList ListCurText = new ArrayList();
            for (int i = 1; i < TextData.Length; i++)
            {
                if (TextData[i] != 0)
                    ListCurText.Add(TextData[i]);
                else
                {
                    byte[] CurText = (byte[])ListCurText.ToArray(typeof(byte));
                    DicTextData.Add(i - ListCurText.Count, Encoding.UTF8.GetString(CurText));
                    ListCurText.Clear();
                }
            }

            return DtTable;
        }

        public static void SaveData(string DBCFilePath, DataTable DtTable)
        {
            bool[] IsTextCol = new bool[DtTable.Columns.Count];
            bool HasTextCol = false;
            Regex Reg = new Regex("^(-)|()\\d+$");
            for (int i = 0; i < DtTable.Columns.Count; i++)
            {
                for (int j = 0; j < DtTable.Rows.Count; j++)
                {
                    if (!Reg.Match(DtTable.Rows[j][i].ToString()).Success)
                    {
                        IsTextCol[i] = true;
                        HasTextCol = true;
                        break;
                    }
                }
            }

            ArrayList ArrTextData = new ArrayList
            {
                new byte()
            };
            if (HasTextCol)
            {
                for (int i = 0; i < DtTable.Rows.Count; i++)
                {
                    for (int j = 0; j < DtTable.Columns.Count; j++)
                    {
                        if (IsTextCol[j])
                        {
                            int CurPos = ArrTextData.Count;
                            ArrTextData.AddRange(Encoding.UTF8.GetBytes(DtTable.Rows[i][j].ToString()));
                            ArrTextData.Add(new byte());
                            DtTable.Rows[i][j] = CurPos;
                        }
                    }
                }
            }

            FileStream FStream = new FileStream(DBCFilePath, FileMode.Create);
            BinaryWriter BW = new BinaryWriter(FStream);
            BW.Write(0x43424457);
            BW.Write(DtTable.Rows.Count);
            BW.Write(DtTable.Columns.Count);
            BW.Write(4 * DtTable.Columns.Count);
            BW.Write(ArrTextData.Count);
            for (int i = 0; i < DtTable.Rows.Count; i++)
            {
                for (int j = 0; j < DtTable.Columns.Count; j++)
                {
                    int Celldata = 0;
                    try
                    {
                        Celldata = int.Parse(DtTable.Rows[i][j].ToString());
                    }
                    catch
                    { }
                    BW.Write(Celldata);
                }
            }
            BW.Write((byte[])ArrTextData.ToArray(typeof(byte)));
            BW.Close();
            FStream.Close();
        }

        private static int BytesToint(byte[] Bytes)
        {
            int Ret = 0;
            Ret |= (Bytes[0] & 0xff) << 0;
            Ret |= (Bytes[1] & 0xff) << 8;
            Ret |= (Bytes[2] & 0xff) << 16;
            Ret |= (Bytes[3] & 0xff) << 24;

            return Ret;
        }
    }

    class LibMPQ
    {
        [DllImport("SFmpq.dll")]
        public static extern bool MpqAddFileToArchive(int hMPQ, string lpSourceFileName, string lpDestFileName, int dwFlags);
        [DllImport("SFmpq.dll")]
        public static extern bool MpqCompactArchive(int hMPQ);
        [DllImport("SFmpq.dll")]
        public static extern int MpqOpenArchiveForUpdate(string lpFileName, int dwFlags, int dwMaximumFilesInArchive);
        [DllImport("SFmpq.dll")]
        public static extern bool SFileCloseArchive(int hMPQ);
    }
}
