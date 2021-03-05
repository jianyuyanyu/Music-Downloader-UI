using AduSkin.Controls.Metro;
using Microsoft.Win32;
using MusicDownloader.Json;
using MusicDownloader.Library;
using MusicDownloader.Pages;
using Panuon.UI.Silver;
using Panuon.UI.Silver.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace MusicDownloader
{
    public partial class MainWindow : Window
    {
        private readonly Music music = null;
        private readonly Setting setting;
        private readonly List<DownloadList> downloadlist = new List<DownloadList>();
        private readonly Page HomePage;
        private readonly Page DownloadPage;
        private readonly Page SettingPage;
        private readonly Page Donate = new Donate();
        private readonly System.Windows.Forms.NotifyIcon notifyicon = new System.Windows.Forms.NotifyIcon();
        static public string ApiUpdateInfo;
        //BG.ImageSource = new BitmapImage(new Uri(@"C:\Users\10240\Desktop\Background3.jpg"));

        #region 界面
        private void BlogButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.nitian1207.cn/");
        }

        //private void LeftMenu_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    if (frame != null)
        //    {
        //        switch (((System.Windows.Controls.HeaderedItemsControl)e.NewValue).Header)
        //        {
        //            case "主页":
        //                frame.Content = HomePage;
        //                break;
        //            case "下载":
        //                frame.Content = DownloadPage;
        //                break;
        //            case "设置":
        //                frame.Content = SettingPage;
        //                break;
        //            case "赞助":
        //                frame.Content = Donate;
        //                break;
        //            case "反馈":
        //                Process.Start("https://docs.qq.com/form/edit/DT0RraHhRZXRmYlVY");
        //                break;
        //            case "开源":
        //                Process.Start("https://github.com/NiTian1207/Music-Downloader-UI");
        //                break;
        //            case "帮助":
        //                Process.Start("https://www.nitian1207.cn/archives/663");
        //                break;
        //        }
        //    }
        //}
        #endregion

        #region 事件
        private void NotifyUpdate()
        {
            AduMessageBox.Show("检测到新版,请到Github或Telegram更新", "提示");
            Environment.Exit(0);
        }

        private void NotifyError()
        {
            VerTextblock.Text += "(Error)";
            //var result = AduMessageBox.Show("连接更新服务器错误", "提示", Application.Current.MainWindow, MessageBoxButton.OK, new AduMessageBoxConfigurations()
            //{
            //    MessageBoxIcon = MessageBoxIcon.Error
            //});
            //Environment.Exit(0);
        }
        #endregion

        public MainWindow()
        {
            Api.GetPort();
            MusicDownloader.Pages.SettingPage.ChangeBlurEvent += BlurChange;
            MusicDownloader.Pages.SettingPage.SaveBlurEvent += BlurSave;
            MusicDownloader.Pages.SettingPage.EnableLoacApiEvent += EnableLoaclApi;
            Api.NotifyNpmEventHandle += NpmNotExist;
            Api.NotifyZipEventHandle += Api_NotifyZipEventHandle;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            setting = new Setting()
            {
                SavePath = Tool.Config.Read("SavePath") ?? Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                DownloadQuality = Tool.Config.Read("DownloadQuality") ?? "999000",
                IfDownloadLrc = bool.Parse(Tool.Config.Read("IfDownloadLrc") ?? "true"),
                IfDownloadPic = bool.Parse(Tool.Config.Read("IfDownloadPic") ?? "true"),
                SaveNameStyle = int.Parse(Tool.Config.Read("SaveNameStyle") ?? "0"),
                SavePathStyle = int.Parse(Tool.Config.Read("SavePathStyle") ?? "0"),
                SearchQuantity = Tool.Config.Read("SearchQuantity") ?? "100",
                IfSearchResultFilter = bool.Parse(Tool.Config.Read("IfSearchResultFilter") ?? "true"),
                SearchResultFilter = Tool.Config.Read("SearchResultFilter") ?? "",
                TranslateLrc = int.Parse(Tool.Config.Read("TranslateLrc") ?? "0"),
                Api1 = Tool.Config.Read("Source1") ?? "",
                Api2 = Tool.Config.Read("Source2") ?? "",
                Cookie1 = Tool.Config.Read("Cookie1") ?? "",
                AutoLowerQuality = bool.Parse(Tool.Config.Read("AutoLowerQuality") ?? "true"),
                EnableLoacApi = bool.Parse(Tool.Config.Read("EnableLoacApi") ?? "false")
            };
            music = new Music(setting);
            HomePage = new SearchPage(music, setting);
            DownloadPage = new DownloadPage(music);
            SettingPage = new SettingPage(setting, music);
            InitializeComponent();
            frame.Content = HomePage;
            string ver = "";
            foreach (int s in music.version)
            {
                ver += s.ToString() + ".";
            }
            VerTextblock.Text = ver.Substring(0, ver.Length - 1);
            if (music.Beta)
            {
                VerTextblock.Text += "(Beta)";
            }
            if (Tool.Config.Read("H") != null)
            {
                Height = int.Parse(Tool.Config.Read("H"));
                Width = int.Parse(Tool.Config.Read("W"));
            }
            if (!string.IsNullOrEmpty(Tool.Config.Read("Background")) && File.Exists(Tool.Config.Read("Background")))
            {
                BG.Source = new BitmapImage(new Uri(Tool.Config.Read("Background")));
            }
            if (!string.IsNullOrEmpty(Tool.Config.Read("Blur")))
            {
                Blur.Radius = double.Parse(Tool.Config.Read("Blur"));
            }
        }

        private void Api_NotifyZipEventHandle()
        {
            setting.EnableLoacApi = false;
            Dispatcher.Invoke(new Action(() =>
            {
                AduMessageBox.Show("本地API文件下载失败", "提示");
            }));
        }

        /// <summary>
        /// 处理未知异常 保存日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            SaveLog(e);
        }

        public static void SaveLog(UnhandledExceptionEventArgs e)
        {
            StreamWriter sw = null;
            if (File.Exists("Error.log"))
            {
                sw = File.AppendText("Error.log");
            }
            else
            {
                sw = new StreamWriter("Error.log");

            }
            sw.WriteLine($"--- {DateTime.Now.ToString("G")} ---");
            sw.WriteLine(e.ExceptionObject.ToString());
            sw.Flush();
            sw.Close();
            MessageBox.Show("遇到未知错误，具体信息查看 " + Environment.CurrentDirectory + "\\Error.log");
        }

        public static void SaveLog(Exception e)
        {
            StreamWriter sw = null;
            if (File.Exists("Error.log"))
            {
                sw = File.AppendText("Error.log");
            }
            else
            {
                sw = new StreamWriter("Error.log");
                sw.WriteLine($"--- {DateTime.Now.ToString("G")} ---");
            }
            sw.WriteLine(e.Message.ToString() + "/r/n" + e.StackTrace);
            sw.Flush();
            sw.Close();
        }

        private async void WindowX_ContentRendered(object sender, EventArgs e)
        {
            Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MusicDownloader/FirstRun.m");
            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MusicDownloader/FirstRun.m"))
            {
                if (AduMessageBox.ShowYesNo("建议阅读帮助并启用本地API", "欢迎", "是", "否") == MessageBoxResult.Yes)
                {
                    Process.Start("https://www.nitianblog.com/?p=868");
                }
                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MusicDownloader/"))
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MusicDownloader/");
                File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MusicDownloader/FirstRun.m").Close();
            }
            notifyicon.Visible = true;
            notifyicon.BalloonTipText = "Music Downloader UI";
            notifyicon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            notifyicon.MouseClick += Notifyicon_MouseClick;
            System.Windows.Forms.MenuItem menu1 = new System.Windows.Forms.MenuItem("关闭");
            menu1.Click += Menu1_Click;
            notifyicon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] { menu1 });
            string result = "";
            NoticeManager.Initialize();
            await Task.Run(() =>
            {
                result = music.Update();
            });
            if (result == "Error")
            {
                NotifyError();
            }
            if (result == "Needupdate")
            {
                NotifyUpdate();
            }
            ApiUpdateInfo = result;
            if (setting.EnableLoacApi)
            {
                EnableLoaclApi();
            }
        }

        private void Notifyicon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (Visibility == Visibility.Hidden)
                {
                    Visibility = Visibility.Visible;
                }
                else
                {
                    Visibility = Visibility.Hidden;
                }
            }
        }

        private void Menu1_Click(object sender, EventArgs e)
        {
            Api.StopApi();
            Environment.Exit(0);
        }

        private void WindowX_Closed(object sender, EventArgs e)
        {
            Tool.Config.Write("H", ((int)Height).ToString());
            Tool.Config.Write("W", ((int)Width).ToString());
            notifyicon.Dispose();
            NoticeManager.ExitNotifiaction();
            Api.StopApi();
            Application.Current.Shutdown();
        }

        private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(Tool.Config.Read("Close")))
            {
                switch (int.Parse(Tool.Config.Read("Close")))
                {
                    case 0:
                        MessageBoxResult result = AduMessageBox.ShowYesNo("                    确定关闭程序?", "提示", "退出", "最小化");
                        if (result == MessageBoxResult.No)
                        {
                            e.Cancel = true;
                            Visibility = Visibility.Hidden;
                        }
                        if (result == MessageBoxResult.Yes)
                        {
                            notifyicon.Dispose();
                            Application.Current.Shutdown();
                        }
                        break;
                    case 1:
                        e.Cancel = true;
                        Visibility = Visibility.Hidden;
                        break;
                    case 2:
                        notifyicon.Dispose();
                        Application.Current.Shutdown();
                        break;

                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            DropShadowEffect de = null;
            if (WindowState == WindowState.Normal)
            {
                de = new DropShadowEffect();
                BorderThickness = new Thickness(20);
                de.BlurRadius = 20;
                de.Opacity = 0.15;
                de.ShadowDepth = 0;
                Effect = de;
            }
            else
            {
                BorderThickness = new Thickness(5);
                Effect = null;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try { DragMove(); } catch { }
        }

        private void Home_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((bool)Home.IsChecked)
                {
                    frame.Content = HomePage;
                }
            }
            catch { }
        }

        private void Download_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)Download.IsChecked)
            {
                frame.Content = DownloadPage;
            }
        }

        private void Setting_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)Setting.IsChecked)
            {
                frame.Content = SettingPage;
            }
        }

        private void Donat_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)Donat.IsChecked)
            {
                frame.Content = Donate;
            }
        }

        private void Feedback_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)Feedback.IsChecked)
            {
                Process.Start("https://tx.me/NiTian1207Home");
                Home.IsChecked = true;
            }
        }

        private void Code_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)Code.IsChecked)
            {
                Process.Start("https://github.com/NiTian1207/Music-Downloader-UI");
                Home.IsChecked = true;
            }
        }

        private void Help_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)Help.IsChecked)
            {
                if (music.canJumpToBlog)
                {
                    Process.Start("https://www.nitianblog.com/?p=868");
                    Home.IsChecked = true;
                }
                else
                {
                    AduMessageBox.Show("因帮助页面为博客链接，为避广告嫌疑，52Pojie版暂无帮助页面，请见谅", "提示");
                }
            }
        }

        private void Skin_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "图片文件(*.jpg,*.bmp,*.png)|*.jpg;*.bmp;*.png"
            };
            ofd.ShowDialog();
            string path = ofd.FileName;
            if (path != "")
            {
                BG.Source = new BitmapImage(new Uri(path));
                Tool.Config.Write("Background", path);
            }
        }

        private void NT_Click(object sender, RoutedEventArgs e)
        {
            if (music.canJumpToBlog)
            {
                Process.Start("https://www.nitianblog.com");
            }
        }

        private void BG_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        public void BlurChange(double value)
        {
            Blur.Radius = value;
        }

        public void BlurSave(double value)
        {
            Tool.Config.Write("Blur", value.ToString());
        }

        public async void EnableLoaclApi()
        {
            if (ApiUpdateInfo == "ApiUpdate" || !Directory.Exists(Api.ApiFilePath1) || !Directory.Exists(Api.ApiFilePath2))
            {

                IPendingHandler pb = PendingBox.Show("初始化信息接口中...", null, false, Application.Current.MainWindow, new PendingBoxConfigurations()
                {
                    MinHeight = 110,
                    MaxHeight = 110,
                    MinWidth = 280,
                    MaxWidth = 280
                });

                await Task.Run(() =>
                {
                    bool r = Api.ApiStart(music.apiver, music.zipurl);
                    if (!r)
                    {
                        Dispatcher.Invoke(new Action(() => { setting.EnableLoacApi = false; pb.Close(); AduMessageBox.Show("服务启动错误", "提示"); }));
                    }
                    while (!Api.ok)
                    { }
                });
                if (!Api.SetCookie(music.qqcookie))
                {
                    AduMessageBox.Show("初始化错误", "提示");
                    Api.StopApi();
                    Environment.Exit(0);
                }
                pb.Close();
            }
            else
            {
                IPendingHandler pb = PendingBox.Show("启动服务中...", null, false, Application.Current.MainWindow, new PendingBoxConfigurations()
                {
                    MinHeight = 110,
                    MaxHeight = 110,
                    MinWidth = 280,
                    MaxWidth = 280
                });
                await Task.Run(() =>
                {
                    bool r = Api.ApiStart(music.apiver, music.zipurl);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        if (!r)
                        {
                            AduMessageBox.Show("服务启动错误", "提示");
                        }
                    }));

                    while (!Api.ok)
                    { }
                });
                if (!Api.SetCookie(music.qqcookie))
                {
                    AduMessageBox.Show("服务启动错误", "提示");
                    Api.StopApi();
                    Environment.Exit(0);
                }
                pb.Close();
            }
            //music.NeteaseApiUrl = "http://127.0.0.1:" + Api.port1 + "/";
            music.QQApiUrl = "http://127.0.0.1:" + Api.port2 + "/";
        }

        public void NpmNotExist()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                setting.EnableLoacApi = false; AduMessageBox.Show("npm调用失败，程序即将退出\n如果再次启动仍出现提示请删除 (*.exe.config) 文件", "提示");
            }));
        }
    }
}
