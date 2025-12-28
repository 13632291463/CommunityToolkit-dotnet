

## WPF中使用CommunityToolkit.Mvvm的示例

### 1. 基本视图模型示例

首先，我们创建一个继承自 [ObservableObject](file://d:\Users\84574\Documents\GitHub\CommunityToolkit-dotnet\src\CommunityToolkit.Mvvm.SourceGenerators\EmbeddedResources\ObservableObject.cs#L7-L532) 的视图模型：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace WpfApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int _age;

        [ObservableProperty]
        private bool _isEnabled = true;

        public MainViewModel()
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
    }
}
```

### 2. XAML界面示例

```xml
<Window x:Class="WpfApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        Title="CommunityToolkit.Mvvm 示例" Height="300" Width="400">
    
    <Window.DataContext>
        <vm:MainViewModel />
    </Window.DataContext>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- 姓名显示 -->
        <Label Grid.Row="0" Grid.Column="0" Content="姓名:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="0" Grid.Column="1" Margin="5" 
                 Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"/>
        <Button Grid.Row="0" Grid.Column="2" Margin="5" Content="更新" 
                Command="{Binding UpdateNameCommand}" 
                CommandParameter="{Binding ElementName=nameTextBox, Path=Text}"/>

        <!-- 年龄显示 -->
        <Label Grid.Row="1" Grid.Column="0" Content="年龄:" VerticalAlignment="Center" 
               Grid.RowSpan="2"/>
        <TextBlock Grid.Row="1" Grid.Column="1" Margin="5" VerticalAlignment="Center"
                   Text="{Binding Age, StringFormat='当前年龄: {0}'}"/>
        
        <StackPanel Grid.Row="2" Grid.Column="1" Orientation="Horizontal" Margin="5">
            <Button Content="增加" Margin="0,0,5,0" 
                    Command="{Binding IncrementAgeCommand}"/>
            <Button Content="减少" 
                    Command="{Binding DecrementAgeCommand}"/>
        </StackPanel>

        <!-- 启用状态 -->
        <CheckBox Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2" 
                  Content="启用功能" Margin="5" 
                  IsChecked="{Binding IsEnabled}"/>
        
        <!-- 显示信息 -->
        <TextBlock Grid.Row="4" Grid.Column="0" Grid.ColumnSpan="3" 
                   Margin="5,20,5,5" TextWrapping="Wrap"
                   Text="{Binding Name, StringFormat='你好, {0}! 你今年 {Binding Age} 岁了。'}"/>
    </Grid>
</Window>
```

### 3. 更复杂的示例 - 任务管理器

```csharp
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
```

### 4. 任务管理器的XAML界面

```xml
<Window x:Class="WpfApp.TaskManagerWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        Title="任务管理器 - CommunityToolkit.Mvvm 示例" Height="500" Width="600">
    
    <Window.DataContext>
        <vm:TaskManagerViewModel />
    </Window.DataContext>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 添加任务表单 -->
        <GroupBox Grid.Row="0" Header="添加新任务" Margin="0,0,0,10">
            <Grid Margin="10">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <StackPanel Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" Orientation="Horizontal">
                    <TextBox Text="{Binding NewTaskTitle, UpdateSourceTrigger=PropertyChanged}" 
                             Margin="0,0,5,5" Width="200" PlaceholderText="任务标题"/>
                    <TextBox Text="{Binding NewTaskDescription, UpdateSourceTrigger=PropertyChanged}" 
                             Margin="0,0,5,5" Width="300" PlaceholderText="任务描述"/>
                    <Button Content="添加任务" Command="{Binding AddTaskCommand}" 
                            Margin="0,0,5,5" Padding="10,3"/>
                </StackPanel>
            </Grid>
        </GroupBox>

        <!-- 任务列表 -->
        <GroupBox Grid.Row="1" Header="任务列表" Margin="0,0,0,10">
            <DataGrid Grid.Row="2" ItemsSource="{Binding Tasks}" 
                      AutoGenerateColumns="False" CanUserAddRows="False"
                      IsReadOnly="True" Height="250">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="标题" Binding="{Binding Title}" Width="*"/>
                    <DataGridTextColumn Header="描述" Binding="{Binding Description}" Width="*"/>
                    <DataGridCheckBoxColumn Header="完成" Binding="{Binding IsCompleted}" Width="80"/>
                    <DataGridTextColumn Header="创建日期" Binding="{Binding CreatedDate, StringFormat='yyyy-MM-dd HH:mm'}" Width="120"/>
                    <DataGridTemplateColumn Header="操作" Width="120">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal">
                                    <Button Content="完成" Command="{Binding DataContext.ToggleTaskCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" 
                                            CommandParameter="{Binding}" Margin="2" Padding="5,2"/>
                                    <Button Content="删除" Command="{Binding DataContext.DeleteTaskCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" 
                                            CommandParameter="{Binding}" Margin="2" Padding="5,2"/>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </GroupBox>

        <!-- 操作按钮 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="清除已完成任务" Command="{Binding ClearCompletedTasksCommand}" 
                    Margin="5" Padding="10,5"/>
        </StackPanel>
    </Grid>
