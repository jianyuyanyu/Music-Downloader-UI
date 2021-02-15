using MusicDownloader.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using static MusicDownloader.Library.Tool;

namespace MusicDownloader.Library
{
    public class Music
    {
        public List<int> version = new List<int> { 1, 3, 8 };
        public bool Beta = false;
        private readonly string UpdateJsonUrl = "";
        public string api1 = "";
        public string api2 = "";
        public bool canJumpToBlog = true;
        /*
            我的json格式,如果更改请重写下方Update()方法
            {
            "Version": [1,3,3],
            "Cookie": "",
            "Zip": "",
            "Cookie1": "",
            "ApiVer": "",
            "QQ": ""
            }
        */
        #region 
        public string NeteaseApiUrl = "";
        public string QQApiUrl = "";
        public string cookie = "";
        public string _cookie = "";
        #endregion

        public Setting setting;
        public List<DownloadList> downloadlist = new List<DownloadList>();
        public Thread th_Download;
        public delegate void UpdateDownloadPageEventHandler();
        public delegate void NotifyUpdateEventHandler();
        public delegate void NotifyConnectErrorEventHandler();
        public event UpdateDownloadPageEventHandler UpdateDownloadPage;
        public string qqcookie = "";
        public string zipurl = "";
        public string apiver = "";
        private bool wait = false;
        public bool pause = false;

        /// <summary>
        /// 获取更新数据 这个方法是获取程序更新信息 二次开发请修改
        /// </summary>
        /// <returns></returns>
        public string Update()
        {
            WebClientPro wc = new WebClientPro();
            StreamReader sr = null;
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                sr = new StreamReader(wc.OpenRead(UpdateJsonUrl));
                // 读取一个在线文件判断接口状态获取网易云音乐Cookie,可以写死
            }
            catch (Exception e)
            {
                MainWindow.SaveLog(e);
                return "Error";
            }
            Update update = JsonConvert.DeserializeObject<Update>(sr.ReadToEnd());
            zipurl = update.Zip;
            qqcookie = update.Cookie1;
            apiver = update.ApiVer;
            Api.qq = update.QQ;
            if (update.Cookie != null)
            {
                _cookie = update.Cookie;
                if (setting.Cookie1 == "")
                {
                    cookie = update.Cookie;
                }
            }
            bool needupdate = true;

            if (update.Version[0] < version[0])
            {
                needupdate = false;
            }
            else if (update.Version[0] == version[0])
            {
                if (update.Version[1] < version[1])
                {
                    needupdate = false;
                }
                else if (update.Version[1] == version[1])
                {
                    if (update.Version[2] < version[2])
                    {
                        needupdate = false;
                    }
                    else if (update.Version[2] == version[2])
                    {
                        needupdate = false;
                    }
                }
            }
            if (update.Version[0] == version[0] && update.Version[1] == version[1] && update.Version[2] == version[2] && Beta)
            {
                needupdate = true;
            }
            if (needupdate)
            {
                return "Needupdate";
            }
            else
            {
                if (update.ApiVer == Api.GetApiVer().Replace("\r", "").Replace("\n", "").Replace(" ", ""))
                {
                    return "";
                }
                else
                {
                    return "ApiUpdate";
                }
            }
        }

