using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace WpfApp.ViewModels
{
    public partial class MyViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name;


        public ICommand MyCommand { get; }

        public MyViewModel()
        {
            MyCommand = new RelayCommand(ExecuteCommand, CanExecuteCommand);
        }

        private void ExecuteCommand()
        {
            // 命令执行的逻辑
            Name = "Hello World!";
        }

        private bool CanExecuteCommand()
        {
            // 命令是否可以执行的逻辑
            return true;
        }

    }
}