</Window>
```

### 5. 主窗口代码

```csharp
using System.Windows;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
```

## 主要特性说明

CommunityToolkit.Mvvm 提供了以下主要功能：

1. **[ObservableProperty]** - 自动生成属性通知代码
2. **[RelayCommand]** - 简化命令创建
3. **ObservableObject** - 实现 INotifyPropertyChanged 和 INotifyPropertyChanging
4. **AsyncRelayCommand** - 支持异步命令

这些示例展示了在WPF应用程序中使用CommunityToolkit.Mvvm的基本方法，它大大简化了MVVM模式的实现，减少了样板代码的编写。

使用CommunityToolkit.Mvvm的主要优势：
- 减少样板代码
- 提供类型安全的命令
- 自动生成属性更改通知
- 支持异步命令
- 与数据验证集成

这些示例可以直接在WPF项目中使用，只需要安装CommunityToolkit.Mvvm NuGet包即可。



---



# 完整用法示例

CommunityToolkit.Mvvm 在 WPF 中的完整用法示例

基于 CommunityToolkit.Mvvm 库的源代码，我将为您提供在 WPF 中使用此库的所有主要功能的详细示例。

## 1. 基础 ObservableObject 和 ObservableProperty

### 基本视图模型
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace WpfApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int _age;

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private string _status = "就绪";

        public MainViewModel()
        {
            Name = "张三";
            Age = 25;
        }
    }
}
```

### 对应的 XAML
```xml
<Window x:Class="WpfApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        Title="CommunityToolkit.Mvvm 基础示例" Height="300" Width="400">
    
    <Window.DataContext>
        <vm:MainViewModel />
    </Window.DataContext>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <Label Grid.Row="0" Grid.Column="0" Content="姓名:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="0" Grid.Column="1" Margin="5" 
                 Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"/>

        <Label Grid.Row="1" Grid.Column="0" Content="年龄:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="1" Grid.Column="1" Margin="5" 
                 Text="{Binding Age, UpdateSourceTrigger=PropertyChanged, StringFormat={}{0}}"/>

        <CheckBox Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2" 
                  Content="启用" Margin="5" 
                  IsChecked="{Binding IsEnabled}"/>

        <TextBlock Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2" 
                   Margin="5,10,5,5" TextWrapping="Wrap"
                   Text="{Binding Status}"/>
    </Grid>
</Window>
```



## 2. RelayCommand 的使用

### 带命令的视图模型
```csharp
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
        [RelayCommand]
        private async Task LoadDataAsync()
        {
            Message = "正在加载数据...";
            await Task.Delay(2000); // 模拟异步操作
            Message = "数据加载完成";
        }
    }
}
```

