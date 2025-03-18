using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace WoWTempDBC
{
    internal static class WinApis
    {
        static WinApis() { }

        #region 程序单例运行
        ///<summary>
        /// 该函数设置由不同线程产生的窗口的显示状态
        /// </summary>
        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

        ///<summary>
        /// 设置顶级窗口并显示 仅一次
        /// </summary>
        [DllImport("User32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private static void HandleRunningInstance(Process Instance)
        {
            ShowWindowAsync(Instance.MainWindowHandle, 1);
            SetForegroundWindow(Instance.MainWindowHandle);
        }

        private static Process RuningInstance()
        {
            Process CurrentProcess = Process.GetCurrentProcess();
            Process[] Processes = Process.GetProcessesByName(CurrentProcess.ProcessName);

            foreach (Process DoProcess in Processes)
            {
                if (DoProcess.Id != CurrentProcess.Id)
                {
                    return DoProcess;
                }
            }

            return null;
        }

        /// <summary>
        /// 程序以单例运行 
        /// </summary>
        public static void SoftSingle<T>() where T : Form, new()
        {
            Process DoProcess = RuningInstance();
            if (DoProcess == null)
            {
                var MainForm = new T();
                Application.Run(MainForm);
            }
            else
            {
                HandleRunningInstance(DoProcess);
            }
        }
        #endregion
    }
}
