using System;
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
            var retry = int.TryParse(ConfigurationManager.AppSettings["ReTryTime"], result: out int result) ? result : 120000;
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

        public void CheckOrChangeAnalysis(object sender, ElapsedEventArgs e)
        {
            if (!File.Exists(_logPath)) File.Create(_logPath);

            var streamWriter = new StreamWriter(_logPath, true);

            // var configs = File.ReadAllLines("config.txt");
            try
            {
                // 默认修改在Service App.Config 修改 也可以手动指定
                var accessKeyId = ConfigurationManager.AppSettings["AccessKeyId"]; //Access Key ID，如 DR2DPjKmg4ww0e79
                var accessKeySecret =
                    ConfigurationManager.AppSettings["AccessKeySecret"];
                ; //Access Key Secret，如 ysHnd1dhWvoOmbdWKx04evlVEdXEW7
                var domainName = ConfigurationManager.AppSettings["DomainName"]; //域名，如 google.com  
                var rr = ConfigurationManager.AppSettings["RR"]; //子域名，如 www
                var ttl = ConfigurationManager.AppSettings["TTL"]; // TTL 时间 单位秒 免费最低120 2分钟 收费最低1秒

                var aliyunClient = new DefaultAliyunClient("http://dns.aliyuncs.com/", accessKeyId, accessKeySecret);
                var req = new DescribeDomainRecordsRequest {DomainName = domainName};
                var response = aliyunClient.Execute(req);

                // 筛选只有A类解析，且前缀为设置的前缀
                var updateRecord = response.DomainRecords.FirstOrDefault(rec => rec.RR == rr && rec.Type == "A");
                if (updateRecord == null)
                    return;

                var httpClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None
                });
                httpClient.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue(new ProductHeaderValue("aliyun-ddns-client-csharp")));
                // 默认修改在Service App.Config 修改 也可以手动指定 获取本地外网IP的地址
                var htmlSource = httpClient.GetStringAsync(ConfigurationManager.AppSettings["IpServer"]).Result;

                var ip = Regex.Match(htmlSource,
                    @"((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))",
                    RegexOptions.IgnoreCase).Value;

                if (updateRecord.Value == ip) return;

                if (!int.TryParse(ttl, out var ttlInt)) ttlInt = 120;

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
                    $"Time:{DateTime.Now}\r\n   Before Ip:{updateRecord.Value}\r\n   Change Ip:{ip}");
            }
            catch (Exception ex)
            {
                streamWriter.WriteLine(ex.ToString());
            }
            finally
            {
                streamWriter.Close();

                streamWriter = null;
            }
        }
    }
}