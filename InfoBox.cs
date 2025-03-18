using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WoWTempDBC
{
    public enum BtnTypes
    {
        /// <summary>
        /// 仅有确定按钮
        /// </summary>
        OK = 0,
        /// <summary>
        /// 确定和取消按钮
        /// </summary>
        OKCancel = 1,
    }

    public partial class InfoBox : Form
    {
        public InfoBox(Form MainForm, string MText, string WinText, BtnTypes BType)
        {
            InitializeComponent();

            if (MainForm == null)
            {
                StartPosition = FormStartPosition.CenterScreen;
            }
            else
            {
                MainForm.Activate();
                MainForm.WindowState = FormWindowState.Normal;

                StartPosition = FormStartPosition.Manual;
                Location = new Point(MainForm.Location.X + (MainForm.Width - Width) / 2, MainForm.Location.Y + (MainForm.Height - Height) / 2);
            }

            Text = WinText;
            IBox.Text = MText;
            if (BType == BtnTypes.OK)
            {
                CBtn.Visible = false;
                OBtn.Location = CBtn.Location;
                CBtn.Enabled = false;
            }
        }

        private void OBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