        /// <summary>
        /// 构造函数 需要提供设置参数
        /// </summary>
        /// <param name="setting"></param>
        public Music(Setting setting)
        {
            this.setting = setting;
            if (setting.Api1 != "")
            {
                NeteaseApiUrl = setting.Api1;
            }
            else
            {
                NeteaseApiUrl = api1;
            }
            if (setting.Api2 != "")
            {
                QQApiUrl = setting.Api2;
            }
            else
            {
                QQApiUrl = api2;
            }
            if (setting.Cookie1 != "")
            {
                cookie = setting.Cookie1;
            }
            else
            {
                cookie = _cookie;
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="api">1.网易云 2.QQ</param>
        /// <returns></returns>
        public List<MusicInfo> Search(string Key, int api)
        {
            Key = Uri.EscapeDataString(Key).Replace("&", "%26");
            if (api == 1)
            {
                try
                {
                    List<MusicInfo> searchItem = new List<MusicInfo>();
                    string key = Key;
                    int quantity = int.Parse(setting.SearchQuantity);
                    int pagequantity = quantity / 100;
                    int remainder = quantity % 100;

                    if (remainder == 0)
                    {
                        remainder = 100;
                    }
                    if (pagequantity == 0)
                    {
                        pagequantity = 1;
                    }
                    for (int i = 0; i < pagequantity; i++)
                    {
                        if (i == pagequantity - 1 && pagequantity >= 1)
                        {
                            List<MusicInfo> Mi = NeteaseSearch(key, i + 1, remainder);
                            if (Mi != null)
                            {
                                searchItem.AddRange(Mi);
                            }
                        }
                        else
                        {
                            List<MusicInfo> Mi = NeteaseSearch(key, i + 1, 100);
                            if (Mi != null)
                            {
                                searchItem.AddRange(Mi);
                            }
                        }
                    }
                    return searchItem;
                }
                catch { return null; }
            }
            if (api == 2)
            {
                try
                {
                    List<MusicInfo> searchItem = new List<MusicInfo>();
                    searchItem = QQSearch(Key);
                    return searchItem;
                }
                catch { return null; }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 带cookie访问
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetHTML(string url, bool withcookie = true)
        {
            try
            {
                Console.Out.WriteLine("url=" + url);
                WebClientPro wc = new WebClientPro();
                //wc.Headers.Add(HttpRequestHeader.Cookie, cookie);
                Stream s = wc.OpenRead(url + (withcookie ? "&cookie=" + cookie : ""));
                StreamReader sr = new StreamReader(s);
                return sr.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 网易云音乐搜索歌曲
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Page"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        private List<MusicInfo> NeteaseSearch(string Key, int Page = 1, int limit = 100)
        {
            if (Key == null || Key == "")
            {
                return null;
            }
            string offset = ((Page - 1) * 100).ToString();
            string url = NeteaseApiUrl + "search?keywords=" + Key + "&limit=" + limit.ToString() + "&offset=" + offset;
            string json = GetHTML(url);
            if (json == null || json == "")
            {
                return null;
            }
            Json.SearchResultJson.Root srj = JsonConvert.DeserializeObject<Json.SearchResultJson.Root>(json);
            List<Json.MusicInfo> ret = new List<Json.MusicInfo>();
            if (srj.result.songs == null)
            {
                return null;
            }
            string ids = "";
            for (int i = 0; i < srj.result.songs.Count; i++)
            {
                ids += srj.result.songs[i].id + ",";
            }
            string _u = NeteaseApiUrl + "song/detail?ids=" + ids.Substring(0, ids.Length - 1);
            string j = GetHTML(_u);
            Json.NeteaseMusicDetails.Root mdr = JsonConvert.DeserializeObject<Json.NeteaseMusicDetails.Root>(j);
            for (int i = 0; i < mdr.songs.Count; i++)
            {
                string singer = "";
                for (int x = 0; x < mdr.songs[i].ar.Count; x++)
                {
                    singer += mdr.songs[i].ar[x].name + "、";
                    //singerid.Add(mdr.songs[i].ar[x].id.ToString());
                }
                if (singer.Length > 100)
                {
                    singer = "群星.";
                }
                Json.MusicInfo mi = new Json.MusicInfo()
                {
                    Album = mdr.songs[i].al.name,
                    Id = mdr.songs[i].id.ToString(),
                    LrcUrl = NeteaseApiUrl + "lyric?id=" + mdr.songs[i].id.ToString(),
                    PicUrl = mdr.songs[i].al.picUrl + "?param=300y300",
                    Singer = singer.Substring(0, singer.Length - 1),
                    Title = mdr.songs[i].name,
                    Api = 1,
                    MVID = mdr.songs[i].mv.ToString(),
                    AlbumUrl = "https://music.163.com/#/album?id=" + mdr.songs[i].al.id.ToString()
                };
                ret.Add(mi);
            }
            return ret;
        }

        /// <summary>
        /// QQ音乐搜索歌曲
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        private List<MusicInfo> QQSearch(string Key)
        {
            List<MusicInfo> res = new List<MusicInfo>();
            //http://c.y.qq.com/soso/fcgi-bin/client_search_cp?format=json&w={key}&cr=1&g_tk=5381
            string url = "";

            int pages = int.Parse(setting.SearchQuantity) / 60;
            int m = int.Parse(setting.SearchQuantity) % 60;
            if (m != 0)
            {
                pages++;
            }

            for (int x = 1; x <= pages; x++)
            {
                if (x != pages)
                {
                    url = $"http://c.y.qq.com/soso/fcgi-bin/client_search_cp?format=json&w={Key}&cr=1&g_tk=5381&n=60&p={x}";
                }
                else
                {
                    url = $"http://c.y.qq.com/soso/fcgi-bin/client_search_cp?format=json&w={Key}&cr=1&g_tk=5381&n={m}&p={x}";
                }
                string resjson = "";
                using (WebClientPro wc = new WebClientPro())
                {
                    Console.WriteLine(url);
                    StreamReader sr = new StreamReader(wc.OpenRead(url));
                    resjson = sr.ReadToEnd();
                }
                QQMusicDetails.Root json = JsonConvert.DeserializeObject<QQMusicDetails.Root>(resjson);
                for (int i = 0; i < json.data.song.list.Count; i++)
                {
                    string singers = "";
                    foreach (QQMusicDetails.singer singer in json.data.song.list[i].singer)
                    {
                        singers += singer.name + "、";
                    }
                    if (singers.Length > 100)
                    {
                        singers = "群星.";
                    }
                    singers = singers.Substring(0, singers.Length - 1);
                    res.Add(
                        new MusicInfo
                        {
                            Album = json.data.song.list[i].albumname,
                            Id = json.data.song.list[i].songmid,
                            Title = json.data.song.list[i].songname,
                            LrcUrl = QQApiUrl + "lyric?songmid=" + json.data.song.list[i].songmid,
                            PicUrl = "https://y.gtimg.cn/music/photo_new/T002R500x500M000" + json.data.song.list[i].albummid + ".jpg",
                            Singer = singers,
                            Api = 2,
                            strMediaMid = json.data.song.list[i].strMediaMid,
                            MVID = json.data.song.list[i].songid.ToString(),
                            AlbumUrl = "https://y.qq.com/n/yqq/album/" + json.data.song.list[i].albummid.ToString() + ".html"
                        });
                }
            }
            return res;
        }

        public string AddToDownloadList(List<DownloadList> dl)
        {
            for (int i = 0; i < dl.Count; i++)
            {
                dl[i].State = "准备下载";
            }
            downloadlist.AddRange(dl);
            UpdateDownloadPage();
            if (th_Download == null || th_Download?.ThreadState == System.Threading.ThreadState.Stopped)
            {
                th_Download = new Thread(_Download);
                th_Download.Start();
            }
            return "";
        }

        private string Download()
        {
            if (downloadlist[0].Api == 1)
            {
                string u = NeteaseApiUrl + "song/url?id=" + downloadlist[0].Id + "&br=" + downloadlist[0].Quality;
                //??接口本身就会降音质
                Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
                //检测音质是否正确
                if (downloadlist[0].Quality == "999000")
                {
                    if (urls.data[0].br == 320000 || urls.data[0].br == 128000)
                    {
                        //音质降低
                        if (setting.AutoLowerQuality)
                        {
                            downloadlist[0].Url = urls.data[0].url;
                        }
                        else
                        {
                            downloadlist[0].Url = null;
                        }
                    }
                    else
                    {
                        downloadlist[0].Url = urls.data[0].url;
                    }
                }
                else
                {
                    if (downloadlist[0].Quality == urls.data[0].br.ToString())
                    {
                        //音质没降
                        downloadlist[0].Url = urls.data[0].url;
                    }
                    else
                    {
                        //音质降低
                        if (setting.AutoLowerQuality)
                        {
                            downloadlist[0].Url = urls.data[0].url;
                        }
                        else
                        {
                            downloadlist[0].Url = null;
                        }
                    }
                }
                downloadlist[0].State = "准备下载";
            }
            if (downloadlist[0].Api == 2)
            {
                string url = "";
                if (downloadlist[0].Id == "0")
                {
                    downloadlist[0].State = "无版权";
                }

                if (!string.IsNullOrEmpty(downloadlist[0].strMediaMid))
                {
                    url = QQApiUrl + "song/url?id=" + downloadlist[0].Id + "&type=" + downloadlist[0].Quality.Replace("128000", "128").Replace("320000", "320").Replace("999000", "flac") + "&mediaId=" + downloadlist[0].strMediaMid;
                }
                else
                {
                    url = QQApiUrl + "song/url?id=" + downloadlist[0].Id + "&type=" + downloadlist[0].Quality.Replace("128000", "128").Replace("320000", "320").Replace("999000", "flac");
                }
                using (WebClientPro wc = new WebClientPro())
                {
                    StreamReader sr = null; ;
                    try { sr = new StreamReader(wc.OpenRead(url)); }
                    catch (Exception e)
                    {
                        return e.Message;
                    }

                    string httpjson = sr.ReadToEnd();
                    QQmusicdetails json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);

                    //降音质
                    if (json.result != 100 && setting.AutoLowerQuality)
                    {
                        if (downloadlist[0].Quality == "999000")
                        {
                            url = url.Replace("flac", "320");
                            try { sr = new StreamReader(wc.OpenRead(url)); } catch { }
                            httpjson = sr.ReadToEnd();
                            json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);
                            if (json.result != 100)
                            {
                                url = url.Replace("320", "128");
                                try { sr = new StreamReader(wc.OpenRead(url)); } catch { }
                                httpjson = sr.ReadToEnd();
                                json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);
                            }
                        }
                        if (downloadlist[0].Quality == "320000")
                        {
                            url = url.Replace("320", "128");
                            try { sr = new StreamReader(wc.OpenRead(url)); } catch { }
                            httpjson = sr.ReadToEnd();
                            json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);
                        }
                    }
                    downloadlist[0].Url = json.data;
                    downloadlist[0].State = "准备下载";
                }
            }
            Console.WriteLine(downloadlist[0].Url);
            return "";
        }

        /// <summary>
        /// 下载方法
        /// </summary>
        /// <param name="dl"></param>
        //public string Download(List<DownloadList> dl, int api)
        //{
        //    string ids = "";
        //    if (api == 1)
        //    {
        //        int times = dl.Count / 150;
        //        int remainder = dl.Count % 150;
        //        if (remainder == 0)
        //        {
        //            remainder = 150;
        //        }
        //        else
        //        {
        //            times++;
        //        }
        //        for (int i = 0; i < times; i++)
        //        {
        //            if (i == times - 1)
        //            {
        //                ids = "";
        //                for (int x = 0; x < remainder; x++)
        //                {
        //                    ids += dl[i * 150 + x].Id + ",";
        //                }
        //                ids = ids.Substring(0, ids.Length - 1);
        //                string u = NeteaseApiUrl + "song/url?id=" + ids + "&br=" + dl[0].Quality;
        //                //??接口本身就会降音质
        //                Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
        //                for (int x = 0; x < remainder; x++)
        //                {
        //                    for (int y = 0; y < dl.Count; y++)
        //                    {
        //                        if (urls.data[x].id.ToString() == dl[y].Id)
        //                        {
        //                            //检测音质是否正确
        //                            if (dl[0].Quality == "999000")
        //                            {
        //                                if (urls.data[x].br == 320000 || urls.data[x].br == 128000)
        //                                {
        //                                    //音质降低
        //                                    if (setting.AutoLowerQuality)
        //                                    {
        //                                        dl[y].Url = urls.data[x].url;
        //                                    }
        //                                    else
        //                                    {
        //                                        dl[y].Url = null;
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    dl[y].Url = urls.data[x].url;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                if (dl[0].Quality == urls.data[x].br.ToString())
        //                                {
        //                                    //音质没降
        //                                    dl[y].Url = urls.data[x].url;
        //                                }
        //                                else
        //                                {
        //                                    //音质降低
        //                                    if (setting.AutoLowerQuality)
        //                                    {
        //                                        dl[y].Url = urls.data[x].url;
        //                                    }
        //                                    else
        //                                    {
        //                                        dl[y].Url = null;
        //                                    }
        //                                }
        //                            }
        //                            dl[y].State = "准备下载";
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                ids = "";
        //                for (int x = 0; x < 150; x++)
        //                {
        //                    ids += dl[i * 150 + x].Id + ",";
        //                }
        //                ids = ids.Substring(0, ids.Length - 1);
        //                string u = NeteaseApiUrl + "song/url?id=" + ids + "&br=" + dl[0].Quality;
        //                Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
        //                for (int x = 0; x < 150; x++)
        //                {
        //                    for (int y = 0; y < dl.Count; y++)
        //                    {
        //                        if (urls.data[x].id.ToString() == dl[y].Id)
        //                        {
        //                            dl[y].Url = urls.data[x].url;
        //                            dl[y].State = "准备下载";
        //                        }
        //                    }
        //                }
        //            }
        //            Thread.Sleep(1000);
        //        }
        //    }
        //    else if (api == 2)
        //    {
        //        for (int i = 0; i < dl.Count; i++)
        //        {
        //            string url = null;
        //            if (dl[i].Id == "0")
        //            {
        //                dl[i].State = "无版权";
        //                continue;
        //            }
        //            if (!string.IsNullOrEmpty(dl[i].strMediaMid))
        //            {
        //                url = QQApiUrl + "song/url?id=" + dl[i].Id + "&type=" + dl[i].Quality.Replace("128000", "128").Replace("320000", "320").Replace("999000", "flac") + "&mediaId=" + dl[i].strMediaMid;
        //            }
        //            else
        //            {
        //                url = QQApiUrl + "song/url?id=" + dl[i].Id + "&type=" + dl[i].Quality.Replace("128000", "128").Replace("320000", "320").Replace("999000", "flac");
        //            }
        //            using (WebClientPro wc = new WebClientPro())
        //            {
        //                StreamReader sr = null; ;
        //                try { sr = new StreamReader(wc.OpenRead(url)); }
        //                catch (Exception e)
        //                {
        //                    return e.Message;
        //                }

        //                string httpjson = sr.ReadToEnd();
        //                QQmusicdetails json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);

        //                //降音质
        //                if (json.result != 100 && setting.AutoLowerQuality)
        //                {
        //                    if (dl[i].Quality == "999000")
        //                    {
        //                        url = url.Replace("flac", "320");
        //                        sr = new StreamReader(wc.OpenRead(url));
        //                        httpjson = sr.ReadToEnd();
        //                        json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);
        //                        if (json.result != 100)
        //                        {
        //                            url = url.Replace("320", "128");
        //                            sr = new StreamReader(wc.OpenRead(url));
        //                            httpjson = sr.ReadToEnd();
        //                            json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);
        //                        }
        //                    }
        //                    if (dl[i].Quality == "320000")
        //                    {
        //                        url = url.Replace("320", "128");
        //                        sr = new StreamReader(wc.OpenRead(url));
        //                        httpjson = sr.ReadToEnd();
        //                        json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);
        //                    }
        //                }
        //                dl[i].Url = json.data;
        //                dl[i].State = "准备下载";
        //            }
        //        }
        //    }
        //    downloadlist.AddRange(dl);
        //    UpdateDownloadPage();
        //    if (th_Download == null || th_Download?.ThreadState == System.Threading.ThreadState.Stopped)
        //    {
        //        th_Download = new Thread(_Download);
        //        th_Download.Start();
        //    }
        //    return "";
        //}

        /// <summary>
        /// 获取单个音乐的播放链接
        /// </summary>
        /// <param name="api"></param>
        /// <param name="id"></param>
        /// <param name="strMediaMid"></param>
        /// <returns></returns>
        public string GetMusicUrl(int api, string id, string strMediaMid = "")
        {
            if (api == 1)
            {
                /*
                string u = NeteaseApiUrl + "song/url?id=" + id + "&br=320000";
                Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
                if (urls.data[0].url == null)
                {
                    u = NeteaseApiUrl + "song/url?id=" + id + "&br=128000";
                    urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
                }
                return urls.data[0].url;
                */
                string url = "https://music.163.com/song/media/outer/url?id=" + id + ".mp3";
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "HEAD";
                    req.AllowAutoRedirect = false;
                    HttpWebResponse myResp = (HttpWebResponse)req.GetResponse();
                    if (myResp.StatusCode == HttpStatusCode.Redirect)
                    { url = myResp.GetResponseHeader("Location"); }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception GetMusicUrl(): {0}", e);
                }
                return url ?? "";
            }
            if (api == 2)
            {
                string url = null;
                if (id == "0" || string.IsNullOrEmpty(id))
                {
                    return "";
                }
                if (!string.IsNullOrEmpty(strMediaMid))
                {
                    url = QQApiUrl + "song/url?id=" + id + "&type=flac&mediaId=" + strMediaMid;
                }
                else
                {
                    url = QQApiUrl + "song/url?id=" + id + "&type=flac";
                }
                string html = GetHTML(url, false);
                QQmusicdetails json = null;
                if (!string.IsNullOrEmpty(html))
                {
                    json = JsonConvert.DeserializeObject<QQmusicdetails>(html);
                }
                if (json.data == null)
                {
                    url = QQApiUrl + "song/url?id=" + id + "&type=128&mediaId=" + strMediaMid;
                    html = GetHTML(url, false);
                    if (!string.IsNullOrEmpty(html))
                    {
                        json = JsonConvert.DeserializeObject<QQmusicdetails>(html);
                    }
                }
                return json.data ?? "";
            }
            return "";
        }

        /// <summary>
        /// 文件名检查
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string NameCheck(string name)
        {
            string re = name.Replace("*", " ");
            re = re.Replace("\\", " ");
            re = re.Replace("\"", " ");
            re = re.Replace("<", " ");
            re = re.Replace(">", " ");
            re = re.Replace("|", " ");
            re = re.Replace("?", " ");
            re = re.Replace("/", ",");
            re = re.Replace(":", "：");
            //re = re.Replace("-", "_");
            return re;
        }

        /// <summary>
        /// 刷新下载进度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadProgressUpdate(object sender, DownloadProgressChangedEventArgs e)
        {
            downloadlist[0].State = e.ProgressPercentage.ToString() + "%";
            UpdateDownloadPage();
        }

        /// <summary>
        /// 下载线程
        /// </summary>
        private void _Download()
        {
            while (downloadlist.Count != 0)
            {
                if (wait || pause)
                {
                    continue;
                }
                if (downloadlist.Count == 0)
                {
                    continue;
                }
                downloadlist[0].State = "正在下载音乐";
                Download();
                if (downloadlist[0].Url == null)
                {
                    downloadlist[0].State = "无版权";
                    UpdateDownloadPage();
                    downloadlist.RemoveAt(0);
                    wait = false;
                    continue;
                }
                UpdateDownloadPage();
                string savepath = "";
                string filename = "";
                switch (setting.SaveNameStyle)
                {
                    case 0:
                        if (downloadlist[0].Url.IndexOf("flac") != -1)
                        {
                            filename = NameCheck(downloadlist[0].Title) + " - " + NameCheck(downloadlist[0].Singer) + ".flac";
                        }
                        else
                        {
                            filename = NameCheck(downloadlist[0].Title) + " - " + NameCheck(downloadlist[0].Singer) + ".mp3";
                        }

                        break;
                    case 1:
                        if (downloadlist[0].Url.IndexOf("flac") != -1)
                        {
                            filename = NameCheck(downloadlist[0].Singer) + " - " + NameCheck(downloadlist[0].Title) + ".flac";
                        }
                        else
                        {
                            filename = NameCheck(downloadlist[0].Singer) + " - " + NameCheck(downloadlist[0].Title) + ".mp3";
                        }

                        break;
                }
                switch (setting.SavePathStyle)
                {
                    case 0:
                        savepath = setting.SavePath;
                        break;
                    case 1:
                        savepath = setting.SavePath + "\\" + NameCheck(downloadlist[0].Singer);
                        break;
                    case 2:
                        savepath = setting.SavePath + "\\" + NameCheck(downloadlist[0].Singer) + "\\" + NameCheck(downloadlist[0].Album);
                        break;
                }
                if (!Directory.Exists(savepath))
                {
                    Directory.CreateDirectory(savepath);
                }

                if (downloadlist[0].IfDownloadMusic)
                {
                    if (System.IO.File.Exists(savepath + "\\" + filename))
                    {
                        downloadlist[0].State = "音乐已存在";
                        UpdateDownloadPage();
                        downloadlist.RemoveAt(0);
                        wait = false;
                        continue;
                    }
                    else
                    {
                        using (WebClientPro wc = new WebClientPro())
                        {
                            try
                            {
                                wc.DownloadProgressChanged += DownloadProgressUpdate;
                                wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                                wc.DownloadFileAsync(new Uri(downloadlist[0].Url), savepath + "\\" + filename);
                                downloadlist[0].IsDownloading = true;
                                wait = true;
                            }
                            catch
                            {
                                downloadlist[0].State = "音乐下载错误";
                                downloadlist.RemoveAt(0);
                                wait = false;
                                UpdateDownloadPage();
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    string Lrc = "";
                    if (downloadlist[0].IfDownloadLrc)
                    {
                        downloadlist[0].State = "正在下载歌词";
                        UpdateDownloadPage();
                        using (WebClientPro wc = new WebClientPro())
                        {
                            try
                            {
                                if (downloadlist[0].Api == 1)
                                {
                                    string savename = savepath + "\\" + filename.Replace(".flac", ".lrc").Replace(".mp3", ".lrc");
                                    StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                                    string json = sr.ReadToEnd();
                                    NeteaseLrc.Root lrc = JsonConvert.DeserializeObject<NeteaseLrc.Root>(json);
                                    if (setting.TranslateLrc == 0)
                                    {
                                        Lrc = lrc.lrc.lyric ?? "";
                                    }
                                    if (setting.TranslateLrc == 1)
                                    {
                                        Lrc = lrc.tlyric.lyric ?? lrc.lrc.lyric;
                                    }
                                    if (setting.TranslateLrc == 2)
                                    {
                                        Lrc = lrc.lrc.lyric ?? "";
                                        Lrc += lrc.tlyric.lyric ?? lrc.lrc.lyric;
                                    }

                                    if (Lrc != "")
                                    {
                                        StreamWriter sw = new StreamWriter(savename);
                                        sw.Write(Lrc);
                                        sw.Flush();
                                        sw.Close();
                                    }
                                    else
                                    {
                                        downloadlist[0].State = "歌词下载错误";
                                        UpdateDownloadPage();
                                    }
                                }
                                else if (downloadlist[0].Api == 2)
                                {
                                    string savename = savepath + "\\" + filename.Replace(".flac", ".lrc").Replace(".mp3", ".lrc");
                                    StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                                    string json = sr.ReadToEnd();
                                    QQLrc.Root lrc = JsonConvert.DeserializeObject<QQLrc.Root>(json);
                                    Lrc = lrc.data.lyric ?? "";
                                    if (Lrc != "")
                                    {
                                        StreamWriter sw = new StreamWriter(savename);
                                        sw.Write(Lrc);
                                        sw.Flush();
                                        sw.Close();
                                    }
                                    else
                                    {
                                        downloadlist[0].State = "歌词下载错误";
                                        UpdateDownloadPage();
                                    }
                                }
                            }
                            catch
                            {
                                downloadlist[0].State = "歌词下载错误";
                                UpdateDownloadPage();
                            }
                        }
                    }
                    if (downloadlist[0].IfDownloadPic)
                    {
                        downloadlist[0].State = "正在下载图片";
                        UpdateDownloadPage();
                        using (WebClientPro wc = new WebClientPro())
                        {
                            try
                            {
                                wc.DownloadFile(downloadlist[0].PicUrl, savepath + "\\" + filename.Replace(".flac", ".jpg").Replace(".mp3", ".jpg"));
                            }
                            catch
                            {
                                downloadlist[0].State = "图片下载错误";
                                UpdateDownloadPage();
                            }
                        }
                    }
                    if (File.Exists(savepath + "\\" + filename))
                    {
                        if (filename.IndexOf(".mp3") != -1)
                        {
                            using (TagLib.File tfile = TagLib.File.Create(savepath + "\\" + filename))
                            {
                                //tfile.Tag.Title = downloadlist[0].Title;
                                //tfile.Tag.Performers = new string[] { downloadlist[0].Singer };
                                //tfile.Tag.Album = downloadlist[0].Album;
                                //if (downloadlist[0].IfDownloadLrc && Lrc != "" && Lrc != null)
                                //{
                                //    tfile.Tag.Lyrics = Lrc;
                                //}
                                if (downloadlist[0].IfDownloadPic && System.IO.File.Exists(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg"))
                                {
                                    Tool.PngToJpg(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                                    TagLib.Picture pic = new TagLib.Picture
                                    {
                                        Type = TagLib.PictureType.FrontCover,
                                        MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                                        Data = TagLib.ByteVector.FromPath(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg")
                                    };
                                    tfile.Tag.Pictures = new TagLib.IPicture[] { pic };
                                }
                                tfile.Save();
                            }
                        }
                        else
                        {
                            using (TagLib.File tfile = TagLib.Flac.File.Create(savepath + "\\" + filename))
                            {
                                tfile.Tag.Title = downloadlist[0].Title;
                                tfile.Tag.Performers = new string[] { downloadlist[0].Singer };
                                tfile.Tag.Album = downloadlist[0].Album;
                                if (downloadlist[0].IfDownloadLrc && Lrc != "" && Lrc != null)
                                {
                                    tfile.Tag.Lyrics = Lrc;
                                }
                                if (downloadlist[0].IfDownloadPic && System.IO.File.Exists(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg"))
                                {
                                    Tool.PngToJpg(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                                    TagLib.Picture pic = new TagLib.Picture
                                    {
                                        Type = TagLib.PictureType.FrontCover,
                                        MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                                        Data = TagLib.ByteVector.FromPath(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg")
                                    };
                                    tfile.Tag.Pictures = new TagLib.IPicture[] { pic };
                                }
                                tfile.Save();
                            }
                        }
                    }
                    downloadlist[0].State = "下载完成";
                    UpdateDownloadPage();
                    downloadlist.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 下载完成后
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            string Lrc = "";
            string savepath = "";
            string filename = "";
            string singername = NameCheck(downloadlist[0].Singer);
            switch (setting.SaveNameStyle)
            {
                case 0:
                    if (downloadlist[0].Url.IndexOf("flac") != -1)
                    {
                        filename = NameCheck(downloadlist[0].Title) + " - " + singername + ".flac";
                    }
                    else
                    {
                        filename = NameCheck(downloadlist[0].Title) + " - " + singername + ".mp3";
                    }

                    break;
                case 1:
                    if (downloadlist[0].Url.IndexOf("flac") != -1)
                    {
                        filename = singername + " - " + NameCheck(downloadlist[0].Title) + ".flac";
                    }
                    else
                    {
                        filename = singername + " - " + NameCheck(downloadlist[0].Title) + ".mp3";
                    }

                    break;
            }


            switch (setting.SavePathStyle)
            {
                case 0:
                    savepath = setting.SavePath;
                    break;
                case 1:
                    savepath = setting.SavePath + "\\" + singername;
                    break;
                case 2:
                    savepath = setting.SavePath + "\\" + singername + "\\" + NameCheck(downloadlist[0].Album);
                    break;
            }
            if (!Directory.Exists(savepath))
            {
                Directory.CreateDirectory(savepath);
            }

            FileInfo f = new FileInfo(savepath + "\\" + filename);
            if (f.Length == 0)
            {
                downloadlist[0].State = "无版权";
                f.Delete();
                UpdateDownloadPage();
                downloadlist.RemoveAt(0);
                wait = false;
                return;
            }
            if (downloadlist[0].IfDownloadLrc)
            {
                downloadlist[0].State = "正在下载歌词";
                UpdateDownloadPage();
                using (WebClientPro wc = new WebClientPro())
                {
                    try
                    {
                        if (downloadlist[0].Api == 1)
                        {
                            string savename = savepath + "\\" + filename.Replace(".flac", ".lrc").Replace(".mp3", ".lrc");
                            StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                            string json = sr.ReadToEnd();
                            NeteaseLrc.Root lrc = JsonConvert.DeserializeObject<NeteaseLrc.Root>(json);
                            if (setting.TranslateLrc == 0)
                            {
                                Lrc = lrc.lrc.lyric ?? "";
                            }
                            if (setting.TranslateLrc == 1)
                            {
                                Lrc = lrc.tlyric.lyric ?? lrc.lrc.lyric;
                            }
                            if (setting.TranslateLrc == 2)
                            {
                                Lrc = lrc.lrc.lyric ?? "";
                                Lrc += lrc.tlyric.lyric ?? lrc.lrc.lyric;
                            }

                            if (Lrc != "")
                            {
                                StreamWriter sw = new StreamWriter(savename);
                                sw.Write(Lrc);
                                sw.Flush();
                                sw.Close();
                            }
                            else
                            {
                                downloadlist[0].State = "歌词下载错误";
                                UpdateDownloadPage();
                            }
                        }
                        else if (downloadlist[0].Api == 2)
                        {
                            string savename = savepath + "\\" + filename.Replace(".flac", ".lrc").Replace(".mp3", ".lrc");
                            StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                            string json = sr.ReadToEnd();
                            QQLrc.Root lrc = JsonConvert.DeserializeObject<QQLrc.Root>(json);
                            Lrc = lrc.data.lyric ?? "";
                            if (Lrc != "")
                            {
                                StreamWriter sw = new StreamWriter(savename);
                                sw.Write(Lrc);
                                sw.Flush();
                                sw.Close();
                            }
                            else
                            {
                                downloadlist[0].State = "歌词下载错误";
                                UpdateDownloadPage();
                            }
                        }
                    }
                    catch
                    {
                        downloadlist[0].State = "歌词下载错误";
                        UpdateDownloadPage();
                    }
                }
            }
            if (downloadlist[0].IfDownloadPic)
            {
                downloadlist[0].State = "正在下载图片";
                UpdateDownloadPage();
                using (WebClientPro wc = new WebClientPro())
                {
                    try
                    {
                        wc.DownloadFile(downloadlist[0].PicUrl, savepath + "\\" + filename.Replace(".flac", ".jpg").Replace(".mp3", ".jpg"));
                    }
                    catch
                    {
                        downloadlist[0].State = "图片下载错误";
                        UpdateDownloadPage();
                    }
                }
            }
            try
            {
                if (filename.IndexOf(".mp3") != -1)
                {
                    using (TagLib.File tfile = TagLib.File.Create(savepath + "\\" + filename))
                    {
                        tfile.Tag.Title = downloadlist[0].Title;
                        tfile.Tag.Performers = new string[] { downloadlist[0].Singer };
                        tfile.Tag.Album = downloadlist[0].Album;
                        if (downloadlist[0].IfDownloadLrc && Lrc != "" && Lrc != null)
                        {
                            tfile.Tag.Lyrics = Lrc;
                        }
                        if (downloadlist[0].IfDownloadPic && System.IO.File.Exists(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg"))
                        {
                            Tool.PngToJpg(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                            TagLib.Picture pic = new TagLib.Picture
                            {
                                Type = TagLib.PictureType.FrontCover,
                                MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                                Data = TagLib.ByteVector.FromPath(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg")
                            };
                            tfile.Tag.Pictures = new TagLib.IPicture[] { pic };
                        }
                        tfile.Save();
                    }
                }
                else
                {
                    using (TagLib.File tfile = TagLib.Flac.File.Create(savepath + "\\" + filename))
                    {
                        tfile.Tag.Title = downloadlist[0].Title;
                        tfile.Tag.Performers = new string[] { downloadlist[0].Singer };
                        tfile.Tag.Album = downloadlist[0].Album;
                        if (downloadlist[0].IfDownloadLrc && Lrc != "" && Lrc != null)
                        {
                            tfile.Tag.Lyrics = Lrc;
                        }
                        if (downloadlist[0].IfDownloadPic && System.IO.File.Exists(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg"))
                        {
                            Tool.PngToJpg(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                            TagLib.Picture pic = new TagLib.Picture
                            {
                                Type = TagLib.PictureType.FrontCover,
                                MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                                Data = TagLib.ByteVector.FromPath(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg")
                            };
                            tfile.Tag.Pictures = new TagLib.IPicture[] { pic };
                        }
                        tfile.Save();
                    }
                }
            }
            catch { }
            downloadlist[0].State = "下载完成";
            UpdateDownloadPage();
            downloadlist.RemoveAt(0);
            wait = false;
        }

        /// <summary>
        ///解析歌单，为了稳定每次请求100歌曲信息，所以解析歌单的方法分为两部分，这个方法根据歌曲数量分解请求
        /// </summary>
        public List<MusicInfo> GetMusicList(string Id, int api)
        {
            if (api == 1)
            {
                Musiclist.Root musiclistjson = new Musiclist.Root();
                try
                {
                    musiclistjson = JsonConvert.DeserializeObject<Musiclist.Root>(GetHTML(NeteaseApiUrl + "playlist/detail?id=" + Id));
                }
                catch
                {
                    return null;
                }
                string ids = "";
                for (int i = 0; i < musiclistjson.playlist.trackIds.Count; i++)
                {
                    ids += musiclistjson.playlist.trackIds[i].id.ToString() + ",";
                }
                if (ids == "")
                {
                    return null;
                }
                ids = ids.Substring(0, ids.Length - 1);

                if (musiclistjson.playlist.trackIds.Count > 100)
                {
                    string[] _id = ids.Split(',');

                    int times = musiclistjson.playlist.trackIds.Count / 100;
                    int remainder = musiclistjson.playlist.trackIds.Count % 100;
                    if (remainder == 0)
                    {
                        times--;
                        remainder = 100;
                    }
                    List<MusicInfo> re = new List<MusicInfo>();
                    for (int i = 0; i < times + 1; i++)
                    {
                        string _ids = "";
                        if (i != times)
                        {
                            for (int x = 0; x < 100; x++)
                            {
                                _ids += _id[i * 100 + x] + ",";
                            }
                        }
                        else
                        {
                            for (int x = 0; x < remainder; x++)
                            {
                                _ids += _id[i * 100 + x] + ",";
                            }
                        }
                        re.AddRange(_GetNeteaseMusicList(_ids.Substring(0, _ids.Length - 1)));
                    }
                    return re;
                }
                else
                {
                    return _GetNeteaseMusicList(ids);
                }
            }
            else if (api == 2)
            {
                string url = QQApiUrl + "songlist?id=" + Id;
                using (WebClientPro wc = new WebClientPro())
                {
                    Dictionary<string, string> headers = new Dictionary<string, string> { { "Referer", "https://y.qq.com/n/yqq/playlist" } };
                    Dictionary<string, string> vk = new Dictionary<string, string> { { "format", "json" }, { "type", "1" }, { "utf8", "1" }, { "disstid", Id }, { "loginUin", "0" } };
                    string httpres = HttpHelper.Post("http://c.y.qq.com/qzone/fcg-bin/fcg_ucc_getcdinfo_byids_cp.fcg", headers, vk);
                    if (httpres == null)
                    {
                        return null;
                    }
                    QQmusiclist.Root json = JsonConvert.DeserializeObject<QQmusiclist.Root>(httpres);
                    List<MusicInfo> re = new List<MusicInfo>();
                    if (json.cdlist[0].songlist == null)
                    {
                        return null;
                    }
                    for (int i = 0; i < json.cdlist[0].songlist.Count; i++)
                    {
                        string singers = "";
                        foreach (QQmusiclist.singer singer in json.cdlist[0].songlist[i].singer)
                        {
                            singers += singer.name + "、";
                        }
                        singers = singers.Substring(0, singers.Length - 1);
                        re.Add(new MusicInfo()
                        {
                            Album = json.cdlist[0].songlist[i].albumname,
                            Api = 2,
                            Id = json.cdlist[0].songlist[i].songmid,
                            LrcUrl = QQApiUrl + "lyric?songmid=" + json.cdlist[0].songlist[i].songmid,
                            PicUrl = "https://y.gtimg.cn/music/photo_new/T002R500x500M000" + json.cdlist[0].songlist[i].albummid + ".jpg",
                            Singer = singers,
                            strMediaMid = json.cdlist[0].songlist[i].strMediaMid,
                            Title = json.cdlist[0].songlist[i].songname,
                            AlbumUrl = "https://y.qq.com/n/yqq/album/" //+ json.data.song.list[i].albummid.ToString() + ".html"
                        }
                            );
                    }
                    return re;
                }
            }
            return null;
        }

        /// <summary>
        /// 解析网易云音乐歌单的内部方法
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        private List<MusicInfo> _GetNeteaseMusicList(string ids)
        {
            List<Json.MusicInfo> ret = new List<Json.MusicInfo>();
            string _u = NeteaseApiUrl + "song/detail?ids=" + ids;
            string j = GetHTML(_u);
            Json.NeteaseMusicDetails.Root mdr = JsonConvert.DeserializeObject<Json.NeteaseMusicDetails.Root>(j);
            string u = NeteaseApiUrl + "song/url?id=" + ids + "&br=" + setting.DownloadQuality;
            Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
            for (int i = 0; i < mdr.songs.Count; i++)
            {
                string singer = "";
                List<string> singerid = new List<string>();
                string _url = "";

                for (int x = 0; x < mdr.songs[i].ar.Count; x++)
                {
                    singer += mdr.songs[i].ar[x].name + "、";
                    singerid.Add(mdr.songs[i].ar[x].id.ToString());
                }

                for (int x = 0; x < urls.data.Count; x++)
                {
                    if (urls.data[x].id == mdr.songs[i].id)
                    {
                        _url = urls.data[x].url;
                    }
                }

                MusicInfo mi = new MusicInfo()
                {
                    Album = mdr.songs[i].al.name,
                    Id = mdr.songs[i].id.ToString(),
                    LrcUrl = NeteaseApiUrl + "lyric?id=" + mdr.songs[i].id.ToString(),
                    PicUrl = mdr.songs[i].al.picUrl + "?param=300y300",
                    Singer = singer.Substring(0, singer.Length - 1),
                    Title = mdr.songs[i].name,
                    Api = 1,
                    AlbumUrl = "https://music.163.com/#/album?id=" + mdr.songs[i].al.id.ToString()
                };
                ret.Add(mi);
            }
            return ret;
        }

        /// <summary>
        /// 解析专辑
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<MusicInfo> GetAlbum(string id, int api)
        {
            switch (api)
            {
                case 1:
                    {
                        List<MusicInfo> res = new List<MusicInfo>();
                        string url = NeteaseApiUrl + "album?id=" + id;
                        NeteaseAlbum.Root json;
                        try
                        {
                            json = JsonConvert.DeserializeObject<NeteaseAlbum.Root>(GetHTML(url));
                        }
                        catch
                        {
                            return null;
                        }
                        foreach (var t in json.songs)
                        {
                            string singer = "";
                            foreach (var t1 in t.ar)
                            {
                                singer += t1.name + "、";
                            }

                            MusicInfo mi = new MusicInfo()
                            {
                                Title = t.name,
                                Album = json.album.name,
                                Id = t.id.ToString(),
                                LrcUrl = NeteaseApiUrl + "lyric?id=" + t.id.ToString(),
                                PicUrl = t.al.picUrl + "?param=300y300",
                                Singer = singer.Substring(0, singer.Length - 1),
                                Api = 1
                            };

                            res.Add(mi);
                        }
                        return res;
                    }
                case 2:
                    {
                        string url = QQApiUrl + "album/songs?albummid=" + id;
                        using (WebClientPro wc = new WebClientPro())
                        {
                            StreamReader sr = new StreamReader(wc.OpenRead(url));
                            string httpres = sr.ReadToEnd();
                            QQAlbum.Root json = null;
                            try
                            {
                                json = JsonConvert.DeserializeObject<QQAlbum.Root>(httpres);
                            }
                            catch
                            {
                                return null;
                            }
                            List<MusicInfo> res = new List<MusicInfo>();
                            if (json.data.list == null || json.data.list.Count == 0)
                            {
                                return null;
                            }
                            for (int i = 0; i < json.data.list.Count; i++)
                            {
                                string singers = "";
                                foreach (QQAlbum.singer singer in json.data.list[i].singer)
                                {
                                    singers += singer.title + "、";
                                }
                                singers = singers.Substring(0, singers.Length - 1);
                                MusicInfo mi = new MusicInfo()
                                {
                                    Title = json.data.list[i].title,
                                    Album = json.data.list[i].album.title,
                                    Id = json.data.list[i].mid,
                                    LrcUrl = QQApiUrl + "lyric?songmid=" + json.data.list[i].mid,
                                    PicUrl = "https://y.gtimg.cn/music/photo_new/T002R500x500M000" + json.data.list[i].album.mid + ".jpg",
                                    Singer = singers,
                                    Api = 2,
                                    strMediaMid = json.data.list[i].ksong.mid
                                };
                                res.Add(mi);
                            }
                            return res;
                        }
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获取qq音乐榜单
        /// </summary>
        /// <param name="id">参考接口 /top/category </param>
        /// <returns></returns>
        public List<MusicInfo> GetQQTopList(string id)
        {
            string url = QQApiUrl + "top?id=" + id;
            using (WebClientPro wc = new WebClientPro())
            {
                StreamReader sr = new StreamReader(wc.OpenRead(url));
                QQTopList.Root json = JsonConvert.DeserializeObject<QQTopList.Root>(sr.ReadToEnd());
                List<MusicInfo> re = new List<MusicInfo>();
                for (int i = 0; i < json.data.list.Count; i++)
                {
                    re.Add(new MusicInfo
                    {
                        Album = json.data.list[i].album.title,
                        Api = 2,
                        Id = json.data.list[i].mid,
                        Singer = json.data.list[i].singerName,
                        strMediaMid = json.data.list[i].file.media_mid,
                        Title = json.data.list[i].title,
                        LrcUrl = QQApiUrl + "lyric?songmid=" + json.data.list[i].mid,
                        PicUrl = "https://y.gtimg.cn/music/photo_new/T002R500x500M000" + json.data.list[i].album.mid + ".jpg",
                        AlbumUrl = "https://y.qq.com/n/yqq/album/" + json.data.list[i].album.mid.ToString() + ".html"
                    });
                }
                return re;
            }
        }

        public string GetMvUrl(int api, string id)
        {
            string url = null;
            if (api == 1)
            {
                url = NeteaseApiUrl + "mv/url?id=" + id;
                WebClientPro wc = new WebClientPro();
                StreamReader sr = new StreamReader(wc.OpenRead(url));
                string pattern = "(?<=\"url\":\").+?(?=\")";
                return Regex.Match(sr.ReadToEnd(), pattern).Value;
            }
            if (api == 2)
            {
                url = QQApiUrl + "song/mv?id=" + id;
                WebClientPro wc = new WebClientPro();
                StreamReader sr = new StreamReader(wc.OpenRead(url));
                string pattern = "(?<=\"vid\":\").+?(?=\")";
                url = QQApiUrl + "mv/url?id=" + Regex.Match(sr.ReadToEnd(), pattern).Value;
                sr = new StreamReader(wc.OpenRead(url));
                pattern = "(?<=http:).+?(?=\")";
                return "http:" + Regex.Match(sr.ReadToEnd(), pattern).Value;
            }
            return "";
        }
    }
}
