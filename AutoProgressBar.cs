using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WoWTempDBC
{
    class AutoProgressBar : ProgressBar
    {
        private readonly BackgroundWorker BWorker = new BackgroundWorker();

        public void Start()
        {
            if (BWorker.IsBusy) return;

            Style = ProgressBarStyle.Continuous;
            Value = 0;
            BWorker.DoWork += new DoWorkEventHandler(Bgw_DoWork);
            BWorker.ProgressChanged += new ProgressChangedEventHandler(Bgw_ProgressChanged);
            BWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Bgw_RunWorkerCompleted);
            BWorker.WorkerReportsProgress = true;
            BWorker.WorkerSupportsCancellation = true;
            BWorker.RunWorkerAsync(2);
        }

        void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            int Step = (int)e.Argument;
            int Pv = 0;
            while (!BWorker.CancellationPending)
            {
                Thread.Sleep(64);
                int Percent = Pv;
                if (Percent > Maximum)
                {
                    Percent = Maximum;
                    Pv = 0;
                }
                else
                    Pv += Step;

                BWorker.ReportProgress(Percent);
            }
        }

        void Bgw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (!BWorker.CancellationPending)
                    Invoke((MethodInvoker)delegate { Value = e.ProgressPercentage; });
            }
            catch (Exception)
            {

            }
        }

        void Bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Task.Run(() => ClearValue());
        }

        public void Stop()
        {
            if (BWorker.IsBusy)
                BWorker.CancelAsync();

            Task.Run(() => ClearValue());
        }

        private async Task ClearValue()
        {
            await Task.Factory.StartNew(() =>
            {
                while (BWorker.CancellationPending || Value != 0)
                {
                    Invoke((MethodInvoker)delegate { Value = 0; });
                    Task.Delay(50).Wait();
                }
            });
        }
    }
}
