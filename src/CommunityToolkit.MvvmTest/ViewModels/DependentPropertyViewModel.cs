using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WpfApp.ViewModels
{
    public partial class DependentPropertyViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        // FullName 依赖于 FirstName 和 LastName
        public string FullName => $"{FirstName} {LastName}".Trim();

        partial void OnFirstNameChanged(string value) => OnPropertyChanged(nameof(FullName));
        partial void OnLastNameChanged(string value) => OnPropertyChanged(nameof(FullName));

        [RelayCommand]
        private void UpdateNames()
        {
            FirstName = "张";
            LastName = "三";
        }
    }
}