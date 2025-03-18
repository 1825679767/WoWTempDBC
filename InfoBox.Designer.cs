
namespace WoWTempDBC
{
    partial class InfoBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InfoBox));
            this.IBox = new System.Windows.Forms.TextBox();
            this.OBtn = new System.Windows.Forms.Button();
            this.CBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // IBox
            // 
            this.IBox.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.IBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.IBox.ForeColor = System.Drawing.Color.Blue;
            this.IBox.Location = new System.Drawing.Point(12, 12);
            this.IBox.MaxLength = 2147364847;
            this.IBox.Multiline = true;
            this.IBox.Name = "IBox";
            this.IBox.ReadOnly = true;
            this.IBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.IBox.Size = new System.Drawing.Size(380, 200);
            this.IBox.TabIndex = 0;
            this.IBox.TabStop = false;
            this.IBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // OBtn
            // 
            this.OBtn.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.OBtn.Location = new System.Drawing.Point(220, 224);
            this.OBtn.Name = "OBtn";
            this.OBtn.Size = new System.Drawing.Size(80, 30);
            this.OBtn.TabIndex = 1;
            this.OBtn.TabStop = false;
            this.OBtn.Text = "确定";
            this.OBtn.UseVisualStyleBackColor = true;
            this.OBtn.Click += new System.EventHandler(this.OBtn_Click);
            // 
            // CBtn
            // 
            this.CBtn.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.CBtn.Location = new System.Drawing.Point(312, 224);
            this.CBtn.Name = "CBtn";
            this.CBtn.Size = new System.Drawing.Size(80, 30);
            this.CBtn.TabIndex = 2;
            this.CBtn.TabStop = false;
            this.CBtn.Text = "取消";
            this.CBtn.UseVisualStyleBackColor = true;
            this.CBtn.Click += new System.EventHandler(this.CBtn_Click);
            // 
            // InfoBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(404, 261);
            this.Controls.Add(this.CBtn);
            this.Controls.Add(this.OBtn);
            this.Controls.Add(this.IBox);
            this.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(420, 300);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(420, 300);
            this.Name = "InfoBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox IBox;
        private System.Windows.Forms.Button OBtn;
        private System.Windows.Forms.Button CBtn;
    }
}