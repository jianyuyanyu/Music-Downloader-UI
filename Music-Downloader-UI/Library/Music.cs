using MusicDownloader.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using static MusicDownloader.Library.Tool;
using System.Security.Cryptography;
using System.Text;
using NeteaseCloudMusicApi;
using System.Threading.Tasks;

namespace MusicDownloader.Library
{
    public class Music
    {
        public List<int> version = new List<int> { 1, 5, 2 };
        public bool Beta = false;
        private readonly string UpdateJsonUrl = "https://json.nite07.com/MusicDownloader.json";
        //public string api1 = "";
        //public string api2 = "";
        public bool canJumpToBlog = true;
        /*
            我的json格式,如果更改请重写下方Update()方法
            {
            "Version": [1,3,3],
            "Cookie": "",音源1cookie
            "Zip": "",本地api下载链接
            "Cookie1": "",音源2cookie
            "ApiVer": "",本地api版本号
            "QQ": "",
            "Lastupdatetime": ""
            }
        */
        #region 
        //public string NeteaseApiUrl = "";
        public string QQApiUrl = "";
        public string cookie = ""; //可写死
        public string _cookie = "__remember_me=true; Max-Age=1296000; Expires=Thu, 3 Jun 2021 11:53:52 GMT; Path=/;;MUSIC_U=1a3646e3a37f5c386570fdcf1daf4f81a32a893cce6f12bc85b3d93c200e8e110931c3a9fbfe3df2; Max-Age=1296000; Expires=Thu, 3 Jun 2021 11:53:52 GMT; Path=/;;NMTID=00OJAkEohli8PNuQEybjdZM_D3C2rkAAAF5hHmb_A; Max-Age=315360000; Expires=Sat, 17 May 2031 11:53:52 GMT; Path=/;;__csrf=7f55f3eebda65620c5c7282b5a789849; Max-Age=1296010; Expires=Thu, 3 Jun 2021 11:54:02 GMT; Path=/;";
        #endregion

        public CloudMusicApi capi = new CloudMusicApi();
        public CloudMusicApi Downloadcapi;

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
        public bool api2avail = false;
        public bool updateend = false;
        string savepath = "";
        string filename = "";

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
            DateTime date = Convert.ToDateTime(update.Lastupdatetime);
            DateTime now = DateTime.Now;
            TimeSpan t = now - date;
            Console.WriteLine("相差时间" + t.TotalDays);
            updateend = true;
            if (t.TotalDays < 1)
            {
                api2avail = true;
            }
            Api.qq = update.QQ;
            if (update.Cookie != null)
            {
                _cookie = update.Cookie;
                if (setting.Cookie1 == "")
                {
                    cookie = update.Cookie;
                    if (!string.IsNullOrEmpty(cookie))
                    {
                        //capi = new CloudMusicApi(cookie);
                        Downloadcapi = new CloudMusicApi(cookie);
                        SetProxy();
                    }
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
                //NeteaseApiUrl = decrypt(setting.Api1);
            }
            else
            {
                //NeteaseApiUrl = decrypt(api1);
            }
            if (setting.Api2 != "")
            {
                QQApiUrl = decrypt(setting.Api2);
            }
            //else
            //{
            //    QQApiUrl = decrypt(api2);
            //}
            if (setting.Cookie1 != "")
            {
                cookie = setting.Cookie1;
            }
            else
            {
                cookie = _cookie;
            }
            if (!string.IsNullOrEmpty(cookie))
            {
                //capi = new CloudMusicApi(cookie);
                Downloadcapi = new CloudMusicApi();
            }
            SetProxy();
        }

