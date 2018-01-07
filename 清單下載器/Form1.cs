using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using 清單下載器.Properties;
using UnityExport;

namespace 清單下載器
{
    public partial class Form1 : Form
    {
        public Log LogForm = new Log();
        Stopwatch SW = new Stopwatch();
        TaskbarManager tbManager = TaskbarManager.Instance;
        AutoResetEvent evtDownload = new AutoResetEvent(false);
        public static Dictionary<string, MemoryStream> ExtFile;
        bool Working;
        int RAM = 0;
        PerformanceCounter cpuPF = null, ramPF = null;

        #region 檔案處理
        private void btn_Save_Click(object sender, EventArgs e)
        {
            if (FBO.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.SavePath = FBO.SelectedPath;
                Directory_Read(FBO.SelectedPath);
            }
        }

        private void btn_FIle_Click(object sender, EventArgs e)
        {
            if (OFD.ShowDialog() == DialogResult.OK) File_Read(OFD.FileName);
        }

        private bool File_Read(string File_path)
        {
            if (File_path == "")
            {
                MessageBox.Show("未選擇下載清單");
                return false;
            }
            if (!File.Exists(File_path))
            {
                MessageBox.Show("路徑錯誤，無檔案");
                return false;
            }
            txt_File.Text = File_path;
            lib_File.Items.Clear();
            lib_Error.Items.Clear();
            lib_File.BeginUpdate();
            foreach (string item in File.ReadAllLines(File_path)) lib_File.Items.Add(item.Replace(".", "_").Replace("/", "_").Split(new char[] { ',' })[0]);
            lib_File.EndUpdate();
            SetLabelText("下載檔案:無", lab_Download);
            SetProgressBarValue(0, PB_TotalFile);
            SetProgressBarValue(0, PB_SingleFile);
            SetProgressBarMaxValue(lib_File.Items.Count, PB_TotalFile);
            lab_Item.Text = "檔案數: " + lib_File.Items.Count;
            lab_ErrorItem.Text = "錯誤檔案數: 0";
            lab_DownItem.Text = "已下載數: 0";
            lab_Execute.Text = "解包: 無";
            btn_Start.Enabled = true;
            btn_Save_Error.Enabled = false;
            return true;
        }

        private void Directory_Read(string Directory_path)
        {
            if (Program.noCreateDirectory) txt_Save.Text = Directory_path + "\\";
            else txt_Save.Text = Directory_path + "\\" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + "\\";
            txt_Save.Text = txt_Save.Text.Replace("\\\\", "\\");
            SFD.InitialDirectory = Directory_path;
        }
        #endregion
        
        #region 委派
        private delegate void SetButton(bool Enable, Button Btn);
        private delegate void SetGroup(bool Enable, GroupBox GB);
        private delegate void SetToolStripLabel(string Text, ToolStripLabel Tool);
        private delegate void SetToolStripTextBox(bool enable);
        private delegate void SetLable(string Text, Label lab);
        private delegate void SetTool(bool enable, ToolStripMenuItem TSM);
        private delegate void SetTextBox(bool enable, ToolStripTextBox TST);
        private delegate void SetCheckBox(bool enable);
        private delegate void SetListItem(string Text, ListBox List);
        private delegate void SetProgressValue(int Value, ProgressBar PB);

        private void SetCheckBoxEnable(bool enable)
        {
            if (InvokeRequired)
            {
                Invoke(new SetCheckBox(SetCheckBoxEnable), new object[] { enable });
                return;
            }
            chb_Unity3d.Enabled = enable;
        }

        private void SetToolStripTextBoxEnable(bool enable)
        {
            if (InvokeRequired)
            {
                Invoke(new SetToolStripTextBox(SetToolStripTextBoxEnable), new object[] { enable });
                return;
            }
            TSM_Exclude.Enabled = enable;
        }

        private void SetTextBoxReadOnly(bool enable, ToolStripTextBox TST)
        {
            if (InvokeRequired)
            {
                Invoke(new SetTextBox(SetTextBoxReadOnly), new object[] { enable, TST });
                return;
            }
            TST.ReadOnly = enable;
        }

        private void SetGroupBoxEnable(bool Enable, GroupBox GB)
        {
            if (InvokeRequired)
            {
                Invoke(new SetGroup(SetGroupBoxEnable), new object[] { Enable, GB });
                return;
            }
            GB.Enabled = Enable;
        }

