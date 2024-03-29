﻿using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Timers;
using Aliyun.Api;
using Aliyun.Api.DNS.DNS20150109.Request;

namespace DDNSService
{
    public partial class DDNSService : ServiceBase
    {
        private readonly string _logPath = @"C:\DDNSLog.txt";

        /// <summary>
        ///     The frequency
        /// </summary>
        private readonly int frequency = 120000; // 默认是2分钟更新一次

        /// <summary>
        ///     The timer
        /// </summary>
        private Timer _timer;

        public DDNSService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            CheckOrChangeAnalysis(null, null); // 默认先执行一次
            var retry = int.TryParse(ConfigurationManager.AppSettings["ReTryTime"], result: out int result) ? result : frequency;
            _timer = new Timer(retry); //实例化Timer类，设置间隔时间为60000毫秒；
            _timer.Elapsed += CheckOrChangeAnalysis; //到达时间的时候执行事件；
            _timer.AutoReset = true; //设置是执行一次（false）还是一直执行(true)；
            _timer.Enabled = true; //是否执行System.Timers.Timer.Elapsed事件；
            _timer.Start();
        }

        protected override void OnStop()
        {
            _timer?.Dispose();

            _timer = null;
        }

        public void change_ipv4(StreamWriter streamWriter)
        {
            try
            {
                // 默认修改在Service App.Config 修改 也可以手动指定
                var accessKeyId = ConfigurationManager.AppSettings["AccessKeyId"]; //Access Key ID，如 DR2DPjKmg4ww0e79
                var accessKeySecret =
                    ConfigurationManager.AppSettings["AccessKeySecret"];
                ; //Access Key Secret，如 ysHnd1dhWvoOmbdWKx04evlVEdXEW7
                var domainName = ConfigurationManager.AppSettings["DomainName"]; //域名，如 google.com  
                var rr = ConfigurationManager.AppSettings["RR4"]; //子域名，如 www
                var ttl = ConfigurationManager.AppSettings["TTL"]; // TTL 时间 单位秒 免费最低600 10分钟 收费最低1秒

                var aliyunClient = new DefaultAliyunClient("http://dns.aliyuncs.com/", accessKeyId, accessKeySecret);
                var req = new DescribeDomainRecordsRequest { DomainName = domainName };
                var response = aliyunClient.Execute(req);

                // 筛选只有A类解析，且前缀为设置的前缀
                var updateRecord = response.DomainRecords.FirstOrDefault(rec => rec.RR == rr && rec.Type == "A");
                if (updateRecord == null)
                {
                    streamWriter.WriteLine(
                        $"Time:{DateTime.Now}\r\n没有该IPV4RR");
                    return;
                }

                var httpClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None
                });
                httpClient.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue(new ProductHeaderValue("aliyun-ddns-client-csharp")));
                // 设置1分钟半的超时时间，一般已经足够，如果还是不行，就不行，等下一个循环
                httpClient.Timeout = TimeSpan.FromSeconds(90);
                // 默认修改在Service App.Config 修改 也可以手动指定 获取本地外网IP的地址
                var htmlSource = httpClient.GetStringAsync(ConfigurationManager.AppSettings["Ip4Server"]).Result;

                var ip = Regex.Match(htmlSource,
                    @"((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))",
                    RegexOptions.IgnoreCase).Value;

                if (updateRecord.Value == ip)
                {
                    //streamWriter.WriteLine(
                    //    $"Time:{DateTime.Now}\r\nIp4未改变");
                    return;
                }

                if (!int.TryParse(ttl, out var ttlInt)) ttlInt = 600;

                var changeValueRequest = new UpdateDomainRecordRequest
                {
                    RecordId = updateRecord.RecordId,
                    Value = ip,
                    Type = "A",
                    RR = rr,
                    TTL = ttlInt
                };
                aliyunClient.Execute(changeValueRequest);

                streamWriter.WriteLine(
                    $"Time:{DateTime.Now}\r\n   之前 Ip4:{updateRecord.Value}\r\n   之后 Ip4:{ip}");
            }
            catch (Exception ex)
            {
                streamWriter.WriteLine(ex.ToString());
            }
        }

        public void change_ipv6(StreamWriter streamWriter)
        {
            try
            {
                // 默认修改在Service App.Config 修改 也可以手动指定
                var accessKeyId = ConfigurationManager.AppSettings["AccessKeyId"]; //Access Key ID，如 DR2DPjKmg4ww0e79
                var accessKeySecret =
                    ConfigurationManager.AppSettings["AccessKeySecret"];
                ; //Access Key Secret，如 ysHnd1dhWvoOmbdWKx04evlVEdXEW7
                var domainName = ConfigurationManager.AppSettings["DomainName"]; //域名，如 google.com  
                var rr = ConfigurationManager.AppSettings["RR6"]; //子域名，如 www
                var ttl = ConfigurationManager.AppSettings["TTL"]; // TTL 时间 单位秒 免费最低600 10分钟 收费最低1秒

                var aliyunClient = new DefaultAliyunClient("http://dns.aliyuncs.com/", accessKeyId, accessKeySecret);
                var req = new DescribeDomainRecordsRequest { DomainName = domainName };
                var response = aliyunClient.Execute(req);

                // 筛选只有A类解析，且前缀为设置的前缀
                var updateRecord = response.DomainRecords.FirstOrDefault(rec => rec.RR == rr && rec.Type == "AAAA");
                if (updateRecord == null)
                {
                    streamWriter.WriteLine(
                        $"Time:{DateTime.Now}\r\n没有该IPV6RR");
                    return;
                }

                var httpClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None
                });
                httpClient.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue(new ProductHeaderValue("aliyun-ddns-client-csharp")));
                // 设置1分钟半的超时时间，一般已经足够，如果还是不行，就不行，等下一个循环
                httpClient.Timeout = TimeSpan.FromSeconds(90);

                // 默认修改在Service App.Config 修改 也可以手动指定 获取本地外网IP的地址
                var htmlSource = httpClient.GetStringAsync(ConfigurationManager.AppSettings["Ip6Server"]).Result;

                var ip = Regex.Match(htmlSource,
                    @"(^([\da-fA-F]{1,4}:){6}((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)|::([\da−fA−F]1,4:)0,4((25[0−5]|2[0−4]\d|[01]?\d\d?)\.)3(25[0−5]|2[0−4]\d|[01]?\d\d?)|::([\da−fA−F]1,4:)0,4((25[0−5]|2[0−4]\d|[01]?\d\d?)\.)3(25[0−5]|2[0−4]\d|[01]?\d\d?)|^([\da-fA-F]{1,4}:):([\da-fA-F]{1,4}:){0,3}((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)|([\da−fA−F]1,4:)2:([\da−fA−F]1,4:)0,2((25[0−5]|2[0−4]\d|[01]?\d\d?)\.)3(25[0−5]|2[0−4]\d|[01]?\d\d?)|([\da−fA−F]1,4:)2:([\da−fA−F]1,4:)0,2((25[0−5]|2[0−4]\d|[01]?\d\d?)\.)3(25[0−5]|2[0−4]\d|[01]?\d\d?)|^([\da-fA-F]{1,4}:){3}:([\da-fA-F]{1,4}:){0,1}((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)|([\da−fA−F]1,4:)4:((25[0−5]|2[0−4]\d|[01]?\d\d?)\.)3(25[0−5]|2[0−4]\d|[01]?\d\d?)|([\da−fA−F]1,4:)4:((25[0−5]|2[0−4]\d|[01]?\d\d?)\.)3(25[0−5]|2[0−4]\d|[01]?\d\d?)|^([\da-fA-F]{1,4}:){7}[\da-fA-F]{1,4}|:((:[\da−fA−F]1,4)1,6|:)|:((:[\da−fA−F]1,4)1,6|:)|^[\da-fA-F]{1,4}:((:[\da-fA-F]{1,4}){1,5}|:)|([\da−fA−F]1,4:)2((:[\da−fA−F]1,4)1,4|:)|([\da−fA−F]1,4:)2((:[\da−fA−F]1,4)1,4|:)|^([\da-fA-F]{1,4}:){3}((:[\da-fA-F]{1,4}){1,3}|:)|([\da−fA−F]1,4:)4((:[\da−fA−F]1,4)1,2|:)|([\da−fA−F]1,4:)4((:[\da−fA−F]1,4)1,2|:)|^([\da-fA-F]{1,4}:){5}:([\da-fA-F]{1,4})?|([\da−fA−F]1,4:)6:|([\da−fA−F]1,4:)6:)",
                    RegexOptions.IgnoreCase).Value;

                if (updateRecord.Value == ip)
                {
                    //streamWriter.WriteLine(
                    //    $"Time:{DateTime.Now}\r\nIp6未改变");
                    return;
                }

                if (!int.TryParse(ttl, out var ttlInt)) ttlInt = 600;

                var changeValueRequest = new UpdateDomainRecordRequest
                {
                    RecordId = updateRecord.RecordId,
                    Value = ip,
                    Type = "AAAA",
                    RR = rr,
                    TTL = ttlInt
                };
                aliyunClient.Execute(changeValueRequest);

                streamWriter.WriteLine(
                    $"Time:{DateTime.Now}\r\n   之前 Ip6:{updateRecord.Value}\r\n   改变后 Ip6:{ip}");
            }
            catch (Exception ex)
            {
                streamWriter.WriteLine(ex.ToString());
            }
        }

        public void CheckOrChangeAnalysis(object sender, ElapsedEventArgs e)
        {

            if (!File.Exists(_logPath)) File.Create(_logPath);

            var streamWriter = new StreamWriter(_logPath, true);

            if (sender is null)
            {
                streamWriter.WriteLine(
                    $"Time:{DateTime.Now}\r\n程序开始运行");
            }

            // var configs = File.ReadAllLines("config.txt");

            change_ipv4(streamWriter);

            change_ipv6(streamWriter);

            streamWriter.Close();
        }
    }
}