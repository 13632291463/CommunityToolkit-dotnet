using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;
using WpfApp.Services;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 配置依赖注入容器
            var services = new ServiceCollection();
            services.AddSingleton<IDataService, MockDataService>();

            // 注册到 Ioc 容器
            CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.ConfigureServices(services.BuildServiceProvider());

            base.OnStartup(e);
        }
    }

}