        private void SetToolStripMenuuItemEnable(bool Enable, ToolStripMenuItem TSM)
        {
            if (InvokeRequired)
            {
                Invoke(new SetTool(SetToolStripMenuuItemEnable), new object[] { Enable, TSM });
                return;
            }
            TSM.Enabled = Enable;
        }

        private void SetButtonEnable(bool Enable, Button Btn)
        {
            if (InvokeRequired)
            {
                Invoke(new SetButton(SetButtonEnable), new object[] { Enable, Btn });
                return;
            }
            Btn.Enabled = Enable;
        }

        private void SetLabelText(string Text, Label lab)
        {
            if (InvokeRequired)
            {
                Invoke(new SetLable(SetLabelText), new object[] { Text, lab });
                return;
            }
            lab.Text = Text;
        }

        private void SetToolStripLabelText(string Text, ToolStripLabel Tool)
        {
            if (InvokeRequired)
            {
                Invoke(new SetToolStripLabel(SetToolStripLabelText), new object[] { Text, Tool });
                return;
            }
            Tool.Text = Text;
            if (Tool == lab_Execute && !LogForm.iscolse) LogForm.richTextBox1.AppendText(Text + "\r\n");
        }

        private void AddListBoxItem(string Text, ListBox List)
        {
            if (InvokeRequired)
            {
                Invoke(new SetListItem(AddListBoxItem), new object[] { Text, List });
                return;
            }
            List.Items.Add(Text);
        }

        private void DelListBox(string Text, ListBox List)
        {
            if (InvokeRequired)
            {
                Invoke(new SetListItem(DelListBox), new object[] { Text, List });
                return;
            }
            List.Items.Remove(Text);
        }

        private void SetProgressBarValue(int Value, ProgressBar PB)
        {
            if (Value > PB.Maximum) return;
            if (InvokeRequired)
            {
                Invoke(new SetProgressValue(SetProgressBarValue), new object[] { Value, PB });
                return;
            }
            PB.Value = Value;
        }

        private void SetProgressBarMaxValue(int Value, ProgressBar PB)
        {
            if (InvokeRequired)
            {
                Invoke(new SetProgressValue(SetProgressBarMaxValue), new object[] { Value, PB });
                return;
            }
            PB.Maximum = Value;
        }
        #endregion