### 对应的 XAML
```xml
<Window x:Class="WpfApp.CommandWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        xmlns:sys="http://schemas.microsoft.com/winfx/2009/xaml"
        Title="CommunityToolkit.Mvvm 命令示例"
        Width="500"
        Height="400">

    <Window.DataContext>
        <vm:CommandViewModel />
    </Window.DataContext>

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <Label Grid.Row="0" Grid.Column="0" VerticalAlignment="Center" Content="姓名:" />
        <TextBox Grid.Row="0" Grid.Column="1" Margin="5" Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" />
        <Button Grid.Row="0" Grid.Column="2" Margin="5" Content="保存" Command="{Binding SaveCommand}" />

        <Label Grid.Row="1" Grid.Column="0" VerticalAlignment="Center" Content="年龄:" />
        <TextBox Grid.Row="1" Grid.Column="1" Margin="5" Text="{Binding Age, UpdateSourceTrigger=PropertyChanged}" />

        <StackPanel Grid.Row="2" Grid.Column="1" Margin="5" Orientation="Horizontal">
            <Button Margin="0,0,5,0" Content="设置为25" Command="{Binding UpdateAgeCommand}" CommandParameter="25" />
            <Button Margin="0,0,5,0" Content="设置为30" Command="{Binding UpdateAgeCommand}" CommandParameter="30" />
            <Button Content="设置为35" Command="{Binding UpdateAgeCommand}" CommandParameter="35" />
        </StackPanel>

        <Button Grid.Row="3"
                Grid.Column="0"
                Grid.ColumnSpan="2"
                Margin="5"
                Content="重置"
                Command="{Binding ResetCommand}" />

        <Button Grid.Row="4"
                Grid.Column="0"
                Grid.ColumnSpan="2"
                Margin="5"
                Content="异步加载数据"
                Command="{Binding LoadDataCommand}" />

        <TextBlock Grid.Row="5"
                   Grid.Column="0"
                   Grid.ColumnSpan="3"
                   Margin="5,10,5,5"
                   TextWrapping="Wrap"
                   Text="{Binding Message}" />
    </Grid>
</Window>
```

## 3. 数据验证示例

### 带验证的视图模型
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;

namespace WpfApp.ViewModels
{
    [INotifyPropertyChanged]
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
                var errors = string.Join("\n", GetErrors().SelectMany(e => e.Value).Cast<string>());
                MessageBox.Show($"存在验证错误:\n{errors}");
            }
            else
            {
                MessageBox.Show($"提交成功: {Name}, {Age}, {Email}");
            }
        }
    }
}
```

### 对应的 XAML
```xml
<Window x:Class="WpfApp.ValidationWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        Title="CommunityToolkit.Mvvm 验证示例" Height="400" Width="500">
    
    <Window.DataContext>
        <vm:ValidationViewModel />
    </Window.DataContext>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <Label Grid.Row="0" Grid.Column="0" Content="姓名:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="0" Grid.Column="1" Margin="5" 
                 Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"/>
        <TextBlock Grid.Row="0" Grid.Column="2" Margin="5" Text="*" 
                   Foreground="Red" Visibility="{Binding HasErrors, Converter={StaticResource BoolToVisibilityConverter}}"/>

        <Label Grid.Row="1" Grid.Column="0" Content="年龄:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="1" Grid.Column="1" Margin="5" 
                 Text="{Binding Age, UpdateSourceTrigger=PropertyChanged, StringFormat={}{0}}"/>

        <Label Grid.Row="2" Grid.Column="0" Content="邮箱:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="2" Grid.Column="1" Margin="5" 
                 Text="{Binding Email, UpdateSourceTrigger=PropertyChanged}"/>

        <StackPanel Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="3" 
                    Orientation="Horizontal" Margin="5">
            <Button Content="验证" Margin="0,0,5,0" 
                    Command="{Binding ValidateCommand}"/>
            <Button Content="提交" 
                    Command="{Binding SubmitCommand}"/>
        </StackPanel>

        <ItemsControl Grid.Row="4" Grid.Column="0" Grid.ColumnSpan="3" 
                      Margin="5,10,5,5" 
                      ItemsSource="{Binding Path=(Validation.Errors), RelativeSource={RelativeSource AncestorType=FrameworkElement}}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding ErrorContent}" Foreground="Red"/>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </Grid>
