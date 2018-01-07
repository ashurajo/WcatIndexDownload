using System;
using System.Net;
using System.IO;
using UnityExport;

namespace 清單下載器
{
    public class DownloadInfo
    {
        private string URL = "", SavePath = "";
        public bool IsDone { get; private set; }
        public bool IsCancelled { get; private set; }
        public string FileName { get; private set; }
        public string Error { get; private set; }
        public int KBytesReceived { get; private set; }
        public int TotalKBytesToReceive { get; private set; }
        public WebClient WC = new WebClient();
        private Form1 form = null;

        public DownloadInfo(string URL, string SavePath, Form1 form, string FileName = "")
        {
            IsDone = false;
            IsCancelled = false;
            this.URL = URL;
            this.SavePath = SavePath;
            if (FileName != "") this.FileName = FileName;
            else this.FileName = Path.GetFileName(URL);
            this.form = form;
        }

        public void StartDownload()
        {
            WC.DownloadDataCompleted += new DownloadDataCompletedEventHandler((sender, e) =>
            {
                if (!e.Cancelled)
                {
                    if (e.Error == null)
                    {
                        Form1.ExtFile.Add(Path.GetFileNameWithoutExtension(FileName), new MemoryStream(e.Result));
                        if (!form.LogForm.iscolse)
                        {
                            if (form.LogForm.InvokeRequired) form.LogForm.BeginInvoke(new Action(delegate { form.LogForm.richTextBox1.AppendText(FileName + " 下載完成\r\n"); }));
                            else form.LogForm.richTextBox1.AppendText(FileName + " 下載完成\r\n");
                        }
                    }
                }
                IsCancelled = e.Cancelled;
                if (!IsCancelled) Error = (e.Error != null ? e.Error.Message : "");
                IsDone = true;
                WC.Dispose();
                WC = null;
            });
            WC.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) =>
            {
                KBytesReceived = (int)(e.BytesReceived / 1024);
                TotalKBytesToReceive = (int)(e.TotalBytesToReceive / 1024);
            });
            WC.DownloadDataAsync(new Uri(URL));
        }
    }
}
