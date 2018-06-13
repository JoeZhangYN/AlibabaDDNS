# AlibabaDDNSService
运用阿里云和查公网IP的接口，动态更新域名绑定。需要手动指定端口转发和防火墙设置才能直接通过公网IP访问本地服务。

主要更改的地方为：

DDNSService->app.config
    <add key="AccessKeyId" value="你的阿里云KeyID" />
    <add key="AccessKeySecret" value="你的阿里云Key密码" />
    <add key="DomainName" value="你的网站域名" />
    <add key="RR" value="前缀" />
    <add key="TTL" value="超时时间按照秒为单位（免费则改成120）" />
    
更改后直接生成DDNSServerInstall就好了

安装服务，服务启动后，


每相隔一分钟，如果对应本机外网IP更改，则自动更新对应域名解析。

本地C盘会有记录显示。