</Window>
```

## 4. Messenger 消息传递

### 使用消息传递的视图模型
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace WpfApp.ViewModels
{
    public partial class MessengerViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string _message = string.Empty;
        
        [ObservableProperty]
        private string _receivedMessage = string.Empty;

        public MessengerViewModel(IMessenger messenger) : base(messenger)
        {
            // 注册消息接收器
            this.Messenger.Register<ValueChangedMessage<string>>(this, (r, m) =>
            {
                ReceivedMessage = $"收到消息: {m.Value}";
            });
        }

        [RelayCommand]
        private void SendMessage()
        {
            this.Messenger.Send(new ValueChangedMessage<string>(Message));
            Message = string.Empty;
        }
    }
}
```

### 对应的 XAML
```xml
<Window x:Class="WpfApp.MessengerWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        Title="CommunityToolkit.Mvvm 消息传递示例" Height="300" Width="500">
    
    <Window.DataContext>
        <vm:MessengerViewModel />
    </Window.DataContext>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <Label Grid.Row="0" Grid.Column="0" Content="发送消息:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="0" Grid.Column="1" Margin="5" 
                 Text="{Binding Message, UpdateSourceTrigger=PropertyChanged}"/>

        <Button Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2" 
                Margin="5" Content="发送消息" 
                Command="{Binding SendMessageCommand}"/>

        <Label Grid.Row="2" Grid.Column="0" Content="接收消息:" VerticalAlignment="Center"/>
        <TextBlock Grid.Row="2" Grid.Column="1" Margin="5" 
                   Text="{Binding ReceivedMessage}" 
                   TextWrapping="Wrap"/>

        <TextBlock Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2" 
                   Margin="5,10,5,5" TextWrapping="Wrap"
                   Text="此示例展示了如何在不同组件间传递消息，而不需要直接引用。"/>
    </Grid>
</Window>
```



在使用 `MessengerViewModel` 时，您遇到了依赖注入的问题。由于 `MessengerViewModel` 的构造函数需要 [IMessenger](file://d:\Users\84574\Documents\GitHub\CommunityToolkit-dotnet\src\CommunityToolkit.Mvvm\Messaging\IMessenger.cs#L59-L154) 参数，而 XAML 中直接创建实例时无法提供这个参数，您需要采用以下几种解决方案之一：

### 方案1：使用 Ioc.Default（推荐）

修改您的 `MessengerViewModel`：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace WpfApp.ViewModels
{
    public partial class MessengerViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string _message = string.Empty;
        
        [ObservableProperty]
        private string _receivedMessage = string.Empty;

        // 使用默认构造函数，通过 Ioc.Default 获取 Messenger
        public MessengerViewModel() : base(CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IMessenger>())
        {
            // 注册消息接收器
            this.Messenger.Register<ValueChangedMessage<string>>(this, (r, m) =>
            {
                ReceivedMessage = $"收到消息: {m.Value}";
            });
        }

        [RelayCommand]
        private void SendMessage()
        {
            this.Messenger.Send(new ValueChangedMessage<string>(Message));
            Message = string.Empty;
        }
    }
}
```

### 方案2：使用无参数基类构造函数

或者，您也可以使用无参数构造函数，然后在需要时通过 [WeakReferenceMessenger.Default](file://d:\Users\84574\Documents\GitHub\CommunityToolkit-dotnet\src\CommunityToolkit.Mvvm\Messaging\WeakReferenceMessenger.cs#L90-L90) 获取默认的 messenger：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace WpfApp.ViewModels
{
    public partial class MessengerViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string _message = string.Empty;
        
        [ObservableProperty]
        private string _receivedMessage = string.Empty;

        public MessengerViewModel() : base()
        {
            // 注册消息接收器
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<string>>(this, (r, m) =>
            {
                ReceivedMessage = $"收到消息: {m.Value}";
            });
        }

        [RelayCommand]
        private void SendMessage()
        {
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<string>(Message));
            Message = string.Empty;
        }
    }
}
```



### 方案3：使用依赖注入容器

如果您想使用依赖注入，需要在应用程序启动时注册视图模型：

