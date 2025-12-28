using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;

namespace WpfApp.ViewModels
{
    public partial class AsyncCommandViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _status = "就绪";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private string _result = string.Empty;

        public AsyncCommandViewModel()
        {
            Status = "准备就绪，可以执行异步操作";
        }

        [RelayCommand]
        private async Task LongRunningOperationAsync()
        {
            try
            {
                IsBusy = true;
                Status = "正在执行长时间操作...";
                Progress = 0;

                // 模拟长时间运行的操作
                for (int i = 0; i <= 100; i += 10)
                {
                    Status = $"操作进度: {i}%";
                    Progress = i;
                    await Task.Delay(300); // 模拟工作
                }

                Status = "操作完成！";
                Result = "长时间操作已成功完成";
            }
            finally
            {
                IsBusy = false;
                Progress = 0;
            }
        }

        [RelayCommand]
        private async Task CancelableOperationAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;
                Status = "执行可取消操作...";
                Progress = 0;

                for (int i = 0; i <= 100; i += 5)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Status = "操作已取消";
                        Result = "用户取消了操作";
                        return;
                    }

                    Status = $"操作进度: {i}%";
                    Progress = i;
                    await Task.Delay(200, cancellationToken);
                }

                Status = "操作完成！";
                Result = "可取消操作已成功完成";
            }
            finally
            {
                IsBusy = false;
                Progress = 0;
            }
        }

        [RelayCommand]
        private async Task MultipleOperationsAsync()
        {
            try
            {
                IsBusy = true;
                Status = "执行多个异步操作...";

                // 并行执行多个异步操作
                var task1 = SimulateWorkAsync("任务1", 1500);
                var task2 = SimulateWorkAsync("任务2", 2000);
                var task3 = SimulateWorkAsync("任务3", 1000);

                var results = await Task.WhenAll(task1, task2, task3);

                Status = "所有操作完成！";
                Result = string.Join("\n", results);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<string> SimulateWorkAsync(string taskName, int delayMs)
        {
            await Task.Delay(delayMs);
            return $"{taskName} 完成于 {DateTime.Now:HH:mm:ss}";
        }
    }
}