        public void SetProxy()
        {
            if (!string.IsNullOrEmpty(setting.ProxyIP) && !string.IsNullOrEmpty(setting.ProxyPort))
            {
                try
                {
                    capi.Proxy = new WebProxy();
                    Downloadcapi.Proxy = new WebProxy();
                }
                catch { }
            }
            else
            {
                capi.Proxy = null;
                Downloadcapi.Proxy = null;
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
            //Key = Uri.EscapeDataString(Key).Replace("&", "%26");
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
                            List<MusicInfo> Mi = NeteaseSearch(key, i + 1, remainder).Result;
                            if (Mi != null)
                            {
                                searchItem.AddRange(Mi);
                            }
                        }
                        else
                        {
                            List<MusicInfo> Mi = NeteaseSearch(key, i + 1, 100).Result;
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
        async private Task<List<MusicInfo>> NeteaseSearch(string Key, int Page = 1, int limit = 100)
        {
            if (Key == null || Key == "")
            {
                return null;
            }
            string offset = ((Page - 1) * 100).ToString();
            var queries = new Dictionary<string, object>();
            queries["keywords"] = Key;
            queries["limit"] = limit.ToString();
            queries["offset"] = offset;
            var json = await capi.RequestAsync(CloudMusicApiProviders.Search, queries);
            //string url = NeteaseApiUrl + "search?keywords=" + Key + "&limit=" + limit.ToString() + "&offset=" + offset;
            //string json = GetHTML(url);
            //if (json == null || json == "")
            if (!CloudMusicApi.IsSuccess(json))
            {
                return null;
            }
            Json.SearchResultJson.Root srj = JsonConvert.DeserializeObject<Json.SearchResultJson.Root>(json.ToString());
            List<Json.MusicInfo> ret = new List<Json.MusicInfo>();
            if (json["result"]["songs"] /*srj.result.songs*/ == null)
            {
                return null;
            }
            string ids = "";
            for (int i = 0; i < srj.result.songs.Count; i++)
            {
                ids += srj.result.songs[i].id + ",";
            }
            //string _u = NeteaseApiUrl + "song/detail?ids=" + ids.Substring(0, ids.Length - 1);
            queries = new Dictionary<string, object>();
            queries["ids"] = ids.Substring(0, ids.Length - 1);
            var j = await capi.RequestAsync(CloudMusicApiProviders.SongDetail, queries);
            //string j = GetHTML(_u);

            Json.NeteaseMusicDetails.Root mdr = JsonConvert.DeserializeObject<Json.NeteaseMusicDetails.Root>(j.ToString());
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
                //queries = new Dictionary<string, object>();
                //queries["id"] = mdr.songs[i].id.ToString();
                //var _j = await capi.RequestAsync(CloudMusicApiProviders.Lyric, queries);
                Json.MusicInfo mi = new Json.MusicInfo()
                {
                    Album = mdr.songs[i].al.name,
                    Id = mdr.songs[i].id.ToString(),
                    LrcUrl = null,
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

        private async Task<string> Download()
        {
            if (downloadlist[0].Api == 1)
            {
                var queries = new Dictionary<string, object>();
                queries["id"] = downloadlist[0].Id;
                queries["br"] = downloadlist[0].Quality;
                var u = await Downloadcapi.RequestAsync(CloudMusicApiProviders.SongUrl, queries);
                //string u = NeteaseApiUrl + "song/url?id=" + downloadlist[0].Id + "&br=" + downloadlist[0].Quality;
                //??接口本身就会降音质
                //Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
                Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(u.ToString());
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
                    req.Timeout = 10000;
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
                    url = QQApiUrl + "song/url?id=" + id + "&type=320&mediaId=" + strMediaMid;
                }
                else
                {
                    url = QQApiUrl + "song/url?id=" + id + "&type=320";
                }
                string html = GetHTML(url, false);
                QQmusicdetails json = null;
                if (!string.IsNullOrEmpty(html))
                {
                    json = JsonConvert.DeserializeObject<QQmusicdetails>(html);
                }
                if (json == null)
                    return "";
                if (json.result != 100)
                    return "";
                using (WebClient wc = new WebClient())
                {
                    HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(json.data);
                    wr.Timeout = 10000;
                    try { HttpWebResponse r = (HttpWebResponse)wr.GetResponse(); } catch { json.data = null; }
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
        public async Task<string> GetMusicUrlBySettingAsync(int api, string id, string strMediaMid = "")
        {
            String Url = "";
            if (api == 1)
            {
                var queries = new Dictionary<string, object>();
                queries["id"] = id;
                queries["br"] = setting.DownloadQuality;

                var u = await Downloadcapi.RequestAsync(CloudMusicApiProviders.SongUrl, queries);
                //string u = NeteaseApiUrl + "song/url?id=" + downloadlist[0].Id + "&br=" + downloadlist[0].Quality;
                //??接口本身就会降音质
                //Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
                Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(u.ToString());
                //检测音质是否正确
                if (setting.DownloadQuality == "999000")
                {
                    if (urls.data[0].br == 320000 || urls.data[0].br == 128000)
                    {
                        //音质降低
                        if (setting.AutoLowerQuality)
                        {
                            Url = urls.data[0].url;
                        }
                        else
                        {
                            Url = null;
                        }
                    }
                    else
                    {
                        Url = urls.data[0].url;
                    }
                }
                else
                {
                    if (setting.DownloadQuality == urls.data[0].br.ToString())
                    {
                        //音质没降
                        Url = urls.data[0].url;
                    }
                    else
                    {
                        //音质降低
                        if (setting.AutoLowerQuality)
                        {
                            Url = urls.data[0].url;
                        }
                        else
                        {
                            Url = null;
                        }
                    }
                }
            }
            if (api == 2)
            {
                await Task.Run(() =>
                {
                    string url = "";
                    if (id == "0")
                    {
                        Url = "";
                    }
                    if (!string.IsNullOrEmpty(strMediaMid))
                    {
                        url = QQApiUrl + "song/url?id=" + id + "&type=" + setting.DownloadQuality.Replace("128000", "128").Replace("320000", "320").Replace("999000", "flac") + "&mediaId=" + strMediaMid;
                    }
                    else
                    {
                        url = QQApiUrl + "song/url?id=" + id + "&type=" + setting.DownloadQuality.Replace("128000", "128").Replace("320000", "320").Replace("999000", "flac");
                    }
                    using (WebClientPro wc = new WebClientPro())
                    {
                        StreamReader sr = null; ;
                        try { sr = new StreamReader(wc.OpenRead(url)); }
                        catch
                        {
                            //return e.Message;
                        }

                        string httpjson = sr.ReadToEnd();
                        QQmusicdetails json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);

                        //降音质
                        if (json.result != 100 && setting.AutoLowerQuality)
                        {
                            if (setting.DownloadQuality == "999000")
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
                            if (setting.DownloadQuality == "320000")
                            {
                                url = url.Replace("320", "128");
                                try { sr = new StreamReader(wc.OpenRead(url)); } catch { }
                                httpjson = sr.ReadToEnd();
                                json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);
                            }
                        }
                        Url = json.data;
                    }

                });
            }
            return Url;
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
        private async void _Download()
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
                await Download();
                if (downloadlist[0].Url == null)
                {
                    downloadlist[0].State = "无版权";
                    UpdateDownloadPage();
                    downloadlist.RemoveAt(0);
                    wait = false;
                    continue;
                }
                UpdateDownloadPage();
                savepath = "";
                filename = "";
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
                string singer = NameCheck(downloadlist[0].Singer);
                if (singer.Length >= 248)
                {
                    singer = "群星";
                }
                string album = NameCheck(downloadlist[0].Album);
                if (album.Length >= 248)
                {
                    album = album.Substring(0, 10);
                }

                switch (setting.SavePathStyle)
                {
                    case 0:
                        savepath = setting.SavePath;
                        break;
                    case 1:

                        savepath = setting.SavePath + "\\" + singer;
                        break;
                    case 2:

                        savepath = setting.SavePath + "\\" + singer + "\\" + album;
                        break;
                }

                if ((savepath + "\\" + filename).Length >= 260)
                {
                    string[] h = filename.Split('.');
                    filename = filename.Substring(0, 10) + "." + h[h.Length - 1];
                    if ((savepath + "\\" + filename).Length >= 260)
                    {
                        downloadlist[0].State = "路径过长";
                        UpdateDownloadPage();
                        downloadlist.RemoveAt(0);
                        wait = false;
                        continue;
                    }
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
                                    var queries = new Dictionary<string, object>();
                                    queries["id"] = downloadlist[0].Id;
                                    var json = await capi.RequestAsync(CloudMusicApiProviders.Lyric, queries);
                                    //StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                                    //string json = sr.ReadToEnd();
                                    NeteaseLrc.Root lrc = JsonConvert.DeserializeObject<NeteaseLrc.Root>(json.ToString());
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
        private async void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            string Lrc = "";
            string singername = NameCheck(downloadlist[0].Singer);
            //switch (setting.SaveNameStyle)
            //{
            //    case 0:
            //        if (downloadlist[0].Url.IndexOf("flac") != -1)
            //        {
            //            filename = NameCheck(downloadlist[0].Title) + " - " + singername + ".flac";
            //        }
            //        else
            //        {
            //            filename = NameCheck(downloadlist[0].Title) + " - " + singername + ".mp3";
            //        }

            //        break;
            //    case 1:
            //        if (downloadlist[0].Url.IndexOf("flac") != -1)
            //        {
            //            filename = singername + " - " + NameCheck(downloadlist[0].Title) + ".flac";
            //        }
            //        else
            //        {
            //            filename = singername + " - " + NameCheck(downloadlist[0].Title) + ".mp3";
            //        }

            //        break;
            //}
            //filename = filename.Replace(".mp3", ".lrc").Replace(".flac", ".lrc");

            //switch (setting.SavePathStyle)
            //{
            //    case 0:
            //        savepath = setting.SavePath;
            //        break;
            //    case 1:
            //        savepath = setting.SavePath + "\\" + singername;
            //        break;
            //    case 2:
            //        savepath = setting.SavePath + "\\" + singername + "\\" + NameCheck(downloadlist[0].Album);
            //        break;
            //}

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
                            //StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                            //string json = sr.ReadToEnd();
                            var queries = new Dictionary<string, object>();
                            queries["id"] = downloadlist[0].Id;
                            var json = await capi.RequestAsync(CloudMusicApiProviders.Lyric, queries);
                            NeteaseLrc.Root lrc = JsonConvert.DeserializeObject<NeteaseLrc.Root>(json.ToString());
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
        public async Task<List<MusicInfo>> GetMusicList(string Id, int api)
        {
            if (api == 1)
            {
                Musiclist.Root musiclistjson = new Musiclist.Root();
                try
                {
                    var queries = new Dictionary<string, object>();
                    queries["id"] = Id;
                    var j = await capi.RequestAsync(CloudMusicApiProviders.PlaylistDetail, queries);
                    //musiclistjson = JsonConvert.DeserializeObject<Musiclist.Root>(GetHTML(NeteaseApiUrl + "playlist/detail?id=" + Id));
                    musiclistjson = JsonConvert.DeserializeObject<Musiclist.Root>(j.ToString());
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
                        re.AddRange(_GetNeteaseMusicList(_ids.Substring(0, _ids.Length - 1)).Result);
                    }
                    return re;
                }
                else
                {
                    return _GetNeteaseMusicList(ids).Result;
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
        private async Task<List<MusicInfo>> _GetNeteaseMusicList(string ids)
        {
            List<Json.MusicInfo> ret = new List<Json.MusicInfo>();
            var queries = new Dictionary<string, object>();
            queries["ids"] = ids;
            string j = (await capi.RequestAsync(CloudMusicApiProviders.SongDetail, queries)).ToString();
            Json.NeteaseMusicDetails.Root mdr = JsonConvert.DeserializeObject<Json.NeteaseMusicDetails.Root>(j);
            queries = new Dictionary<string, object>();
            for (int i = 0; i < mdr.songs.Count; i++)
            {
                string singer = "";
                List<string> singerid = new List<string>();
                //string _url = "";

                for (int x = 0; x < mdr.songs[i].ar.Count; x++)
                {
                    singer += mdr.songs[i].ar[x].name + "、";
                    singerid.Add(mdr.songs[i].ar[x].id.ToString());
                }
                MusicInfo mi = new MusicInfo()
                {
                    Album = mdr.songs[i].al.name,
                    MVID = mdr.songs[i].mv.ToString(),
                    Id = mdr.songs[i].id.ToString(),
                    LrcUrl = null,
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
        public async Task<List<MusicInfo>> GetAlbum(string id, int api)
        {
            switch (api)
            {
                case 1:
                    {
                        var queries = new Dictionary<string, object>();
                        queries["id"] = id;
                        var j = await capi.RequestAsync(CloudMusicApiProviders.Album, queries);
                        List<MusicInfo> res = new List<MusicInfo>();
                        NeteaseAlbum.Root json;
                        json = JsonConvert.DeserializeObject<NeteaseAlbum.Root>(j.ToString());
                        foreach (var t in json.songs)
                        {
                            string singer = "";
                            foreach (var t1 in t.ar)
                            {
                                singer += t1.name + "、";
                            }
                            queries = new Dictionary<string, object>();
                            queries["id"] = t.id.ToString();
                            MusicInfo mi = new MusicInfo()
                            {
                                Title = t.name,
                                Album = json.album.name,
                                Id = t.id.ToString(),
                                LrcUrl = null,
                                PicUrl = t.al.picUrl + "?param=300y300",
                                Singer = singer.Substring(0, singer.Length - 1),
                                Api = 1,
                                MVID = t.mv.ToString()
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
                StreamReader sr;
                try
                {
                    sr = new StreamReader(wc.OpenRead(url));
                }
                catch
                {
                    return null;
                }
                QQTopList.Root json = JsonConvert.DeserializeObject<QQTopList.Root>(sr.ReadToEnd());
                List<MusicInfo> re = new List<MusicInfo>();
                for (int i = 0; i < json.data.list.Count; i++)
                {
                    re.Add(new MusicInfo
                    {
                        MVID = json.data.list[i].mv.vid,
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
                return "http://music.163.com/mv/?id=" + id;
                /*
                var queries = new Dictionary<string, object>();
                queries["id"] = id;
                var j = await capi.RequestAsync(CloudMusicApiProviders.MvUrl, queries);
                string pattern = "(?<=\"url\":\").+?(?=\")";
                return Regex.Match(j.ToString(), pattern).Value;*/
            }
            if (api == 2)
            {
                url = QQApiUrl + "song/mv?id=" + id;
                //https://y.qq.com/n/yqq/mv/v/s00367m6r4d.html
                /*
                    WebClientPro wc = new WebClientPro();
                    StreamReader sr = new StreamReader(wc.OpenRead(url));
                    string pattern = "(?<=\"vid\":\").+?(?=\")";
                    url = QQApiUrl + "mv/url?id=" + Regex.Match(sr.ReadToEnd(), pattern).Value;
                    sr = new StreamReader(wc.OpenRead(url));
                    pattern = "(?<=http:).+?(?=\")";
                    return "http:" + Regex.Match(sr.ReadToEnd(), pattern).Value;
                */
                return "https://y.qq.com/n/yqq/mv/v/" + id + ".html";
            }
            return "";
        }

        public static string decrypt(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return s;
            }
            if (s.Substring(0, 1) == "$")
            {
                string _s = s.Substring(1);
                byte[] inputByteArray = Convert.FromBase64String(_s);
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    des.Key = ASCIIEncoding.ASCII.GetBytes("String64");
                    des.IV = ASCIIEncoding.ASCII.GetBytes("String64");
                    MemoryStream ms = new MemoryStream();
                    using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(inputByteArray, 0, inputByteArray.Length);
                        cs.FlushFinalBlock();
                        cs.Close();
                    }
                    string str = Encoding.UTF8.GetString(ms.ToArray());
                    ms.Close();
                    return str;
                }
            }
            else
            {
                return s;
            }
        }
    }
}
