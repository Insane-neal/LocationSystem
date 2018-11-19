﻿using DbModel.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchorUDPTool.Tools
{
    public class PingEx
    {
        Ping pingSender;
        PingOptions options;
        byte[] buf;
        public PingEx()
        {
            //Ping 实例对象;
             pingSender = new Ping();
            //ping选项;
             options = new PingOptions();
            options.DontFragment = true;
            string data = "ping test data";
             buf = Encoding.ASCII.GetBytes(data);
        }

        public string[] ips;

        public int count = 0;

        BackgroundWorker worker;

        private void InitWorker()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerAsync();
        }

        public void Ping(string ip,int count=1)
        {
            ips = new string[] { ip };
            this.count = count;
            InitWorker();
        }

        public void PingRange(string ipStart, string ipEnd, int count = 1)
        {
            ips = IpHelper.GetIPS(ipStart, ipEnd).ToArray();
            this.count = count;
            InitWorker();
        }

        public void PingRange(string ip, int count = 1)
        {
            ips = IpHelper.GetIPS(ip).ToArray();
            this.count = count;
            InitWorker();
        }

        public string AllResult = "";

        public int SuccessIpCount = 0;

        public int FailIpCount = 0;

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int allCount = ips.Length * count;
            for (int i1 = 0; i1 < ips.Length; i1++)
            {
                string ip = ips[i1];
                if (string.IsNullOrEmpty(ip)) continue;
                result = "";
                int successCount = 0;
                for (int i = 0; i < count; i++)
                {
                    string line = "";
                    //调用同步send方法发送数据，结果存入reply对象;
                    PingReply reply = pingSender.Send(ip, 120, buf, options);
                    line += string.Format("[{0}][{1}] ", ip, i);
                    if (reply.Status == IPStatus.Success)
                    {
                        line += ("主机地址::" + reply.Address) + "\t";
                        line += ("往返时间::" + reply.RoundtripTime) + "\t";
                        line += ("生存时间TTL::" + reply.Options.Ttl) + "\t";
                        line += ("缓冲区大小::" + reply.Buffer.Length) + "\t";
                        line += ("数据包是否分段::" + reply.Options.DontFragment) + "";
                        successCount++;
                    }
                    else
                    {
                        line += "失败!";
                    }

                    result += line + "\n";
                    int percent = (int)((i1 * count + i + 1.0) / allCount * 100);
                    PingResult pr = new PingResult();
                    pr.Type = 0;
                    pr.Line = DateTime.Now.ToString("HH:mm:ss.fff")+"|"+line;
                    pr.Ip = ip;
                    worker.ReportProgress(percent, pr);
                    Thread.Sleep(100);
                }

                string t = "";
                bool s = successCount > 0;
                if (s)
                {
                    SuccessIpCount++;
                    t = string.Format("成功 [{0}]", SuccessIpCount);
                }
                else
                {
                    FailIpCount++;
                    t = string.Format("失败！ [{0}]", FailIpCount);
                }

                PingResult r = new PingResult();
                r.Type = 1;
                r.ResultText = DateTime.Now.ToString("HH:mm:ss.fff") + "|" +string.Format("{0}:{1}", ip, t);
                r.Ip = ip;
                worker.ReportProgress(0, r);
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            PingResult pr = e.UserState as PingResult;
            if (ProgressChanged != null)
            {
                ProgressChanged(e.ProgressPercentage, pr);
            }
        }

        public event Action<int, PingResult> ProgressChanged;

        string result = "";

    }


    public class PingResult
    {
        public int Type = 0;
        public string Ip;
        public string Line;
        public string ResultText;
        public bool Result;
    }
}
