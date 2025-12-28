using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp.ViewModels
{
    public partial class TaskManagerViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _newTaskTitle = string.Empty;

        [ObservableProperty]
        private string _newTaskDescription = string.Empty;

        public ObservableCollection<TaskItem> Tasks { get; set; } = new();

        [RelayCommand]
        private void AddTask()
        {
            if (!string.IsNullOrWhiteSpace(NewTaskTitle))
            {
                var task = new TaskItem
                {
                    Title = NewTaskTitle,
                    Description = NewTaskDescription,
                    IsCompleted = false,
                    CreatedDate = System.DateTime.Now
                };

                Tasks.Add(task);

                // 清空输入字段
                NewTaskTitle = string.Empty;
                NewTaskDescription = string.Empty;
            }
        }

        [RelayCommand]
        private void DeleteTask(TaskItem task)
        {
            if (task != null)
            {
                Tasks.Remove(task);
            }
        }

        [RelayCommand]
        private void ToggleTask(TaskItem task)
        {
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
            }
        }

        [RelayCommand]
        private void ClearCompletedTasks()
        {
            var completedTasks = Tasks.Where(t => t.IsCompleted).ToList();
            foreach (var task in completedTasks)
            {
                Tasks.Remove(task);
            }
        }
    }

    public partial class TaskItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private bool _isCompleted;

        public System.DateTime CreatedDate { get; set; }
    }
}