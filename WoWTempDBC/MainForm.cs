using MySql.Data.MySqlClient;
using Spire.Xls;
using Spire.Xls.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WoWTempDBC
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// 是否连接成功数据库
        /// </summary>
        public bool GState = false;

        /// <summary>
        /// 委托控件处理
        /// </summary>
        public delegate void CheckColEventHandler(bool On, DataSet Ds);
        public event CheckColEventHandler OnCheckColUpdate;

        /// <summary>
        /// 委托DBC文件列表
        /// </summary>
        private delegate void DBCFileInfoUpdate(string FileName);
        private DBCFileInfoUpdate DBCFileInfoUpdateDelegate;

        /// <summary>
        /// 委托SQL文件列表
        /// </summary>
        private delegate void SQLUpdate(string SQLName);
        private SQLUpdate SQLUpdateDelegate;

        /// <summary>
        /// 委托文字进度提示
        /// </summary>
        private delegate void BarLabUpdate(string MText);
        private BarLabUpdate BarLabUpdateDelegate;

        public MainForm()
        {
            InitializeComponent();
        }

        private bool TestConMysql()
        {
            string MPort = TBox1.Text.Trim();
            string MRoot = TBox2.Text.Trim();
            string MPass = TBox3.Text.Trim();

            try
            {
                string ConStringDatas = string.Format("server=localhost;port={0};user={1};password={2};database={3};charset=utf8;pooling=true", MPort, MRoot, MPass, string.Empty);
                MySqlConnection DBNameConn = new MySqlConnection(ConStringDatas);
                DBNameConn.Open();
                if (DBNameConn.State == ConnectionState.Open)
                {
                    DBNameConn.Close();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ClearListDBC()
        {
            Invoke((MethodInvoker)delegate
            {
                DataView.Items.Clear();
                DataView.Items.Add(new ListViewItem(Helper.ViewItemTitle, 0));
            });
        }

        private void ClearListSQL()
        {
            Invoke((MethodInvoker)delegate
            {
                SQLView.Items.Clear();
                SQLView.Items.Add(new ListViewItem(Helper.ViewItemTitle, 0));
            });
        }

        private void AutoConnectMysql()
        {
            if (GState)
                return;

            string MPort = TBox1.Text.Trim();
            string MRoot = TBox2.Text.Trim();
            string MPass = TBox3.Text.Trim();

            Task MTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    string ConStringDatas = string.Format("server=localhost;port={0};user={1};password={2};database={3};charset=utf8;pooling=true", MPort, MRoot, MPass, string.Empty);
                    MySqlConnection DBNameConn = new MySqlConnection(ConStringDatas);
                    DBNameConn.Open();
                    if (DBNameConn.State != ConnectionState.Open)
                    {
                        OnCheckColEvent(false);
                        return;
                    }

                    MySqlDataAdapter Adapter = new MySqlDataAdapter("SHOW DATABASES;", DBNameConn);
                    DataSet Ds = new DataSet();
                    Adapter.Fill(Ds);
                    Adapter.Dispose();

                    OnCheckColEvent(true, Ds);

                    Adapter.Dispose();
                    DBNameConn.Close();

                    Properties.Settings.Default.MPort = TBox1.Text.Trim();
                    Properties.Settings.Default.MRoot = TBox2.Text.Trim();
                    Properties.Settings.Default.MPass = TBox3.Text.Trim();
                    Properties.Settings.Default.Save();
                }
                catch (Exception)
                {
                    OnCheckColEvent(false);
                }
            });
        }

        private void AutoMysqlShowTables()
        {
            string MPort = TBox1.Text.Trim();
            string MRoot = TBox2.Text.Trim();
            string MPass = TBox3.Text.Trim();
            string DBName = CBox1.Text.Trim();
            Task MTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    string ConStringDatas = string.Format("server=localhost;port={0};user={1};password={2};database={3};charset=utf8;pooling=true", MPort, MRoot, MPass, DBName);
                    MySqlConnection DBNameConn = new MySqlConnection(ConStringDatas);
                    DBNameConn.Open();
                    if (DBNameConn.State != ConnectionState.Open)
                    {
                        OnCheckColEvent(false);
                        return;
                    }

                    MySqlDataAdapter Adapter = new MySqlDataAdapter("SHOW TABLES;", DBNameConn);
                    DataSet Ds = new DataSet();
                    Adapter.Fill(Ds);
                    Adapter.Dispose();

                    if (Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow Item in Ds.Tables[0].Rows)
                        {
                            string TabName = Item[0].ToString();
                            if (!TabName.Contains(Helper.CustomTitle))
                                continue;

                            UpdateSQLInfo(TabName);
                        }
                    }
                    Adapter.Dispose();
                    DBNameConn.Close();
                }
                catch (Exception)
                {
                    OnCheckColEvent(false);
                }
            });
        }

        private void AutoUpdateDBCFileInfo()
        {
            Task MTask = Task.Factory.StartNew(() =>
            {
                DirectoryInfo DInfo = new DirectoryInfo(Helper.SaveDBCPath);
                FileInfo[] Files = DInfo.GetFiles();
                foreach (FileInfo DoFile in Files)
                {
                    //排除类型文件
                    string Extension = Path.GetExtension(DoFile.FullName);
                    if (!Extension.ToUpper().Equals(".DBC"))
                        continue;

                    if (Helper.PListDbc.Contains(DoFile.Name))
                        continue;

                    UpdateDBCFileInfo(DoFile.Name);
                }
            });
        }

        private bool IsDataViewChecked()
        {
            int Count = 0;
            for (int i = 1; i < DataView.Items.Count; i++)
            {
                if (DataView.Items[i].Checked)
                {
                    Count++;
                }
            }
            return (Count > 0);
        }

        private string[] AutoLoadDBCFiles()
        {
            List<string> FList = new List<string>();
            for (int i = 1; i < DataView.Items.Count; i++)
            {
                if (DataView.Items[i].Checked)
                {
                    string DBCName = DataView.Items[i].Text;
                    FList.Add(Helper.SaveDBCPath + "\\" + DBCName);
                }
            }
            return FList.ToArray();
        }

        private void AutoToSQLData(string MPort, string DBName, string MRoot, string MPass)
        {
            string ConStringDatas = string.Format("server=localhost;port={0};database={1};user={2};password={3};charset=utf8;pooling=true", MPort, DBName, MRoot, MPass);
            foreach (var DoEntry in Database.Entries)
            {
                UpdateBarLabText(string.Format("正在写入数据库 [{0}]", DoEntry.FileName));
                DoEntry.ToSQLTable(ConStringDatas);
            }
        }

        private void AutoToXLSData()
        {
            foreach (var DoEntry in Database.Entries)
            {
                UpdateBarLabText(string.Format("正在写入XLS [{0}]", DoEntry.FileName));
                string CFileName = DoEntry.FileName.Substring(0, DoEntry.FileName.Length - 4);
                string CsvName = Path.Combine(Helper.TempFolder, CFileName + ".csv");
                using (StreamWriter Csv = new StreamWriter(CsvName, false, Encoding.UTF8))
                    Csv.Write(DoEntry.ToCSV());

                Workbook WBbook = new Workbook();
                WBbook.LoadFromFile(CsvName, ",", 1, 1);
                Worksheet WSheet = WBbook.Worksheets[0];
                CellRange UsedRange = WSheet.AllocatedRange;
                UsedRange.IgnoreErrorOptions = IgnoreErrorType.NumberAsText;
                UsedRange.AutoFitColumns();
                UsedRange.AutoFitRows();
                WBbook.SaveToFile(Helper.XLSFolder + CFileName + ".xlsx", ExcelVersion.Version2013);

                File.Delete(CsvName);
            }
        }

        private string[] AutoLoadSQLFiles()
        {
            List<string> FList = new List<string>();
            for (int i = 1; i < SQLView.Items.Count; i++)
            {
                if (SQLView.Items[i].Checked)
                {
                    string DBCName = SQLView.Items[i].Text;
                    DBCName = DBCName.Replace(Helper.CustomTitle, "");
                    FList.Add(DBCName);
                }
            }
            return FList.ToArray();
        }

        public void ProgressBarHandle(bool Start)
        {
            BarLab.Text = "";

            if (Start)
                AProgressBar.Start();
            else
                AProgressBar.Stop();

            BarLab.Visible = Start;

            ASBtn1.Enabled = !Start;
            ASBtn2.Enabled = !Start;
            CBox1.Enabled = !Start;
            XBtn.Enabled = !Start;
            DataView.Enabled = !Start;
            SQLView.Enabled = !Start;
            KBtn1.Enabled = !Start;
            KBtn2.Enabled = !Start;
            KBtn3.Enabled = !Start;
            KBtn4.Enabled = !Start;
        }

        public Task<bool> DBCToMPQ(string SavePath, IEnumerable<string> DBCFiles)
        {
            bool Success = false;
            int Handle = LibMPQ.MpqOpenArchiveForUpdate(SavePath, 1, 4096);
            if (Handle <= 0)
                return Task.FromResult(false);

            foreach (var DBCPath in DBCFiles)
            {
                UpdateBarLabText(string.Format("正在打包MPQ [{0}]", Path.GetFileName(DBCPath)));
                Success = LibMPQ.MpqAddFileToArchive(Handle, DBCPath, "DBFilesClient\\" + Path.GetFileName(DBCPath), 0x00030200);
            }

            if (!Success)
                return Task.FromResult(false);

            Success = LibMPQ.MpqCompactArchive(Handle);
            if (!Success)
                return Task.FromResult(false);

            Success = LibMPQ.SFileCloseArchive(Handle);
            if (!Success)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Type RType = DataView.GetType();
            PropertyInfo RInfo = RType.GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            RInfo.SetValue(DataView, true, null);

            RType = SQLView.GetType();
            RInfo = RType.GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            RInfo.SetValue(SQLView, true, null);

            DataView.Items.Add(new ListViewItem(Helper.ViewItemTitle, 0));
            SQLView.Items.Add(new ListViewItem(Helper.ViewItemTitle, 0));

            OnCheckColUpdate = OnCheckColEvent;

            DBCFileInfoUpdateDelegate = new DBCFileInfoUpdate(UpdateDBCFileInfo);
            SQLUpdateDelegate = new SQLUpdate(UpdateSQLInfo);
            BarLabUpdateDelegate = new BarLabUpdate(UpdateBarLabText);


            TBox1.Text = Properties.Settings.Default.MPort.Trim();
            TBox2.Text = Properties.Settings.Default.MRoot.Trim();
            TBox3.Text = Properties.Settings.Default.MPass.Trim();
            CBox1.Text = Properties.Settings.Default.MDB.Trim();
            TBox4.Text = Properties.Settings.Default.DBCPath.Trim();

            string Gs = (CBox1.Text.Trim() == string.Empty) ? "无" : CBox1.Text.Trim();
            LabGs2.Text = string.Format("已选择数据库: {0}", Gs);
            CBox1.Enabled = false;
            Helper.SaveDBCPath = TBox4.Text;

            Task.Factory.StartNew(() =>
            {
                if (!Directory.Exists(Helper.TempFolder))
                    Directory.CreateDirectory(Helper.TempFolder);

                if (!Directory.Exists(Helper.XLSFolder))
                    Directory.CreateDirectory(Helper.XLSFolder);

                Database.Definitions.LoadDefinition(Helper.XMLPath);
            });
        }

        private void OnCheckColEvent(bool On, DataSet Ds = null)
        {
            Invoke((MethodInvoker)delegate
            {
                LabGs1.ForeColor = On ? Color.Green : Color.Red;
                LabGs1.Text = On ? "连接数据库成功" : "连接数据库失败......";
                TBox1.Enabled = !On;
                TBox2.Enabled = !On;
                TBox3.Enabled = !On;
                CBtn.Enabled = !On;

                GState = On;
                CBox1.Enabled = On;

                CBox1.Items.Clear();

                if (On && Ds != null)
                {
                    foreach (DataRow Row in Ds.Tables[0].Rows)
                        CBox1.Items.Add(Row["Database"].ToString());
                }
            });
        }

        private void UpdateDBCFileInfo(string FileName)
        {
            if (DataView.InvokeRequired)
            {
                //调用自己
                DataView.Invoke(DBCFileInfoUpdateDelegate, FileName);
            }
            else
            {
                DataView.BeginUpdate();
                DataView.Items.Add(new ListViewItem(FileName, 0));
                DataView.EndUpdate();
            }
        }

        private void UpdateSQLInfo(string SQLName)
        {
            if (SQLView.InvokeRequired)
            {
                //调用自己
                SQLView.Invoke(SQLUpdateDelegate, SQLName);
            }
            else
            {
                SQLView.BeginUpdate();
                SQLView.Items.Add(new ListViewItem(SQLName, 0));
                SQLView.EndUpdate();
            }
        }

        public void UpdateBarLabText(string MText)
        {
            if (BarLab.InvokeRequired)
            {
                //调用自己
                BarLab.Invoke(BarLabUpdateDelegate, MText);
            }
            else
            {
                BarLab.Text = MText;
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            AutoConnectMysql();
            AutoUpdateDBCFileInfo();
            AutoMysqlShowTables();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void CBtn_Click(object sender, EventArgs e)
        {
            AutoConnectMysql();
        }

        private void CBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LabGs2.Text = string.Format("已选择数据库: {0}", CBox1.Text.Trim());
            Properties.Settings.Default.MDB = CBox1.Text.Trim();
            Properties.Settings.Default.Save();

            if (TestConMysql())
            {
                ClearListSQL();
                AutoMysqlShowTables();
            }
        }

        private void XBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog Dialog = new FolderBrowserDialog();
            DialogResult Result = Dialog.ShowDialog();
            if (Result == DialogResult.OK && !string.IsNullOrWhiteSpace(Dialog.SelectedPath))
            {
                TBox4.Text = Dialog.SelectedPath;
                Helper.SaveDBCPath = TBox4.Text.Trim();
                Properties.Settings.Default.DBCPath = TBox4.Text.Trim();
                Properties.Settings.Default.Save();

                ClearListDBC();
                AutoUpdateDBCFileInfo();
            }
        }

        private void DataView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Index == 0 && e.Item.SubItems[0].Text.Equals(Helper.ViewItemTitle))
            {
                foreach (ListViewItem LItem in DataView.Items)
                {
                    LItem.Checked = e.Item.Checked;
                }
            }
        }

        private void SQLView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Index == 0 && e.Item.SubItems[0].Text.Equals(Helper.ViewItemTitle))
            {
                foreach (ListViewItem LItem in SQLView.Items)
                {
                    LItem.Checked = e.Item.Checked;
                }
            }
        }

        private void KBtn1_Click(object sender, EventArgs e)
        {
            if (!TestConMysql())
            {
                Helper.BoxShow(this, "数据库未连接成功", "提示");
                return;
            }

            if (string.IsNullOrEmpty(Helper.SaveDBCPath) || !Directory.Exists(Helper.SaveDBCPath))
            {
                Helper.BoxShow(this, "没有定位服务端DBC目录或目录错误", "提示");
                return;
            }

            string MPort = TBox1.Text.Trim();
            string DBName = CBox1.Text.Trim();
            string MRoot = TBox2.Text.Trim();
            string MPass = TBox3.Text.Trim();

            string[] DBCFiles = AutoLoadDBCFiles();
            if (DBCFiles.Length == 0)
            {
                Helper.BoxShow(this, "没有选择需要转存的DBC文件", "提示");
                return;
            }

            ProgressBarHandle(true);
            Task.Run(() => Database.LoadFiles(DBCFiles, this)).ContinueWith(X =>
            {
                if (X.Result.Count > 0)
                {
                    ProgressBarHandle(false);
                    string Error = string.Join("\n", X.Result);
                    Helper.BoxShow(this, Error, "错误");
                    //做错误消息框提示
                    return;
                }

                Task.Run(() =>
                AutoToSQLData(MPort, DBName, MRoot, MPass)
                ).ContinueWith(T =>
                {
                    ProgressBarHandle(false);
                    Database.Entries.Clear();

                    ClearListSQL();
                    AutoMysqlShowTables();

                    if (T.IsFaulted)
                        Helper.BoxShow(this, "转存到数据库出错", "错误");
                    else
                        Helper.BoxShow(this, "所有选择的DBC文件已转存到数据库完成", "错误");

                }, TaskScheduler.FromCurrentSynchronizationContext());

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void KBtn3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Helper.SaveDBCPath) || !Directory.Exists(Helper.SaveDBCPath))
            {
                Helper.BoxShow(this, "没有定位服务端DBC目录或目录错误", "提示");
                return;
            }

            if (!IsDataViewChecked())
            {
                Helper.BoxShow(this, "没有选择需要打包的DBC文件", "提示");
                return;
            }

            SaveFileDialog SDialog = new SaveFileDialog
            {
                Title = "请选择保存的目录",
                FileName = "patch-zhCN-Z.MPQ",
                RestoreDirectory = true
            };
            DialogResult Result = SDialog.ShowDialog();
            if (Result == DialogResult.OK)
            {
                string SavePath = SDialog.FileName;
                if (File.Exists(SavePath))
                    File.Delete(SavePath);

                ProgressBarHandle(true);
                string[] DBCFiles = AutoLoadDBCFiles();
                Task.Run(() => DBCToMPQ(SavePath, DBCFiles)).ContinueWith(X =>
                {
                    if (!X.Result)
                    {
                        File.Delete(SavePath);
                        ProgressBarHandle(false);
                        Helper.BoxShow(this, "打包MPQ出错", "错误");
                        return;
                    }

                    ProgressBarHandle(false);
                    if (X.IsFaulted)
                    {
                        File.Delete(SavePath);
                        Helper.BoxShow(this, "打包MPQ出错", "错误");
                        return;
                    }

                    Helper.BoxShow(this, "所有选择的DBC文件已全部打包到MPQ", "提示");

                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void KBtn2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Helper.SaveDBCPath) || !Directory.Exists(Helper.SaveDBCPath))
            {
                Helper.BoxShow(this, "没有定位服务端DBC目录或目录错误", "提示");
                return;
            }

            if (!TestConMysql())
            {
                Helper.BoxShow(this, "数据库未连接成功", "提示");
                return;
            }

            if (SQLView.Items.Count < 1)
            {
                Helper.BoxShow(this, "数据库中没有自定义的数据 无法转存", "提示");
                return;
            }

            string[] SQLFiles = AutoLoadSQLFiles();
            if (SQLFiles.Length == 0)
            {
                Helper.BoxShow(this, "没有选择需要转存的数据库表", "提示");
                return;
            }

            string MPort = TBox1.Text.Trim();
            string MRoot = TBox2.Text.Trim();
            string MPass = TBox3.Text.Trim();
            string DBName = CBox1.Text.Trim();
            string ConStringDatas = string.Format("server=localhost;port={0};database={1};user={2};password={3};charset=utf8;pooling=true", MPort, DBName, MRoot, MPass);

            ProgressBarHandle(true);
            Task MTask = Task.Factory.StartNew(() =>
            {
                foreach (var QLFile in SQLFiles)
                {
                    string LFileName = QLFile.Substring(0, QLFile.Length - 4);
                    Table T = Database.Definitions.Tables.FirstOrDefault(A => A.Name.ToLower() == Path.GetFileNameWithoutExtension(LFileName).ToLower());
                    if (T == null)
                        continue;

                    string DBCPath = Helper.DataDBCOnePath + T.Name + ".dbc";
                    if (!File.Exists(DBCPath))
                        continue;

                    string WToDBCPath = Helper.SaveDBCPath + "\\" + T.Name + ".dbc";
                    DBReader Reader = new DBReader();
                    DBEntry DoEntry = Reader.Read(DBCPath);

                    UpdateBarLabText(string.Format("正在写入DBC [{0}]", DoEntry.FileName));

                    DoEntry.SQLToDBC(ConStringDatas);
                    new DBReader().Write(DoEntry, WToDBCPath);

                    DoEntry.Dispose();
                    DoEntry = null;
                }
            }).ContinueWith(X =>
            {
                ProgressBarHandle(false);
                if (X.IsFaulted)
                {
                    Helper.BoxShow(this, "转存到DBC时出错", "错误");
                    return;
                }

                Helper.BoxShow(this, "所有选择的数据表转存到DBC完成", "提示");

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void KBtn4_Click(object sender, EventArgs e)
        {
            string MPort = TBox1.Text.Trim();
            string DBName = CBox1.Text.Trim();
            string MRoot = TBox2.Text.Trim();
            string MPass = TBox3.Text.Trim();

            string[] DBCFiles = AutoLoadDBCFiles();
            if (DBCFiles.Length == 0)
            {
                Helper.BoxShow(this, "没有选择需要转存的DBC文件", "提示");
                return;
            }

            ProgressBarHandle(true);
            Task.Run(() => Database.LoadFiles(DBCFiles, this)).ContinueWith(X =>
            {
                if (X.Result.Count > 0)
                {
                    ProgressBarHandle(false);
                    string Error = string.Join("\n", X.Result);
                    Helper.BoxShow(this, Error, "错误");
                    //做错误消息框提示
                    return;
                }

                Task.Run(() =>
                AutoToXLSData()
                ).ContinueWith(T =>
                {
                    ProgressBarHandle(false);
                    Database.Entries.Clear();

                    if (T.IsFaulted)
                        Helper.BoxShow(this, "转存到XLS出错", "提示");
                    else
                        Helper.BoxShow(this, "所有选择的DBC文件已转存到XLS完成", "提示");

                }, TaskScheduler.FromCurrentSynchronizationContext());

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void KBtn5_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Helper.SaveDBCPath) || !Directory.Exists(Helper.SaveDBCPath))
            {
                Helper.BoxShow(this, "没有定位服务端DBC目录或目录错误", "提示");
                return;
            }

            if (!TestConMysql())
            {
                Helper.BoxShow(this, "数据库未连接成功", "提示");
                return;
            }

            string MPort = TBox1.Text.Trim();
            string MRoot = TBox2.Text.Trim();
            string MPass = TBox3.Text.Trim();
            string DBName = CBox1.Text.Trim();

            string CmdSQL = "SELECT entry, class, subclass, SoundOverrideSubclass, material, displayid, InventoryType, sheath FROM item_template ORDER BY entry ASC;";
            string ConStringDatas = string.Format("server=localhost;port={0};database={1};user={2};password={3};charset=utf8;pooling=true", MPort, DBName, MRoot, MPass);

            ProgressBarHandle(true);
            Task MTask = Task.Factory.StartNew(() =>
            {
                MySqlConnection DataConn = new MySqlConnection(ConStringDatas);
                DataConn.Open();
                MySqlDataAdapter Adapter = new MySqlDataAdapter(CmdSQL, DataConn);
                DataSet Dt = new DataSet();
                Adapter.Fill(Dt);
                Adapter.Dispose();
                DataConn.Close();

                DataTable DtMastitems = LibDBC.GetData(Helper.SaveDBCPath + "\\Item.dbc");
                DataTable DtAdditems = new DataTable();
                foreach (DataColumn Dc in DtMastitems.Columns)
                    DtAdditems.Columns.Add(Dc.ColumnName, Dc.DataType);

                int RowIndex = 0;
                DtMastitems.DefaultView.Sort = DtMastitems.Columns[0].ColumnName + " ASC";
                for (int i = 0; i < DtMastitems.Rows.Count; i++)
                {
                    if (i >= DtMastitems.Rows.Count)
                        break;

                    UpdateBarLabText("正在筛选自定义物品 " + (i * 100 / DtMastitems.Rows.Count).ToString() + "%");

                    DataRow Curdr = DtMastitems.Rows[i];
                    bool IsExists = false;
                    for (int j = RowIndex; j < Dt.Tables[0].Rows.Count; j++)
                    {
                        DataRow Row = Dt.Tables[0].Rows[j];
                        if (int.Parse(Row[0].ToString()) > int.Parse(Curdr[0].ToString()))
                            break;
                        if (Curdr[0].ToString() == Row[0].ToString())
                        {
                            IsExists = true;
                            RowIndex = j + 1;
                            break;
                        }
                    }

                    if (!IsExists)
                    {
                        DtAdditems.Rows.Add(Curdr.ItemArray);
                    }
                }

                foreach (DataRow Row in DtAdditems.Rows)
                    Dt.Tables[0].Rows.Add(Row.ItemArray);

                Dt.Tables[0].DefaultView.Sort = Dt.Tables[0].Columns[0].ColumnName + " ASC";
                DataTable SaveDtItems = Dt.Tables[0].DefaultView.ToTable();
                LibDBC.SaveData(Helper.SaveDBCPath + "\\Item.dbc", SaveDtItems);

            }).ContinueWith(X =>
            {
                ProgressBarHandle(false);
                if (X.IsFaulted)
                {
                    Helper.BoxShow(this, "转存到DBC时出错\r\nItem.dbc不存在或该文件不是有效的DBC文件", "错误");
                    return;
                }

                Helper.BoxShow(this, "物品补丁提取生成完毕", "提示");

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void ASBtn1_Click(object sender, EventArgs e)
        {
            ClearListDBC();
            AutoUpdateDBCFileInfo();
        }

        private void ASBtn2_Click(object sender, EventArgs e)
        {
            ClearListSQL();
            AutoMysqlShowTables();
        }
    }
}