在 `App.xaml.cs` 中：
```csharp
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using WpfApp.ViewModels;

namespace WpfApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            
            // 注册 Messenger 服务
            services.AddSingleton<IMessenger, WeakReferenceMessenger>();
            
            // 注册视图模型
            services.AddTransient<MessengerViewModel>();
            
            CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.ConfigureServices(services.BuildServiceProvider());

            base.OnStartup(e);
        }
    }
}
```

然后在 XAML 中使用依赖注入获取视图模型：

```xml
<Window x:Class="WpfApp.MessengerWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        Title="CommunityToolkit.Mvvm 消息传递示例" Height="300" Width="500">
    
    <Window.DataContext>
        <x:Static Member="vm:CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default" />
    </Window.DataContext>
    
    <Grid Margin="20">
        <!-- 界面内容 -->
    </Grid>
</Window>
```

不过这种方式比较复杂，所以推荐使用**方案1**或**方案2**。

### 完整的修改后代码（推荐方案2）

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace WpfApp.ViewModels
{
    public partial class MessengerViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string _message = string.Empty;
        
        [ObservableProperty]
        private string _receivedMessage = string.Empty;

        public MessengerViewModel() : base()
        {
            // 注册消息接收器
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<string>>(this, (r, m) =>
            {
                ReceivedMessage = $"收到消息: {m.Value}";
            });
        }

        [RelayCommand]
        private void SendMessage()
        {
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<string>(Message));
            Message = string.Empty;
        }
    }
}
```

```xml
<Window x:Class="WpfApp.MessengerWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        Title="CommunityToolkit.Mvvm 消息传递示例" Height="300" Width="500">
    
    <Window.DataContext>
        <vm:MessengerViewModel />
    </Window.DataContext>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <Label Grid.Row="0" Grid.Column="0" Content="发送消息:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="0" Grid.Column="1" Margin="5" 
                 Text="{Binding Message, UpdateSourceTrigger=PropertyChanged}"/>

        <Button Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2" 
                Margin="5" Content="发送消息" 
                Command="{Binding SendMessageCommand}"/>

        <Label Grid.Row="2" Grid.Column="0" Content="接收消息:" VerticalAlignment="Center"/>
        <TextBlock Grid.Row="2" Grid.Column="1" Margin="5" 
                   Text="{Binding ReceivedMessage}" 
                   TextWrapping="Wrap"/>

        <TextBlock Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2" 
                   Margin="5,10,5,5" TextWrapping="Wrap"
                   Text="此示例展示了如何在不同组件间传递消息，而不需要直接引用。"/>
    </Grid>
</Window>
```

这样修改后，`MessengerViewModel` 就可以正常在 XAML 中使用了。



## 5. 依赖注入示例

### 配置和使用依赖注入
```csharp
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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
```

### 应用程序启动配置
```csharp
using Microsoft.Extensions.DependencyInjection;
using WpfApp.Services;

namespace WpfApp
{
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
```

## 6. 高级用法 - 部分方法

### 使用部分方法处理属性更改
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

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
```

## 7. 继承和多态示例

### 基类和派生类
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfApp.ViewModels
{
    [ObservableObject]  // 使用特性标记整个类
    public partial class BaseViewModel
    {
        [ObservableProperty]
        private string _title = string.Empty;
    }

    public partial class DerivedViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private DateTime _lastUpdated = DateTime.Now;
    }
}
```

## 8. 通知属性更改依赖项

### 依赖属性更改
```csharp
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
```

### 对应的 XAML
```xml
<Window x:Class="WpfApp.DependentPropertyWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        Title="依赖属性示例" Height="300" Width="400">
    
    <Window.DataContext>
        <vm:DependentPropertyViewModel />
    </Window.DataContext>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <Label Grid.Row="0" Grid.Column="0" Content="名:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="0" Grid.Column="1" Margin="5" 
                 Text="{Binding FirstName, UpdateSourceTrigger=PropertyChanged}"/>

        <Label Grid.Row="1" Grid.Column="0" Content="姓:" VerticalAlignment="Center"/>
        <TextBox Grid.Row="1" Grid.Column="1" Margin="5" 
                 Text="{Binding LastName, UpdateSourceTrigger=PropertyChanged}"/>

        <Label Grid.Row="2" Grid.Column="0" Content="全名:" VerticalAlignment="Center"/>
        <TextBlock Grid.Row="2" Grid.Column="1" Margin="5" 
                   Text="{Binding FullName}"/>

        <Button Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2" 
                Margin="5" Content="更新姓名" 
                Command="{Binding UpdateNamesCommand}"/>
    </Grid>
