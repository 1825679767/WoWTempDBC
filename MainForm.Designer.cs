
namespace WoWTempDBC
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.Lab2 = new System.Windows.Forms.Label();
            this.Lab3 = new System.Windows.Forms.Label();
            this.Lab4 = new System.Windows.Forms.Label();
            this.TBox2 = new System.Windows.Forms.TextBox();
            this.TBox3 = new System.Windows.Forms.TextBox();
            this.CBox1 = new System.Windows.Forms.ComboBox();
            this.LabGs1 = new System.Windows.Forms.Label();
            this.TBox1 = new System.Windows.Forms.TextBox();
            this.Lab1 = new System.Windows.Forms.Label();
            this.LabGs2 = new System.Windows.Forms.Label();
            this.CBtn = new System.Windows.Forms.Button();
            this.Lab5 = new System.Windows.Forms.Label();
            this.TBox4 = new System.Windows.Forms.TextBox();
            this.XBtn = new System.Windows.Forms.Button();
            this.DataView = new System.Windows.Forms.ListView();
            this.DBCName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SQLView = new System.Windows.Forms.ListView();
            this.SQLName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.KBtn1 = new System.Windows.Forms.Button();
            this.KBtn2 = new System.Windows.Forms.Button();
            this.KBtn3 = new System.Windows.Forms.Button();
            this.BarLab = new System.Windows.Forms.Label();
            this.ASBtn1 = new System.Windows.Forms.Button();
            this.ASBtn2 = new System.Windows.Forms.Button();
            this.ABLab = new System.Windows.Forms.Label();
            this.KBtn4 = new System.Windows.Forms.Button();
            this.AProgressBar = new WoWTempDBC.AutoProgressBar();
            this.KBtn5 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Lab2
            // 
            this.Lab2.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Lab2.Location = new System.Drawing.Point(224, 9);
            this.Lab2.Name = "Lab2";
            this.Lab2.Size = new System.Drawing.Size(80, 24);
            this.Lab2.TabIndex = 0;
            this.Lab2.Text = "数据库账户";
            this.Lab2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Lab3
            // 
            this.Lab3.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Lab3.Location = new System.Drawing.Point(436, 9);
            this.Lab3.Name = "Lab3";
            this.Lab3.Size = new System.Drawing.Size(80, 24);
            this.Lab3.TabIndex = 0;
            this.Lab3.Text = "数据库密码";
            this.Lab3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Lab4
            // 
            this.Lab4.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Lab4.Location = new System.Drawing.Point(12, 60);
            this.Lab4.Name = "Lab4";
            this.Lab4.Size = new System.Drawing.Size(80, 24);
            this.Lab4.TabIndex = 0;
            this.Lab4.Text = "选择数据库";
            this.Lab4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TBox2
            // 
            this.TBox2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TBox2.ForeColor = System.Drawing.Color.Blue;
            this.TBox2.Location = new System.Drawing.Point(310, 9);
            this.TBox2.Multiline = true;
            this.TBox2.Name = "TBox2";
            this.TBox2.Size = new System.Drawing.Size(120, 24);
            this.TBox2.TabIndex = 0;
            this.TBox2.TabStop = false;
            this.TBox2.Text = "root";
            // 
            // TBox3
            // 
            this.TBox3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TBox3.ForeColor = System.Drawing.Color.Blue;
            this.TBox3.Location = new System.Drawing.Point(522, 9);
            this.TBox3.Multiline = true;
            this.TBox3.Name = "TBox3";
            this.TBox3.Size = new System.Drawing.Size(120, 24);
            this.TBox3.TabIndex = 0;
            this.TBox3.TabStop = false;
            this.TBox3.Text = "root";
            // 
            // CBox1
            // 
            this.CBox1.DropDownHeight = 360;
            this.CBox1.DropDownWidth = 240;
            this.CBox1.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.CBox1.ForeColor = System.Drawing.Color.Blue;
            this.CBox1.FormattingEnabled = true;
            this.CBox1.IntegralHeight = false;
            this.CBox1.ItemHeight = 20;
            this.CBox1.Location = new System.Drawing.Point(98, 58);
            this.CBox1.MaxDropDownItems = 100;
            this.CBox1.Name = "CBox1";
            this.CBox1.Size = new System.Drawing.Size(160, 28);
            this.CBox1.TabIndex = 0;
            this.CBox1.TabStop = false;
            this.CBox1.SelectedIndexChanged += new System.EventHandler(this.CBox1_SelectedIndexChanged);
            // 
            // LabGs1
            // 
            this.LabGs1.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.LabGs1.ForeColor = System.Drawing.Color.Red;
            this.LabGs1.Location = new System.Drawing.Point(274, 60);
            this.LabGs1.Name = "LabGs1";
            this.LabGs1.Size = new System.Drawing.Size(180, 24);
            this.LabGs1.TabIndex = 0;
            this.LabGs1.Text = "未成功连接数据库";
            this.LabGs1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TBox1
            // 
            this.TBox1.BackColor = System.Drawing.SystemColors.Window;
            this.TBox1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TBox1.ForeColor = System.Drawing.Color.Blue;
            this.TBox1.Location = new System.Drawing.Point(98, 9);
            this.TBox1.Multiline = true;
            this.TBox1.Name = "TBox1";
            this.TBox1.Size = new System.Drawing.Size(120, 24);
            this.TBox1.TabIndex = 0;
            this.TBox1.TabStop = false;
            this.TBox1.Text = "3306";
            // 
            // Lab1
            // 
            this.Lab1.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Lab1.Location = new System.Drawing.Point(12, 9);
            this.Lab1.Name = "Lab1";
            this.Lab1.Size = new System.Drawing.Size(80, 24);
            this.Lab1.TabIndex = 4;
            this.Lab1.Text = "数据库端口";
            this.Lab1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // LabGs2
            // 
            this.LabGs2.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.LabGs2.ForeColor = System.Drawing.Color.Blue;
            this.LabGs2.Location = new System.Drawing.Point(460, 60);
            this.LabGs2.Name = "LabGs2";
            this.LabGs2.Size = new System.Drawing.Size(312, 24);
            this.LabGs2.TabIndex = 0;
            this.LabGs2.Text = "已选择数据库: 未选择";
            this.LabGs2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // CBtn
            // 
            this.CBtn.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.CBtn.Location = new System.Drawing.Point(652, 6);
            this.CBtn.Name = "CBtn";
            this.CBtn.Size = new System.Drawing.Size(120, 30);
            this.CBtn.TabIndex = 0;
            this.CBtn.TabStop = false;
            this.CBtn.Text = "连接数据库";
            this.CBtn.UseVisualStyleBackColor = true;
            this.CBtn.Click += new System.EventHandler(this.CBtn_Click);
            // 
            // Lab5
            // 
            this.Lab5.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Lab5.Location = new System.Drawing.Point(12, 116);
            this.Lab5.Name = "Lab5";
            this.Lab5.Size = new System.Drawing.Size(160, 24);
            this.Lab5.TabIndex = 0;
            this.Lab5.Text = "定位DBC目录";
            this.Lab5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TBox4
            // 
            this.TBox4.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TBox4.ForeColor = System.Drawing.Color.SlateGray;
            this.TBox4.Location = new System.Drawing.Point(178, 116);
            this.TBox4.Multiline = true;
            this.TBox4.Name = "TBox4";
            this.TBox4.ReadOnly = true;
            this.TBox4.Size = new System.Drawing.Size(464, 24);
            this.TBox4.TabIndex = 0;
            this.TBox4.TabStop = false;
            // 
            // XBtn
            // 
            this.XBtn.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.XBtn.Location = new System.Drawing.Point(652, 113);
            this.XBtn.Name = "XBtn";
            this.XBtn.Size = new System.Drawing.Size(120, 30);
            this.XBtn.TabIndex = 0;
            this.XBtn.TabStop = false;
            this.XBtn.Text = "选择目录";
            this.XBtn.UseVisualStyleBackColor = true;
            this.XBtn.Click += new System.EventHandler(this.XBtn_Click);
            // 
            // DataView
            // 
            this.DataView.BackColor = System.Drawing.SystemColors.Menu;
            this.DataView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DataView.CheckBoxes = true;
            this.DataView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.DBCName});
            this.DataView.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.DataView.ForeColor = System.Drawing.Color.Blue;
            this.DataView.FullRowSelect = true;
            this.DataView.HideSelection = false;
            this.DataView.Location = new System.Drawing.Point(12, 216);
            this.DataView.MultiSelect = false;
            this.DataView.Name = "DataView";
            this.DataView.Size = new System.Drawing.Size(340, 500);
            this.DataView.TabIndex = 0;
            this.DataView.TabStop = false;
            this.DataView.UseCompatibleStateImageBehavior = false;
            this.DataView.View = System.Windows.Forms.View.Details;
            this.DataView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.DataView_ItemChecked);
            // 
            // DBCName
            // 
            this.DBCName.Text = "DBC文件名";
            this.DBCName.Width = 320;
            // 
            // SQLView
            // 
            this.SQLView.BackColor = System.Drawing.SystemColors.Menu;
            this.SQLView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SQLView.CheckBoxes = true;
            this.SQLView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.SQLName});
            this.SQLView.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.SQLView.ForeColor = System.Drawing.Color.Blue;
            this.SQLView.FullRowSelect = true;
            this.SQLView.HideSelection = false;
            this.SQLView.Location = new System.Drawing.Point(432, 216);
            this.SQLView.MultiSelect = false;
            this.SQLView.Name = "SQLView";
            this.SQLView.Size = new System.Drawing.Size(340, 500);
            this.SQLView.TabIndex = 0;
            this.SQLView.TabStop = false;
            this.SQLView.UseCompatibleStateImageBehavior = false;
            this.SQLView.View = System.Windows.Forms.View.Details;
            this.SQLView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.SQLView_ItemChecked);
            // 
            // SQLName
            // 
            this.SQLName.Text = "数据库表名";
            this.SQLName.Width = 320;
            // 
            // KBtn1
            // 
            this.KBtn1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.KBtn1.Location = new System.Drawing.Point(12, 733);
            this.KBtn1.Name = "KBtn1";
            this.KBtn1.Size = new System.Drawing.Size(120, 30);
            this.KBtn1.TabIndex = 0;
            this.KBtn1.TabStop = false;
            this.KBtn1.Text = "DBC->数据库";
            this.KBtn1.UseVisualStyleBackColor = true;
            this.KBtn1.Click += new System.EventHandler(this.KBtn1_Click);
            // 
            // KBtn2
            // 
            this.KBtn2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.KBtn2.Location = new System.Drawing.Point(652, 733);
            this.KBtn2.Name = "KBtn2";
            this.KBtn2.Size = new System.Drawing.Size(120, 30);
            this.KBtn2.TabIndex = 5;
            this.KBtn2.TabStop = false;
            this.KBtn2.Text = "数据库->DBC";
            this.KBtn2.UseVisualStyleBackColor = true;
            this.KBtn2.Click += new System.EventHandler(this.KBtn2_Click);
            // 
            // KBtn3
            // 
            this.KBtn3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.KBtn3.Location = new System.Drawing.Point(491, 733);
            this.KBtn3.Name = "KBtn3";
            this.KBtn3.Size = new System.Drawing.Size(120, 30);
            this.KBtn3.TabIndex = 6;
            this.KBtn3.TabStop = false;
            this.KBtn3.Text = "DBC->MPQ";
            this.KBtn3.UseVisualStyleBackColor = true;
            this.KBtn3.Click += new System.EventHandler(this.KBtn3_Click);
            // 
            // BarLab
            // 
            this.BarLab.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.BarLab.ForeColor = System.Drawing.Color.Blue;
            this.BarLab.Location = new System.Drawing.Point(8, 149);
            this.BarLab.Name = "BarLab";
            this.BarLab.Size = new System.Drawing.Size(760, 24);
            this.BarLab.TabIndex = 0;
            this.BarLab.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ASBtn1
            // 
            this.ASBtn1.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ASBtn1.Location = new System.Drawing.Point(12, 180);
            this.ASBtn1.Name = "ASBtn1";
            this.ASBtn1.Size = new System.Drawing.Size(120, 30);
            this.ASBtn1.TabIndex = 0;
            this.ASBtn1.TabStop = false;
            this.ASBtn1.Text = "刷新列表";
            this.ASBtn1.UseVisualStyleBackColor = true;
            this.ASBtn1.Click += new System.EventHandler(this.ASBtn1_Click);
            // 
            // ASBtn2
            // 
            this.ASBtn2.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ASBtn2.Location = new System.Drawing.Point(652, 180);
            this.ASBtn2.Name = "ASBtn2";
            this.ASBtn2.Size = new System.Drawing.Size(120, 30);
            this.ASBtn2.TabIndex = 0;
            this.ASBtn2.TabStop = false;
            this.ASBtn2.Text = "刷新列表";
            this.ASBtn2.UseVisualStyleBackColor = true;
            this.ASBtn2.Click += new System.EventHandler(this.ASBtn2_Click);
            // 
            // ABLab
            // 
            this.ABLab.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ABLab.ForeColor = System.Drawing.Color.CadetBlue;
            this.ABLab.Location = new System.Drawing.Point(12, 770);
            this.ABLab.Name = "ABLab";
            this.ABLab.Size = new System.Drawing.Size(760, 24);
            this.ABLab.TabIndex = 0;
            this.ABLab.Text = "转存的数据库的表名称不要更改 否则出错 QQ3226488166";
            this.ABLab.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // KBtn4
            // 
            this.KBtn4.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.KBtn4.Location = new System.Drawing.Point(168, 733);
            this.KBtn4.Name = "KBtn4";
            this.KBtn4.Size = new System.Drawing.Size(120, 30);
            this.KBtn4.TabIndex = 7;
            this.KBtn4.TabStop = false;
            this.KBtn4.Text = "DBC->XLS";
            this.KBtn4.UseVisualStyleBackColor = true;
            this.KBtn4.Click += new System.EventHandler(this.KBtn4_Click);
            // 
            // AProgressBar
            // 
            this.AProgressBar.Location = new System.Drawing.Point(138, 182);
            this.AProgressBar.Maximum = 512;
            this.AProgressBar.Name = "AProgressBar";
            this.AProgressBar.Size = new System.Drawing.Size(504, 24);
            this.AProgressBar.TabIndex = 0;
            // 
            // KBtn5
            // 
            this.KBtn5.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.KBtn5.Location = new System.Drawing.Point(327, 733);
            this.KBtn5.Name = "KBtn5";
            this.KBtn5.Size = new System.Drawing.Size(120, 30);
            this.KBtn5.TabIndex = 8;
            this.KBtn5.TabStop = false;
            this.KBtn5.Text = "物品补丁DBC";
            this.KBtn5.UseVisualStyleBackColor = true;
            this.KBtn5.Click += new System.EventHandler(this.KBtn5_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(784, 801);
            this.Controls.Add(this.KBtn5);
            this.Controls.Add(this.KBtn4);
            this.Controls.Add(this.ABLab);
            this.Controls.Add(this.ASBtn2);
            this.Controls.Add(this.ASBtn1);
            this.Controls.Add(this.AProgressBar);
            this.Controls.Add(this.BarLab);
            this.Controls.Add(this.SQLView);
            this.Controls.Add(this.DataView);
            this.Controls.Add(this.KBtn3);
            this.Controls.Add(this.KBtn2);
            this.Controls.Add(this.KBtn1);
            this.Controls.Add(this.XBtn);
            this.Controls.Add(this.TBox4);
            this.Controls.Add(this.Lab5);
            this.Controls.Add(this.CBtn);
            this.Controls.Add(this.LabGs2);
            this.Controls.Add(this.TBox1);
            this.Controls.Add(this.Lab1);
            this.Controls.Add(this.LabGs1);
            this.Controls.Add(this.CBox1);
            this.Controls.Add(this.TBox3);
            this.Controls.Add(this.TBox2);
            this.Controls.Add(this.Lab4);
            this.Controls.Add(this.Lab3);
            this.Controls.Add(this.Lab2);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(800, 840);
            this.MinimumSize = new System.Drawing.Size(800, 840);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "简化版数据转换工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Lab2;
        private System.Windows.Forms.Label Lab3;
        private System.Windows.Forms.Label Lab4;
        private System.Windows.Forms.TextBox TBox2;
        private System.Windows.Forms.TextBox TBox3;
        private System.Windows.Forms.ComboBox CBox1;
        private System.Windows.Forms.Label LabGs1;
        private System.Windows.Forms.TextBox TBox1;
        private System.Windows.Forms.Label Lab1;
        private System.Windows.Forms.Label LabGs2;
        private System.Windows.Forms.Button CBtn;
        private System.Windows.Forms.Label Lab5;
        private System.Windows.Forms.TextBox TBox4;
        private System.Windows.Forms.Button XBtn;
        private System.Windows.Forms.ListView DataView;
        private System.Windows.Forms.ColumnHeader DBCName;
        private System.Windows.Forms.ListView SQLView;
        private System.Windows.Forms.ColumnHeader SQLName;
        private System.Windows.Forms.Button KBtn1;
        private System.Windows.Forms.Button KBtn2;
        private System.Windows.Forms.Button KBtn3;
        private System.Windows.Forms.Label BarLab;
        private AutoProgressBar AProgressBar;
        private System.Windows.Forms.Button ASBtn1;
        private System.Windows.Forms.Button ASBtn2;
        private System.Windows.Forms.Label ABLab;
        private System.Windows.Forms.Button KBtn4;
        private System.Windows.Forms.Button KBtn5;
    }
}

