using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace WpfApp.ViewModels
{
    // [INotifyPropertyChanged]
    // 移除 [INotifyPropertyChanged] 特性，因为 ObservableValidator 已经实现了 INotifyPropertyChanged
    public partial class ValidationViewModel : ObservableValidator
    {
        [ObservableProperty]
        [NotifyDataErrorInfo]  // 启用数据错误通知
        [Required(ErrorMessage = "姓名是必需的")]
        [MinLength(2, ErrorMessage = "姓名至少需要2个字符")]
        [MaxLength(50, ErrorMessage = "姓名不能超过50个字符")]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(18, 100, ErrorMessage = "年龄必须在18到100之间")]
        private int _age;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "邮箱是必需的")]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        private string _email = string.Empty;

        [RelayCommand]
        private void Validate()
        {
            ValidateAllProperties();
        }

        [RelayCommand]
        private void Submit()
        {
            ValidateAllProperties();
            if (HasErrors)
            {
                // 显示错误信息
                //var errors = string.Join("\n", GetErrors().SelectMany(e =>  e.Value).Cast<string>());
                var errors = string.Join("\n", GetErrors().Select(e => e.ErrorMessage).Cast<string>());
                MessageBox.Show($"存在验证错误:\n{errors}");
            }
            else
            {
                MessageBox.Show($"提交成功: {Name}, {Age}, {Email}");
            }
        }
    }
}