</Window>
```

## 9. 异步命令示例

### 复杂的异步操作


### 1. 异步命令视图模型

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
```

### 2. XAML 界面

```xml
<Window x:Class="WpfApp.AsyncCommandWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WpfApp.ViewModels"
        Title="CommunityToolkit.Mvvm 异步命令示例" Height="500" Width="600">
    
    <Window.DataContext>
        <vm:AsyncCommandViewModel />
    </Window.DataContext>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- 状态显示 -->
        <TextBlock Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" 
                   Margin="5" Text="{Binding Status}" 
                   FontWeight="Bold" Foreground="Blue"/>

        <!-- 进度条 -->
        <ProgressBar Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2" 
                     Margin="5" Height="20" 
                     Value="{Binding Progress}" 
                     IsIndeterminate="{Binding IsBusy, Converter={StaticResource BoolToVisibilityConverter}}"/>

        <!-- 操作按钮 -->
        <StackPanel Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2" 
                    Orientation="Horizontal" Margin="5">
            <Button Content="执行长时间操作" 
                    Command="{Binding LongRunningOperationCommand}" 
                    Margin="0,0,10,0" 
                    IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBooleanConverter}}"/>
            
            <Button Content="执行可取消操作" 
                    Command="{Binding CancelableOperationCommand}" 
                    Margin="0,0,10,0" 
                    IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBooleanConverter}}"/>
            
            <Button Content="执行多个操作" 
                    Command="{Binding MultipleOperationsCommand}" 
                    IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBooleanConverter}}"/>
        </StackPanel>

        <!-- 操作说明 -->
        <TextBlock Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2" 
                   Margin="5,10,5,5" TextWrapping="Wrap">
            <Run Text="说明:" FontWeight="Bold"/>
            <LineBreak/>
            <Run Text="• 长时间操作：模拟一个需要时间完成的任务，显示进度。"/>
            <LineBreak/>
            <Run Text="• 可取消操作：可以随时取消的异步操作。"/>
            <LineBreak/>
            <Run Text="• 多个操作：同时执行多个异步任务。"/>
        </TextBlock>

        <!-- 结果显示 -->
        <GroupBox Grid.Row="4" Grid.Column="0" Grid.ColumnSpan="2" 
                  Header="结果" Margin="5">
            <ScrollViewer>
                <TextBlock Text="{Binding Result}" 
                           TextWrapping="Wrap" 
                           VerticalAlignment="Top"/>
            </ScrollViewer>
        </GroupBox>

        <!-- 状态指示器 -->
        <Border Grid.Row="5" Grid.Column="0" Grid.ColumnSpan="2" 
                Margin="5,10,5,5" Padding="10" 
                Background="LightGray" CornerRadius="5">
            <StackPanel>
                <TextBlock Text="当前状态信息:" FontWeight="Bold"/>
                <TextBlock Text="{Binding Path=IsBusy, StringFormat='忙碌状态: {0}'}" Margin="10,2,0,0"/>
                <TextBlock Text="{Binding Path=Progress, StringFormat='进度: {0}%'}" Margin="10,2,0,0"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

### 3. 需要的转换器资源

由于上面的 XAML 使用了转换器，您需要在 App.xaml 或 Window.Resources 中定义它们：

#### 在 App.xaml 中添加资源字典：
```xml
<Application x:Class="WpfApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:WpfApp"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
        
        <!-- 反向布尔转换器 -->
        <local:InverseBooleanConverter x:Key="InverseBooleanConverter"/>
    </Application.Resources>
</Application>
```

#### 创建反向布尔转换器：
```csharp
using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value;
        }
    }
}
```

### 4. 完整的主窗口示例

```csharp
using System.Windows;