        public Form1()
        {
            InitializeComponent();
            ExtFile = new Dictionary<string, MemoryStream>();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Program.filePath != "") File_Read(Program.filePath);
            if (Program.directoryPath != "") Directory_Read(Program.directoryPath);
            else if (Settings.Default.SavePath == "") Directory_Read(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            else Directory_Read(Settings.Default.SavePath);
            if (Program.exclude != "") TSM_Exclude.Text = Program.exclude;
            else TSM_Exclude.Text = Settings.Default.exclude;
            TST_DLLength.Text = Settings.Default.DLLength;
            txt_URL.Text = Settings.Default.URL;
            TSM_UnPrefab.Checked = Settings.Default.UnPrefab;
            TSM_Convert.Checked = Settings.Default.Convert;
            TSM_OnlyBundle.Checked = Settings.Default.OnlyBundle;
            TSM_Log.Checked = Settings.Default.Log;
            switch (Program.server)
            {
                case 1:
                    rdb_ST.Checked = true;
                    break;
                case 2:
                    rdb_SJ.Checked = true;
                    break;
                case 3:
                    rdb_SK.Checked = true;
                    break;
                case 4:
                    rdb_tennis.Checked = true;
                    break;
            }
            if (Program.ver == 1) RB_A.Checked = true;
            else RB_I.Checked = true;
            if (Program.autoDownload) btn_Start.PerformClick();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Working)
            {
                MessageBox.Show("還在下載中，請正常暫停後再關閉");
                e.Cancel = true;
            }
            Settings.Default.exclude = TSM_Exclude.Text;
            Settings.Default.URL = txt_URL.Text;
            Settings.Default.DLLength = TST_DLLength.Text;
            Settings.Default.Save();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false)) e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            foreach (string item in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                if (Directory.Exists(item)) Directory_Read(item);
                else if (item.EndsWith(".txt") && File.Exists(item)) File_Read(item);
            }
        }

        private void TSM_Click(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = !((ToolStripMenuItem)sender).Checked;
            if (sender == TSM_UnPrefab)
            {
                Settings.Default.UnPrefab = ((ToolStripMenuItem)sender).Checked;
                TSM_Exclude.ReadOnly = !((ToolStripMenuItem)sender).Checked;
                return;
            }
            if (sender == TSM_Convert)
            {
                Settings.Default.Convert = ((ToolStripMenuItem)sender).Checked;
                return;
            }
            if (sender == TSM_Log)
            {
                Settings.Default.Log = ((ToolStripMenuItem)sender).Checked;
                return;
            }
            if (sender == TSM_OpenLogForm)
            {
                if (LogForm.iscolse) LogForm.Show();
                return;
            }
            Settings.Default.OnlyBundle = ((ToolStripMenuItem)sender).Checked;
        }

        private void rdb_Custom_CheckedChanged(object sender, EventArgs e)
        {
            txt_URL.ReadOnly = !rdb_Custom.Checked;
            GB_Ver.Enabled = !rdb_Custom.Checked;
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            if (txt_Save.Text == "")
            {
                MessageBox.Show("輸出路徑不可空白");
                return;
            }
            if (txt_File.Text == "")
            {
                MessageBox.Show("未選擇清單檔案");
                return;
            }
            if (!File_Read(txt_File.Text)) return;

            if (rdb_Custom.Checked)
            {
                if (!txt_URL.Text.StartsWith("http://") && !txt_URL.Text.StartsWith("https://") && MessageBox.Show("網址非http或https開頭\r\n是否繼續?", "確認", MessageBoxButtons.YesNo) == DialogResult.No) return;
                if (!txt_URL.Text.EndsWith("/") && MessageBox.Show("網址非\"/\"結尾\r\n是否繼續?", "確認", MessageBoxButtons.YesNo) == DialogResult.No) return;
            }

            if (TSM_Log.Checked) { LogForm.richTextBox1.Text = ""; if (LogForm.iscolse) LogForm.Show(); }
            try { ServicePointManager.DefaultConnectionLimit = Convert.ToInt32(TST_DLLength.Text); }
            catch (Exception) { ServicePointManager.DefaultConnectionLimit = 2; }

            Process pro = Process.GetCurrentProcess();
            if (cpuPF == null)
            {
                cpuPF = new PerformanceCounter();
                cpuPF.CategoryName = "Process";
                cpuPF.CounterName = "% Processor Time";
                cpuPF.InstanceName = pro.ProcessName;
                cpuPF.MachineName = ".";
            }

            if (ramPF == null)
            {
                ramPF = new PerformanceCounter();
                ramPF.CategoryName = "Process";
                ramPF.CounterName = "Private Bytes";
                ramPF.InstanceName = pro.ProcessName;
                ramPF.MachineName = ".";
            }
            pro.Dispose();

            Work_Event(true);
            SetToolStripLabelText("下載中(0%)", lab_Status);
            timer1.Start();
            SW.Restart();
            new Thread(RunDownload).Start();
        }

        private void btn_Save_Error_Click(object sender, EventArgs e)
        {
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                using (FileStream fileStream = new FileStream(SFD.FileName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(fileStream))
                    {
                        foreach (string text in lib_Error.Items) if ((text != "") && (text != "\r\n")) streamWriter.WriteLine(text.ToString());
                        lab_Status.Text = "寫入完成";
                    }
                }
            }
        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            Working = false;
            btn_Stop.Enabled = false;
        }

        private void lib_File_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && !Working)
            {
                richTextBox1.Clear();
                richTextBox1.Font = lib_File.Font;
                string[] List = lib_File.Items.Cast<string>().ToArray();
                foreach (string item in List) richTextBox1.Text += item + "\r\n";
                richTextBox1.Visible = true;
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                if (richTextBox1.Text != "")
                {
                    File.WriteAllText(Application.StartupPath + "\\FileList.txt", richTextBox1.Text);
                    File_Read(Application.StartupPath + "\\FileList.txt");
                }
                richTextBox1.Visible = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lab_time.Text = string.Format("已用時間: {0}:{1}", SW.Elapsed.Minutes.ToString("D2"), SW.Elapsed.Seconds.ToString("D2"));
            Text = "清單檔案下載器 " + ("記憶體:" + (RAM = (int)(ramPF.NextValue() / 1048576f)).ToString() + "MB CPU使用率:" + Math.Round(cpuPF.NextValue() / Environment.ProcessorCount, 2).ToString() + "%");
            if (RAM >= 1536) GC.Collect();
        }

        private void RunDownload()
        {
            string Extension = (chb_Unity3d.Checked ? ".unity3d" : "");
            string[] FileList = lib_File.Items.Cast<string>().ToArray();
            tbManager.SetProgressValue(0, FileList.Length);
            tbManager.SetProgressState(TaskbarProgressBarState.Normal);
            string Url;

            if (rdb_ST.Checked) Url = "http://img.wcproject.so-net.tw/assets/469/";
            else if (rdb_SJ.Checked) Url = "http://i-wcat-colopl-jp.akamaized.net/assets/465/";
            else if (rdb_SK.Checked) Url = "http://i-wcat-colopl-kr.akamaized.net/assets/465/";
            else if (rdb_tennis.Checked) Url = "http://i-tennis-colopl-jp.akamaized.net/asset_bundle/";
            else Url = txt_URL.Text;

            if (rdb_tennis.Checked && RB_A.Checked) Url += "android/0.0.1/";
            else if (rdb_tennis.Checked && RB_I.Checked) Url += "ios/0.0.1/";
            else if (!rdb_Custom.Checked && RB_A.Checked) Url += "a/";
            else if (!rdb_Custom.Checked && RB_I.Checked) Url += "i/";
            
            List<DownloadInfo> list = new List<DownloadInfo>();
            SetProgressBarValue(0, PB_TotalFile);
            foreach (string fileName in FileList)
            {
                if (fileName == "" || fileName.Substring(0, 1) == "'") DelListBox(fileName.ToString(), lib_File);
                else
                {
                    string urlAddress = Url + fileName + Extension + "?r=" + DateTime.Now.ToFileTime().ToString();
                    DownloadInfo info = new DownloadInfo(urlAddress, txt_Save.Text, this, fileName + ".unity3d");
                    list.Add(info);
                    info.StartDownload();
                }
            }
            list.ForEach(delegate (DownloadInfo item)
            {
                SetToolStripLabelText("下載中 (" + (PB_TotalFile.Value * 100.0 / list.Count).ToString("0.0") + "%)", lab_Status);
                while (!item.IsDone)
                {
                    if (!Working) { if (item.WC != null) item.WC.CancelAsync(); break; }
                    SetLabelText("下載檔案: " + item.FileName, lab_Download);
                    SetLabelText(string.Format("{0} KB / {1} KB", item.KBytesReceived.ToString("0.0"), item.TotalKBytesToReceive.ToString("0.0")), lab_Speed);
                    SetProgressBarValue(item.KBytesReceived, PB_SingleFile);
                    SetProgressBarMaxValue(item.TotalKBytesToReceive, PB_SingleFile);
                    Thread.Sleep(500);
                }
                if (item.IsCancelled) AddListBoxItem("C." + item.FileName, lib_Error);
                else if (item.Error != "") AddListBoxItem("E." + item.FileName, lib_Error);
                SetProgressBarValue(PB_TotalFile.Value + 1, PB_TotalFile);
                SetToolStripLabelText("錯誤檔案數: " + lib_Error.Items.Count.ToString(), lab_ErrorItem);
                SetToolStripLabelText("已下載數: " + PB_TotalFile.Value, lab_DownItem);
                DelListBox(Path.GetFileNameWithoutExtension(item.FileName), lib_File);
                tbManager.SetProgressValue(PB_TotalFile.Value, FileList.Length);
            });
            list.Clear();
            SetProgressBarValue(0, PB_SingleFile);
            SetProgressBarMaxValue(0, PB_SingleFile);
            SetLabelText("0 KB / 0 KB", lab_Speed);
            if (ExtFile.Count != 0)
            {
                if (!Directory.Exists(txt_Save.Text)) Directory.CreateDirectory(txt_Save.Text);
                SetToolStripLabelText("檔案數: " + ExtFile.Count, lab_Item);
                SetProgressBarValue(0, PB_TotalFile);
                SetProgressBarMaxValue(ExtFile.Count, PB_TotalFile);
                SetToolStripLabelText("解包中", lab_Status);
                Parallel.ForEach(ExtFile, (item) =>
                {
                    if (item.Key == null || item.Value == null) return;
                    BundleFile BF = new BundleFile(item.Value);
                    if (!TSM_OnlyBundle.Checked)
                    {
                        bool Export = true;
                        if (TSM_UnPrefab.Checked && TSM_Exclude.Text != "") foreach (string item2 in TSM_Exclude.Text.Split(new char[] { ',' })) if (Path.GetFileNameWithoutExtension(item.Key).ToLower().Contains(item2.ToLower())) { Export = false; break; }
                        if (Export)
                        {
                            SetToolStripLabelText("解包: " + item.Key, lab_Execute);
                            using (EndianStream ES = new EndianStream(BF.MemoryAssetsFileList[0].memStream, EndianType.BigEndian))
                            {
                                AssetsFile AF = new AssetsFile(item.Key, ES);
                                foreach (var item2 in AF.preloadTable.Values)
                                {
                                    string SavePath = "";
                                    switch (item2.Type2)
                                    {
                                        case 28:
                                            SavePath = txt_Save.Text + "Texture\\";
                                            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
                                            var m_Texture2D = new Texture2D(item2, true);
                                            var bitmap = m_Texture2D.ConvertToBitmap(true);
                                            if (bitmap != null)
                                            {
                                                if (!item.Key.Contains("std") && (bitmap.Width == 1024 && bitmap.Height == 1024)) bitmap = Static_Function.ResizeImage(bitmap, 1024, 1331);
                                                bitmap.Save(SavePath + item.Key + ".png", ImageFormat.Png);
                                                bitmap.Dispose();
                                            }
                                            m_Texture2D = null;
                                            break;
                                        case 83:
                                            SavePath = txt_Save.Text + "Audio\\";
                                            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
                                            AudioClip m_AudioClip = new AudioClip(item2, true);
                                            File.WriteAllBytes(SavePath + item.Key + ".mp3", m_AudioClip.m_AudioData);
                                            m_AudioClip = null;
                                            break;
                                        case 49:
                                            SavePath = txt_Save.Text + "Text\\";
                                            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
                                            TextAsset TA = new TextAsset(item2, true);
                                            File.WriteAllBytes(SavePath + item.Key + ".txt", TA.m_Script);
                                            TA = null;
                                            break;
                                        case 114:
                                            SavePath = txt_Save.Text + "Text\\";
                                            if (!Directory.Exists(SavePath )) Directory.CreateDirectory(SavePath);
                                            MonoBehaviour MB = new MonoBehaviour(item2, true);
                                            File.WriteAllText(SavePath + item.Key + ".txt", MB.serializedText);
                                            MB = null;
                                            break;
                                    }
                                }
                                AF = null;
                            }
                        }
                        else { SetToolStripLabelText("略過: " + item.Key, lab_Execute); Static_Function.WriteBundleFile(BF, txt_Save.Text + "CAB-" + item.Key); }
                    }
                    else { SetToolStripLabelText("輸出: " + item.Key, lab_Execute); Static_Function.WriteBundleFile(BF, txt_Save.Text + "CAB-" + item.Key); }
                    BF = null;
                    item.Value.Dispose();
                    SetProgressBarValue(PB_TotalFile.Value + 1, PB_TotalFile);
                    tbManager.SetProgressValue(PB_TotalFile.Value, ExtFile.Count);
                    SetToolStripLabelText("已解包數: " + PB_TotalFile.Value, lab_DownItem);
                });
                ExtFile.Clear();
                GC.Collect();
            }
            SW.Stop();
            timer1.Stop();
            SetLabelText("下載檔案:無", lab_Download);
            SetToolStripLabelText("等待中", lab_Status);
            Work_Event(false);
            tbManager.SetProgressState(TaskbarProgressBarState.NoProgress);
            Invoke(new Action(delegate { Text = "清單檔案下載器 "; }));
            if (Program.autoClose) Environment.Exit(1);
            if (lib_Error.Items.Count <= 0) SetButtonEnable(false, btn_Save_Error);
            Environment.ExitCode = 1;
            MessageBox.Show("下載完成");
        }        

        private void Work_Event(bool Work)
        {
            SetButtonEnable(!Work, btn_Save);
            SetButtonEnable(!Work, btn_Start);
            SetButtonEnable(!Work, btn_File);
            SetButtonEnable(Work, btn_Stop);
            SetButtonEnable(!Work, btn_Save_Error);
            SetGroupBoxEnable(!Work, GB_Server);
            SetGroupBoxEnable(!Work, GB_Ver);
            SetToolStripMenuuItemEnable(!Work, TSM_UnPrefab);
            SetToolStripMenuuItemEnable(!Work, TSM_Convert);
            SetToolStripMenuuItemEnable(!Work, TSM_OnlyBundle);
            SetToolStripMenuuItemEnable(!Work, TSM_Log);
            SetToolStripTextBoxEnable(!Work);
            SetCheckBoxEnable(!Work);
            SetTextBoxReadOnly(Work && !rdb_Custom.Checked, TSM_Exclude);
            SetTextBoxReadOnly(Work, TST_DLLength);
            Working = Work;
        }
    }
}
