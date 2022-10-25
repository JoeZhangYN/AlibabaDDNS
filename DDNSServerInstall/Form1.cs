using System;
using System.Collections;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Windows.Forms;

namespace DDNSServerInstall
{
    public partial class Form1 : Form
    {
        private readonly string _serviceFilePath = $"{Application.StartupPath}\\DDNSService.exe";
        private readonly string _serviceName = "DDNSService";

        public Form1()
        {
            InitializeComponent();
        }

        //事件：安装服务
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsServiceExisted(_serviceName)) UninstallService(_serviceFilePath);
                InstallService(_serviceFilePath);
                MessageBox.Show("安装成功");
            }
            catch
            {
                MessageBox.Show("安装失败");
            }
        }

        //事件：启动服务
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsServiceExisted(_serviceName)) ServiceStart(_serviceName);

                MessageBox.Show("启动成功");
            }
            catch
            {
                MessageBox.Show("启动失败");
            }
        }

        //事件：停止服务
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsServiceExisted(_serviceName)) ServiceStop(_serviceName);
                MessageBox.Show("停止成功");
            }
            catch
            {
                MessageBox.Show("停止失败");
            }
        }

        //事件：卸载服务
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsServiceExisted(_serviceName))
                {
                    ServiceStop(_serviceName);
                    UninstallService(_serviceFilePath);
                }
                MessageBox.Show("卸载成功");
            }
            catch
            {
                MessageBox.Show("卸载失败");
            }
        }

        //判断服务是否存在
        private bool IsServiceExisted(string serviceName)
        {
            var services = ServiceController.GetServices();
            foreach (var sc in services)
                if (sc.ServiceName.ToLower() == serviceName.ToLower())
                    return true;
            return false;
        }

        //安装服务
        private void InstallService(string serviceFilePath)
        {
            using (var installer = new AssemblyInstaller())
            {
                installer.UseNewContext = true;
                installer.Path = serviceFilePath;
                IDictionary savedState = new Hashtable();
                installer.Install(savedState);
                installer.Commit(savedState);
            }
        }

        //卸载服务
        private void UninstallService(string serviceFilePath)
        {
            using (var installer = new AssemblyInstaller())
            {
                installer.UseNewContext = true;
                installer.Path = serviceFilePath;
                installer.Uninstall(null);
            }
        }

        //启动服务
        private void ServiceStart(string serviceName)
        {
            using (var control = new ServiceController(serviceName))
            {
                if (control.Status == ServiceControllerStatus.Stopped) control.Start();
            }
        }

        //停止服务
        private void ServiceStop(string serviceName)
        {
            using (var control = new ServiceController(serviceName))
            {
                if (control.Status == ServiceControllerStatus.Running) control.Stop();
            }
        }
    }
}