namespace WpfApp
{
    public partial class AsyncCommandWindow : Window
    {
        public AsyncCommandWindow()
        {
            InitializeComponent();
        }
    }
}
```

### 5. 更复杂的异步示例 - 网络请求

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http;
using System.Text.Json;

namespace WpfApp.ViewModels
{
    public partial class NetworkAsyncCommandViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _status = "就绪";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _result = string.Empty;

        private readonly HttpClient _httpClient = new HttpClient();

        [RelayCommand]
        private async Task FetchDataAsync()
        {
            try
            {
                IsBusy = true;
                Status = "正在获取数据...";

                // 模拟网络请求
                var response = await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/posts/1");
                var content = await response.Content.ReadAsStringAsync();
                
                Result = content;
                Status = "数据获取成功";
            }
            catch (Exception ex)
            {
                Result = $"错误: {ex.Message}";
                Status = "获取数据失败";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task FetchMultipleDataAsync()
        {
            try
            {
                IsBusy = true;
                Status = "获取多个数据源...";

                var tasks = new[]
                {
                    _httpClient.GetAsync("https://jsonplaceholder.typicode.com/posts/1"),
                    _httpClient.GetAsync("https://jsonplaceholder.typicode.com/posts/2"),
                    _httpClient.GetAsync("https://jsonplaceholder.typicode.com/posts/3")
                };

                var responses = await Task.WhenAll(tasks);
                var contents = await Task.WhenAll(
                    responses.Select(r => r.Content.ReadAsStringAsync()));

                Result = string.Join("\n\n", contents);
                Status = "所有数据获取完成";
            }
            catch (Exception ex)
            {
                Result = $"错误: {ex.Message}";
                Status = "获取数据失败";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
```

### 主要特性说明

1. **异步命令**: 使用 `async Task` 方法定义异步命令
2. **取消支持**: 使用 [CancellationToken](file://d:\Users\84574\Documents\GitHub\CommunityToolkit-dotnet\src\CommunityToolkit.Mvvm\Messaging\Messages\AsyncCollectionRequestMessage{T}.cs#L59-L59) 参数支持取消操作
3. **状态管理**: 通过 `IsBusy` 属性控制 UI 的忙碌状态
4. **进度显示**: 通过 `Progress` 属性显示操作进度
5. **错误处理**: 在 `try-catch-finally` 块中正确处理异常
6. **UI 更新**: 在操作完成后更新状态和结果显示

这些示例展示了 CommunityToolkit.Mvvm 中异步命令的强大功能，使您可以轻松处理长时间运行的操作，同时保持 UI 的响应性。



## 总结

CommunityToolkit.Mvvm 在 WPF 中的主要用途包括：

1. **ObservableObject**: 实现 INotifyPropertyChanged 和 INotifyPropertyChanging 接口的基础类
2. **ObservableProperty**: 通过特性自动生成属性通知代码
3. **RelayCommand**: 简化命令创建，支持同步和异步操作
4. **ObservableValidator**: 支持数据验证的基类
5. **ObservableRecipient**: 带有消息传递支持的可观察对象
6. **Messenger**: 轻松的消息传递系统
7. **Ioc**: 依赖注入容器支持
8. **部分方法**: 在属性更改前后执行自定义逻辑

这些功能大大简化了 MVVM 模式的实现，减少了样板代码，提高了开发效率。



---



# 获取XAML(VM)实例

在WPF中，有多种方法可以从C#代码中获取XAML中定义的视图模型(VM)实例。以下是几种常用的方法：

## 1. 通过 DataContext 获取

### 方法一：直接访问 Window 的 DataContext
```csharp
// 在 Window 类中获取 VM
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 方法1：直接转换
        var vm = (MainViewModel)this.DataContext;
        
        // 方法2：安全转换
        if (this.DataContext is MainViewModel mainVm)
        {
            // 使用 VM 实例
            mainVm.SomeProperty = "New Value";
            mainVm.SomeCommand.Execute(null);
        }
    }
    
    // 或者在其他方法中获取
    private void SomeButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is MainViewModel vm)
        {
            vm.Name = "New Name";
        }
    }
}
```

## 2. 通过 FindResource 获取

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 如果 VM 在资源中定义
        var vm = this.FindResource("MyViewModel") as MainViewModel;
    }
}
```

## 3. 通过依赖属性获取

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 获取当前窗口的 VM
        var vm = this.TryFindResource("MainViewModel") as MainViewModel;
    }
}
```

