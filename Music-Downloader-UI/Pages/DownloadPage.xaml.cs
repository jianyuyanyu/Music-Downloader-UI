using AduSkin.Controls.Metro;
using MusicDownloader.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicDownloader
{
    /// <summary>
    /// DownloadPage.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadPage : Page
    {
        List<ListModel> listitem = new List<ListModel>();
        Music music = null;

        class ListModel : INotifyPropertyChanged
        {
            [DisplayName("标题")]
            public string Title { get; set; }
            [DisplayName("歌手")]
            public string Singer { get; set; }
            [DisplayName("专辑")]
            public string Album { get; set; }
            [DisplayName("状态")]
            public string State { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public DownloadPage(Music m)
        {
            InitializeComponent();
            //Thread th_Update = new Thread(UpdateList);
            //th_Update.Start();
            music = m;
            music.UpdateDownloadPage += UpdateList;
        }

        public void UpdateList()
        {
            object locker = new object();
            lock (locker)
            {
                bool isadd = false;
                for (int i = 0; i < music.downloadlist.Count; i++)
                {
                    bool exist = false;
                    foreach (ListModel l in listitem)
                    {
                        if (l.Title == music.downloadlist[i].Title && l.Singer == music.downloadlist[i].Singer && l.Album == music.downloadlist[i].Album)
                        {
                            l.State = music.downloadlist[i].State;
                            l.OnPropertyChanged("State");
                            exist = true;
                        }
                    }
                    if (!exist)
                    {
                        isadd = true;
                        listitem.Add(new ListModel
                        {
                            Album = music.downloadlist[i].Album,
                            Singer = music.downloadlist[i].Singer,
                            State = music.downloadlist[i].State,
                            Title = music.downloadlist[i].Title
                        }
                        );
                    }
                }
                if (isadd)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        List.ItemsSource = listitem;
                        List.Items.Refresh();
                    }));
                }
            }
        }

        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(music.setting.SavePath);
        }

        private void Label_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            if (music.th_Download?.ThreadState == System.Threading.ThreadState.Running)
            {
                AduMessageBox.Show("请等待下载完成后再试", "提示", MessageBoxButton.OK);
                return;
            }
            for (int x = 0; x < 10; x++)
            {
                for (int i = 0; i < listitem.Count; i++)
                {
                    if (listitem[i].State == "下载完成" || listitem[i].State == "无版权" || listitem[i].State == "下载错误" || listitem[i].State == "音乐已存在" || listitem[i].State == "路径过长")
                    {
                        listitem.RemoveAt(i);
                    }
                }
            }
            List.ItemsSource = listitem;
            List.Items.Refresh();
            NoticeManager.NotifiactionShow.AddNotifiaction(new NotifiactionModel()
            {
                Title = "提示",
                Content = "已经清除列表"
            });
        }

        private void Label_PreviewMouseDown_2(object sender, MouseButtonEventArgs e)
        {
            if (music.th_Download?.ThreadState == System.Threading.ThreadState.Running)
            {
                AduMessageBox.Show("请等待下载完成后再试", "提示", MessageBoxButton.OK);
                return;
            }
            for (int x = 0; x < 10; x++)
            {
                for (int i = 0; i < listitem.Count; i++)
                {
                    if (listitem[i].State == "下载完成" || listitem[i].State == "音乐已存在")
                    {
                        listitem.RemoveAt(i);
                    }
                }
            }
            List.ItemsSource = listitem;
            List.Items.Refresh();

            NoticeManager.NotifiactionShow.AddNotifiaction(new NotifiactionModel()
            {
                Title = "提示",
                Content = "已经清除下载成功的项目"
            });
        }

        private void Label_PreviewMouseDown_3(object sender, MouseButtonEventArgs e)
        {
            if (music.th_Download?.ThreadState == System.Threading.ThreadState.Running)
            {
                AduMessageBox.Show("请等待下载完成后再试", "提示", MessageBoxButton.OK);
                return;
            }

            string path = @music.setting.SavePath + "\\" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".csv";

            if (save_list_as_csv(path))
            {
                if (File.Exists(path))
                    Process.Start(path);
                else
                {
                    NoticeManager.NotifiactionShow.AddNotifiaction(new NotifiactionModel()
                    {
                        Title = "提示",
                        Content = "没有文件下载失败"
                    });
                }
                return;
            }

            AduMessageBox.Show("导出列表失败", "错误", MessageBoxButton.OK);

        }


        private bool save_list_as_csv(string path)
        {
            //   string path = @"C:\Users\Unity\Desktop\info.csv";

            try
            {
                StringBuilder log = new StringBuilder();
                int log_counter = 0;
                for (int i = 0; i < listitem.Count; i++)
                {
                    if (listitem[i].State == "下载完成" || listitem[i].State == "音乐已存在")
                    {
                        continue;
                    }

                    string title = listitem[i].Title;
                    string singer = listitem[i].Singer;
                    string album = listitem[i].Album;

                    if (title.IndexOf(',') > -1)
                        log.Append("\"" + title + "\",");
                    else
                        log.Append(title + ",");

                    if (singer.IndexOf(',') > -1)
                        log.Append("\"" + singer + "\",");
                    else
                        log.Append(singer + ",");

                    if (album.IndexOf(',') > -1)
                        log.Append("\"" + album + "\",");
                    else
                        log.Append(album + ",");

                    log.Append(listitem[i].State + "\r\n");
                    log_counter++;
                }

                if (log_counter > 0)
                {
                    if (!File.Exists(path))
                        File.Create(path).Close();
                    StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8);
                    //StreamWriter sw = File.CreateText(path);
                    sw.Write("Title,Artist,Album,State\r\n");
                    sw.Write(log);
                    sw.Flush();
                    sw.Close();
                }

            }
            catch
            {
                return false;
            }
            return true;
        }

        private void menu_Title_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(listitem[List.SelectedIndex].Title);
            NoticeManager.NotifiactionShow.AddNotifiaction(new NotifiactionModel()
            {
                Title = "提示",
                Content = "已复制"
            });
        }

        private void menu_Singer_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(listitem[List.SelectedIndex].Singer);
            NoticeManager.NotifiactionShow.AddNotifiaction(new NotifiactionModel()
            {
                Title = "提示",
                Content = "已复制"
            });
        }

        private void menu_Album_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(listitem[List.SelectedIndex].Album);
            NoticeManager.NotifiactionShow.AddNotifiaction(new NotifiactionModel()
            {
                Title = "提示",
                Content = "已复制"
            });
        }

        private void menu_Pause_Click(object sender, RoutedEventArgs e)
        {
            if (music.pause)
            {
                menu_Pause.Header = "暂停后续";
            }
            else
            {
                menu_Pause.Header = "继续下载";
            }
            music.pause = !music.pause;
        }
    }
}
