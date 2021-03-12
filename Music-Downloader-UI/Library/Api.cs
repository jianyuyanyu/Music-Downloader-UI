using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using Panuon.UI.Silver;

namespace MusicDownloader.Library
{
    public static class Api
    {
        //public static string ApiFilePath1 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\MusicDownloader\\" + "NeteaseCloudMusicApi";
        public static string ApiFilePath2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\MusicDownloader\\" + "QQMusicApi";
        //private static Process p1 = new Process();
        private static Process p2 = new Process();
        private static string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\MusicDownloader\\";
        //public static int port1 = 3000;
        public static int port2 = 3300;
        public static bool ok = false;
        public static string qq = "";
        private static bool _isinstall = false;
        private static string _port;
        public static bool nonodejs = false;
        public delegate void NotifyNpmNotExist();
        public static event NotifyNpmNotExist NotifyNpmEventHandle;
        public delegate void NotifyZipNotExist();
        public static event NotifyZipNotExist NotifyZipEventHandle;
        private static string re_ver = null;
        private static string re_zipurl = null;
        public static bool NodejsDownloadSuc = false;

        public static bool ApiStart(string ver, string zipurl)
        {
            re_ver = ver;
            re_zipurl = zipurl;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (File.Exists(path + "Api.txt"))
            {
                StreamReader sr = new StreamReader(path + "Api.txt");
                string d = sr.ReadToEnd().Replace("\r", "").Replace("\n", "").Replace(" ", "");
                sr.Close();
                if (ver != d || /*!Directory.Exists(ApiFilePath1) ||*/ !Directory.Exists(ApiFilePath2))
                {
                    File.Delete(path + "Api.txt");
                    StreamWriter sw = new StreamWriter(path + "Api.txt");
                    sw.WriteLine(ver);
                    sw.Flush();
                    sw.Close();
                    DownloadZip(zipurl);
                }
                else
                {
                    //while (PortInUse(port1))
                    //{
                    //    port1++;
                    //}
                    while (PortInUse(port2))
                    {
                        port2++;
                    }
                    //StartApi(p1, ApiFilePath1, port1.ToString(), 1);
                    StartApi(p2, ApiFilePath2, port2.ToString(), 2);
                    ok = true;
                }
            }
            else
            {
                StreamWriter sw = new StreamWriter(path + "Api.txt");
                sw.WriteLine(ver);
                sw.Flush();
                sw.Close();
                DownloadZip(zipurl);
            }
            return true;
        }

        private static bool DownloadZip(string zipurl)
        {
            if (zipurl.IsNullOrEmpty())
            {
                if (NotifyZipEventHandle != null) NotifyZipEventHandle();
                return false;
            }

            if (Directory.Exists(path + "QQMusicApi"))
            {
                Directory.Delete(path + "QQMusicApi", true);
            }
            //if (Directory.Exists(path + "NeteaseCloudMusicApi"))
            //{
            //    Directory.Delete(path + "NeteaseCloudMusicApi", true);
            //}
            if (File.Exists(path + "INSTALL1.txt"))
            {
                File.Delete(path + "INSTALL1.txt");
            }
            if (File.Exists(path + "INSTALL2.txt"))
            {
                File.Delete(path + "INSTALL2.txt");
            }
            WebClient wc = new WebClient();
            wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
            wc.DownloadFileAsync(new Uri(zipurl), path + "api.zip");
            return true;
        }

        private static void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            ZipFile.ExtractToDirectory(path + "api.zip", path);
            File.Delete(path + "api.zip");
            //while (PortInUse(port1))
            //{
            //    port1++;
            //}
            while (PortInUse(port2))
            {
                port2++;
            }
            //StartApi(p1, ApiFilePath1, port1.ToString(), 1);
            StartApi(p2, ApiFilePath2, port2.ToString(), 2);
            ok = true;
        }

        private static void StartApi(Process p, string ApiFilePath, string port, int type)
        {
            p = new Process();
            string comm = "set PORT=" + port;
            _port = port;
            p.StartInfo.FileName = "CMD.exe";
            p.StartInfo.WorkingDirectory = ApiFilePath;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.OutputDataReceived += P_OutputDataReceived;
            p.ErrorDataReceived += P_ErrorDataReceived;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            //if (_port == port1.ToString() && !File.Exists(path + "INSTALL1.txt"))
            //{
            //    p.StandardInput.WriteLine("npm install --registry=https://registry.npm.taobao.org");
            //}

            if (_port == port2.ToString() && !File.Exists(path + "INSTALL2.txt"))
            {
                p.StandardInput.WriteLine("npm install --registry=https://registry.npm.taobao.org");
            }

            if (qq != "" && type == 2)
            {
                StreamReader sr = new StreamReader(path + "QQMusicApi\\bin\\config.js");
                string js = sr.ReadToEnd();
                sr.Close();
                if (js.IndexOf(qq) == -1)
                {
                    string data = Properties.Resources.config_js.Replace("1024028162", qq);
                    StreamWriter sw = new StreamWriter(path + "QQMusicApi\\bin\\config.js");
                    sw.Write(data);
                    sw.Flush();
                    sw.Close();
                }
            }

            comm += " & npm start";
            p.StandardInput.WriteLine(comm);
            while (!_isinstall)
            { }
            _isinstall = false;
        }