## 4. 从控件获取其父级的 VM

```csharp
// 在事件处理程序中获取 VM
private void Button_Click(object sender, RoutedEventArgs e)
{
    // 从按钮获取其父级的 DataContext
    if (sender is FrameworkElement element)
    {
        if (element.DataContext is MainViewModel vm)
        {
            // 使用 VM
            vm.DoSomething();
        }
    }
}
```

## 5. 通过 Visual Tree 遍历获取

```csharp
public static class VisualTreeHelperExtensions
{
    public static T FindAncestor<T>(DependencyObject current) where T : class
    {
        do
        {
            if (current is T ancestor)
                return ancestor;
            current = VisualTreeHelper.GetParent(current);
        }
        while (current != null);
        
        return null;
    }
}

// 使用方法
private void SomeMethod()
{
    var window = Application.Current.Windows.OfType<Window>()
        .FirstOrDefault(x => x.IsActive);
    
    if (window?.DataContext is MainViewModel vm)
    {
        // 使用 VM
        vm.SomeMethod();
    }
}
```

## 6. 通过 DependencyObject 的 DataContext

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    // 从任意 UI 元素获取 VM
    private MainViewModel GetViewModelFromElement(FrameworkElement element)
    {
        return element.DataContext as MainViewModel;
    }
    
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var vm = GetViewModelFromElement(element);
            if (vm != null)
            {
                vm.SomeCommand.Execute(null);
            }
        }
    }
}
```

## 7. 使用静态属性或服务定位器模式

### 创建一个服务类来管理 VM
```csharp
public static class ViewModelLocator
{
    public static T GetViewModel<T>(Window window) where T : class
    {
        return window.DataContext as T;
    }
    
    public static T GetCurrentWindowViewModel<T>() where T : class
    {
        var window = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.IsActive);
        return window?.DataContext as T;
    }
}

// 使用方法
public partial class SomeOtherClass
{
    private void SomeMethod()
    {
        var vm = ViewModelLocator.GetCurrentWindowViewModel<MainViewModel>();
        if (vm != null)
        {
            vm.Name = "Updated from other class";
        }
    }
}
```

## 8. 通过事件传递 VM

```csharp
// 在 VM 中定义事件
public partial class MainViewModel : ObservableObject
{
    public event Action<MainViewModel> ViewModelReady;
    
    public MainViewModel()
    {
        // 触发事件，传递自身引用
        ViewModelReady?.Invoke(this);
    }
}

// 在代码中订阅事件
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var vm = new MainViewModel();
        vm.ViewModelReady += OnViewModelReady;
        base.OnStartup(e);
    }
    
    private void OnViewModelReady(MainViewModel vm)
    {
        // 现在可以使用 VM 实例
        // 可以将其存储到静态变量或传递给其他组件
    }
}
```

## 9. 通过构造函数注入

```csharp
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    
    public MainWindow()
    {
        _viewModel = new MainViewModel();
        this.DataContext = _viewModel;
        InitializeComponent();
    }
    
    // 现在可以直接使用 _viewModel
    private void SomeMethod()
    {
        _viewModel.SomeProperty = "New Value";
    }
}
```

## 10. 使用 Ioc 容器获取

```csharp
// 如果使用了 Ioc 容器
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 从 Ioc 容器获取 VM
        var vm = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<MainViewModel>();
        
        if (vm != null)
        {
            this.DataContext = vm;
        }
    }
}
```

## 最佳实践建议

1. **推荐使用方法1**（直接从 DataContext 获取）：最简单直接
2. **避免硬编码依赖**：尽量使用松耦合的方式
3. **考虑使用消息传递**：使用 CommunityToolkit.Mvvm 的 Messenger 进行组件间通信
4. **使用接口**：定义接口来降低耦合度

选择哪种方法取决于您的具体需求和应用程序架构。通常情况下，直接从 DataContext 获取是最简单有效的方法。