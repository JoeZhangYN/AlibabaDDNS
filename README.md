# AlibabaDDNSService
运用阿里云和查公网IP的接口，动态更新域名绑定。需要手动指定端口转发和防火墙设置才能直接通过公网IP访问本地服务。

主要更改的地方为：

DDNSService->app.config

文件内有说明
安装服务，服务启动后，

每相隔2分钟，如果对应本机外网IP更改，则自动更新对应域名解析。

本地C盘会有记录显示。
