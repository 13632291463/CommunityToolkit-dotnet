using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace WpfApp.ViewModels
{
    public partial class CommandViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int _age;

        [ObservableProperty]
        private string _message = string.Empty;

        public CommandViewModel()
        {
            Name = "李四";
            Age = 30;
        }

        // 基本命令
        [RelayCommand]
        private void Save()
        {
            Message = $"保存了用户: {Name}, 年龄: {Age}";
        }

        // 带参数的命令
        [RelayCommand]
        private void UpdateAge(string newAgeString)
        {
            int newAge=int.Parse(newAgeString);
            Age = newAge;
            Message = $"年龄已更新为: {Age}";
        }

        // 带条件的命令
        [RelayCommand(CanExecute = nameof(CanReset))]
        private void Reset()
        {
            Name = string.Empty;
            Age = 0;
            Message = "已重置";
        }

        private bool CanReset()
        {
            return !string.IsNullOrEmpty(Name) || Age > 0;
        }

        // 异步命令
        [RelayCommand(CanExecute = nameof(CanSubmit), IncludeCancelCommand = true)]
        private async Task LoadDataAsync(CancellationToken token)
        {
            Message = "正在加载数据...";
            try
            {
                await Task.Delay(2000, token); // 模拟异步操作
                Message = "数据加载完成";
            }
            catch (OperationCanceledException ex)
            {
                Message = "数据加载已取消";
                return;
            }
        }

        private bool CanSubmit()
        {
            return !string.IsNullOrEmpty(Name) && Age > 0;
        }
    }
}