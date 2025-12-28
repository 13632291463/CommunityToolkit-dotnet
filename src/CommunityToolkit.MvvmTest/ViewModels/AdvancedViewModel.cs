using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.Metrics;
using System.Xml.Linq;

namespace WpfApp.ViewModels
{
    public partial class AdvancedViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int _counter;

        // 部分方法 - 在属性更改前调用
        partial void OnNameChanging(string value)
        {
            System.Diagnostics.Debug.WriteLine($"Name 正在从 '{Name}' 更改为 '{value}'");
        }

        // 部分方法 - 在属性更改后调用
        partial void OnNameChanged(string value)
        {
            System.Diagnostics.Debug.WriteLine($"Name 已从更改前的值更改为 '{value}'");
        }

        partial void OnCounterChanged(int value)
        {
            if (value % 5 == 0)
            {
                System.Diagnostics.Debug.WriteLine($"Counter 达到了 5 的倍数: {value}");
            }
        }

        [RelayCommand]
        private void IncrementCounter()
        {
            Counter++;
        }
    }
}