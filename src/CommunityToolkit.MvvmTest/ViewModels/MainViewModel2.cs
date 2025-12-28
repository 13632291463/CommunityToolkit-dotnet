using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace WpfApp.ViewModels
{
    public partial class MainViewModel2 : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GreetingMessage))]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GreetingMessage))]
        private int _age;

        [ObservableProperty]
        private bool _isEnabled = true;

        public MainViewModel2()
        {
            Name = "张三";
            Age = 25;
        }

        // 使用RelayCommand创建命令
        [RelayCommand]
        private void IncrementAge()
        {
            Age++;
        }

        [RelayCommand]
        private void DecrementAge()
        {
            Age--;
        }

        // 带参数的命令
        [RelayCommand]
        private void UpdateName(string newName)
        {
            if (!string.IsNullOrEmpty(newName))
            {
                Name = newName;
            }
        }

        public string GreetingMessage => $"你好, {Name}! 你今年 {Age} 岁了。";

        // 当Name或Age改变时更新GreetingMessage
        //partial void OnNameChanged(string value) => OnPropertyChanged(nameof(GreetingMessage));
        //partial void OnAgeChanged(int value) => OnPropertyChanged(nameof(GreetingMessage));
    }
}