        private static void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
            //不是内部或外部命令
            if (e.Data.Replace("\r","").Replace("\n","").IndexOf("不是内部或外部命令") != -1)
            {
                NotifyNpmEventHandle();
                nonodejs = true;
                if (File.Exists(path + "INSTALL1.txt"))
                    File.Delete(path + "INSTALL1.txt");
                if (File.Exists(path + "INSTALL2.txt"))
                    File.Delete(path + "INSTALL2.txt");
                Environment.Exit(0);
            }
            if (e.Data.IndexOf("Cannot find module") != -1)
            {
                StopApi();
                if (File.Exists(path + "INSTALL1.txt"))
                    File.Delete(path + "INSTALL1.txt");
                if (File.Exists(path + "INSTALL2.txt"))
                    File.Delete(path + "INSTALL2.txt");
                ApiStart(re_ver, re_zipurl);
            }
            if (File.Exists(path + "error.log"))
            {
                StreamReader sr = new StreamReader(path + "error.log");
                string s = sr.ReadToEnd();
                sr.Close();
                StreamWriter sw = new StreamWriter(path + "error.log");
                sw.WriteLine(s + e.Data);
                sw.Flush();
                sw.Close();
            }
            else
            {
                StreamWriter sw = new StreamWriter(path + "error.log");
                sw.WriteLine(e.Data);
                sw.Flush();
                sw.Close();
            }
        }

        private static void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.IndexOf("npm start") != -1 && !nonodejs)
            {
                //if (_port == port1.ToString())
                //{
                //    StreamWriter sw = new StreamWriter(path + "INSTALL1.txt");
                //    sw.WriteLine("true");
                //    sw.Flush();
                //    sw.Close();
                //}
                //else
                //{
                    StreamWriter sw = new StreamWriter(path + "INSTALL2.txt");
                    sw.WriteLine("true");
                    sw.Flush();
                    sw.Close();
                //}
            }
            if (e.Data.IndexOf("http://127.0.0.1:" + _port) != -1 || e.Data.IndexOf("http://localhost:" + _port) != -1)
                _isinstall = true;
            Console.WriteLine(e.Data);
            try
            {
                if (File.Exists(path + "normal.log"))
                {
                    StreamReader sr = new StreamReader(path + "normal.log");
                    string s = sr.ReadToEnd();
                    sr.Close();
                    StreamWriter sw = new StreamWriter(path + "normal.log");
                    sw.WriteLine(s + e.Data);
                    sw.Flush();
                    sw.Close();
                }
                else
                {
                    StreamWriter sw = new StreamWriter(path + "normal.log");
                    sw.WriteLine(e.Data);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch { }
        }

        public static bool SetCookie(string cookies)
        {
            Encoding myEncoding = Encoding.GetEncoding("utf-8");
            cookies = "{\"data\":\"" + cookies + "\"}";
            byte[] myByte = myEncoding.GetBytes(cookies);//数据转码

            string responseResult = string.Empty;//储存结果
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("http://127.0.0.1:" + port2.ToString() + "/user/setCookie");//实例化
                req.Method = "POST";
                req.ContentType = "application/json";
                req.ContentLength = myByte.Length;
                req.GetRequestStream().Write(myByte, 0, myByte.Length);//写入请求
                HttpWebResponse myRespond = null;

                myRespond = (HttpWebResponse)req.GetResponse();//接收结果

                if (myRespond != null && myRespond.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr = new StreamReader(myRespond.GetResponseStream());
                    responseResult = sr.ReadToEnd();
                    sr.Close();
                }
                myRespond.Close();
                Console.WriteLine(responseResult);
                if (responseResult.IndexOf("操作成功") != -1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetApiVer()
        {
            if (File.Exists(path + "Api.txt"))
            {
                StreamReader sr = new StreamReader(path + "Api.txt");
                string s = sr.ReadToEnd();
                sr.Close();
                return s;
            }
            else
            {
                return "";
            }
        }

        public static bool PortInUse(int port)
        {
            bool inUse = false;

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }
            return inUse;
        }

        public static void GetPort()
        {
            //while (PortInUse(port1))
            //{
            //    port1++;
            //}
            while (PortInUse(port2))
            {
                port2++;
            }
        }

        public static void StopApi()
        {
            Process[] ps = Process.GetProcessesByName("node");
            foreach (Process _p in ps)
            {
                try { _p.Kill(); } catch { }
            }
        }

        private static string NodejsUrl = "";

        public static void DownloadNodejs()
        {
            string osVerison = Environment.OSVersion.Version.Major.ToString() + "." + Environment.OSVersion.Version.Minor.ToString();
            switch (osVerison)
            {
                case "6.2":
                case "6.3":
                case "10.0":
                    if (Environment.Is64BitOperatingSystem)
                    {
                        NodejsUrl = "https://npm.taobao.org/mirrors/node/v14.15.4/node-v14.15.4-x64.msi";
                    }
                    else
                    {
                        NodejsUrl = "https://npm.taobao.org/mirrors/node/v14.15.4/node-v14.15.4-x86.msi";
                    }
                    break;
                case "6.1":
                    if (Environment.Is64BitOperatingSystem)
                    {
                        NodejsUrl = "https://npm.taobao.org/mirrors/node/latest-v12.x/node-v12.9.1-x64.msi";
                    }
                    else
                    {
                        NodejsUrl = "https://npm.taobao.org/mirrors/node/latest-v12.x/node-v12.9.1-x86.msi";
                    }
                    break;
            }
            WebClient wc = new WebClient();
            wc.DownloadFileCompleted += Wc_DownloadFileCompleted_Nodejs;
            wc.DownloadFileAsync(new Uri(NodejsUrl), path + Path.GetFileName(NodejsUrl));
        }

        private static void Wc_DownloadFileCompleted_Nodejs(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            NodejsDownloadSuc = true;
            Process.Start(path + Path.GetFileName(NodejsUrl));
        }

        public static void Fix()
        {
            Process p = new Process();
            p.StartInfo.FileName = "CMD.exe";
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = false;
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.StandardInput.WriteLine("npm install -g npm");
        }
    }
}