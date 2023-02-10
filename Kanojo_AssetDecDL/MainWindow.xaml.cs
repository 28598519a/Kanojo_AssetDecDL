using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace Kanojo_AssetDecDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 同時下載的線程池上限
        /// </summary>
        int pool = 50;

        private async void btn_download_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = App.Root;
            openFileDialog.Filter = "hot_file_list.dat|*.dat";
            if (!openFileDialog.ShowDialog() == true)
                return;

            StreamReader reader = new StreamReader(File.OpenRead(openFileDialog.FileName));
            List<string> AssetList = new List<string>();
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line != null)
                {
                    // 取得csv中的第一個columns的資料
                    string[] values = line.Split('	');
                    AssetList.Add(values[0]);
                }
            }

            App.TotalCount = AssetList.Count;

            if (App.TotalCount > 0)
            {
                App.Respath = Path.Combine(App.Root, "Asset", "downloadTmp");
                if (!Directory.Exists(App.Respath))
                    Directory.CreateDirectory(App.Respath);

                int count = 0;
                List<Task> tasks = new List<Task>();
                foreach (string asset in AssetList)
                {
                    string url = App.ServerURL + asset;
                    string path = Path.Combine(App.Respath, asset + ".zip");

                    tasks.Add(DownLoadFile(url, path, cb_isCover.IsChecked == true ? true : false));
                    count++;

                    // 阻塞線程，等待現有工作完成再給新工作
                    if ((count % pool).Equals(0) || App.TotalCount == count)
                    {
                        // await is better than Task.Wait()
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    // 用await將線程讓給UI更新
                    lb_counter.Content = $"進度 : {count} / {App.TotalCount}";
                    await Task.Delay(1);
                }

                if (cb_Debug.IsChecked == true)
                {
                    using (StreamWriter outputFile = new StreamWriter("404.log", false))
                    {
                        foreach (string s in App.log)
                            outputFile.WriteLine(s);
                    }
                }

                App.Unzippath = Path.Combine(App.Root, "Asset", "hotRes");
                if (!Directory.Exists(App.Respath))
                    Directory.CreateDirectory(App.Respath);

                string[] fileList = Directory.GetFiles(App.Respath, "*.zip", SearchOption.TopDirectoryOnly);
                foreach (string file in fileList)
                {
                    ZipFile.ExtractToDirectory(file, App.Unzippath);
                    File.Delete(file);
                }
                // 應該剩空目錄，刪除
                Directory.Delete(App.Respath, false);

                string failmsg = String.Empty;
                if (App.TotalCount - App.glocount > 0)
                    failmsg = $"，{App.TotalCount - App.glocount}個檔案失敗";

                System.Windows.MessageBox.Show($"下載完成，共{App.glocount}個檔案{failmsg}", "Finish");
                lb_counter.Content = String.Empty;
            }
        }

        /// <summary>
        /// 從指定的網址下載檔案
        /// </summary>
        public async Task<Task> DownLoadFile(string downPath, string savePath, bool overWrite)
        {
            if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            if (File.Exists(savePath) && overWrite == false)
                return Task.FromResult(0);

            App.glocount++;

            using (WebClient wc = new WebClient())
            {
                try
                {
                    // Don't use DownloadFileTaskAsync, if 404 it will create a empty file, use DownloadDataTaskAsync instead.
                    byte[] data = await wc.DownloadDataTaskAsync(downPath);
                    File.WriteAllBytes(savePath, data);
                }
                catch (Exception ex)
                {
                    App.glocount--;

                    if (cb_Debug.IsChecked == true)
                        App.log.Add(downPath + Environment.NewLine + savePath + Environment.NewLine);

                    // 沒有的資源直接跳過，避免報錯。
                    //System.Windows.MessageBox.Show(ex.Message.ToString() + Environment.NewLine + downPath + Environment.NewLine + savePath);
                }
            }
            return Task.FromResult(0);
        }

        private void btn_decrypt_Click(object sender, RoutedEventArgs e)
        {
            byte[] XXTEA_sign = new byte[] { 0x0C, 0x07, 0x08, 0x0D, 0x0B, 0x09 };
            byte[] XXTEA_KEY = new byte[] { 0x24, 0xfa, 0x49, 0x9b, 0x10, 0x8d, 0x62, 0x59, 0x29, 0x26, 0x81, 0x67, 0x4b, 0xf7, 0x91, 0xeb };
            int count = 0;
            string selectPath = String.Empty;

            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.InitialFolder = App.Root;

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectPath = openFolderDialog.Folder;
                if (!Directory.Exists(selectPath))
                {
                    selectPath = String.Empty;
                    lb_counter.Content = "Error: 選擇的路徑不存在";
                }
            }

            string[] fileList = Directory.GetFiles(selectPath, "*", SearchOption.TopDirectoryOnly);
            foreach (string file in fileList)
            {
                byte[] data = File.ReadAllBytes(file);

                // File Sign1 check
                if (data.Length > XXTEA_sign.Length)
                {
                    byte[] tmp = new byte[XXTEA_sign.Length];
                    Array.Copy(data, tmp, XXTEA_sign.Length);

                    if (tmp.SequenceEqual(XXTEA_sign))
                    {
                        byte[] zipdata = DecryptFile(data, XXTEA_sign, XXTEA_KEY);
                        byte[] unzipdata = new byte[zipdata.Length - 1];
                        Array.Copy(zipdata, 1, unzipdata, 0, unzipdata.Length);
                        byte[] newdata = SharpZipLibDecompress(unzipdata);
                        File.WriteAllBytes(file, newdata);
                        count++;
                    }
                }
            }
            lb_counter.Content = $"已轉換 {count} 個檔案";
        }

        public XXTEAHelp mXXTEAHelp = new XXTEAHelp();

        private byte[] DecryptFile(byte[] indata, byte[] XXTEA_sign, byte[] XXTEA_KEY)
        {
            //此處需要去掉文件頭的簽名值並重新計算數據長度
            uint ret_length;
            int len = indata.Length - XXTEA_sign.Length;
            byte[] data = new byte[len];
            Buffer.BlockCopy(indata, XXTEA_sign.Length, data, 0, len);
            return mXXTEAHelp.xxtea_decrypt(data, (uint)len, XXTEA_KEY, (uint)XXTEA_KEY.Length, out ret_length);
        }

        public static byte[] SharpZipLibDecompress(byte[] data)
        {
            MemoryStream compressed = new MemoryStream(data);
            MemoryStream decompressed = new MemoryStream();
            InflaterInputStream inputStream = new InflaterInputStream(compressed);
            inputStream.CopyTo(decompressed);
            return decompressed.ToArray();
        }
    }
}
