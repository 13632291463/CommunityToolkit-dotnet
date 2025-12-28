using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;

namespace WpfApp.Services
{
    public interface IDataService
    {
        Task<string> GetDataAsync();
    }

    public class MockDataService : IDataService
    {
        public async Task<string> GetDataAsync()
        {
            await Task.Delay(1000); // 模拟异步操作
            return "模拟数据";
        }
    }

    public partial class DependencyInjectionViewModel : ObservableObject
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private string _data = string.Empty;

        [ObservableProperty]
        private string _status = "就绪";

        public DependencyInjectionViewModel()
        {
            // 从 Ioc 容器获取服务
            _dataService = Ioc.Default.GetRequiredService<IDataService>();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            Status = "正在加载...";
            Data = await _dataService.GetDataAsync();
            Status = "加载完成";
        }
    }
}