[CommunityToolkit从入门到精通（详细版） - 赵书记 - 博客园](https://www.cnblogs.com/davisdabing/p/18196205)

[ObservableObject - Community Toolkits for .NET | Microsoft Learn](https://learn.microsoft.com/zh-cn/dotnet/communitytoolkit/mvvm/observableobject)

## 异常

```
当前 Visual Studio 版本不支持面向 .NET 10.0。请面向 .NET 9.0 或更低版本，或者使用 Visual Studio 17.16 或更高版本
/langversion 的选项“14.0”无效。使用 "/langversion:?" 列出支持的值。
```



```
/langversion 的选项“14.0”无效。使用 "/langversion:?" 列出支持的值。
当前 Visual Studio 版本不支持面向 .NET 10.0。请面向 .NET 9.0 或更低版本，或者使用 Visual Studio 17.16 或更高版本
```



### global.json版本问题

```
当前 Visual Studio 版本不支持面向 .NET 10.0。请面向 .NET 9.0 或更低版本，或者使用 Visual Studio 17.16 或更高版本
无法解析位于 D:\Users\Thinkpad\Documents\GitHub\CommunityToolkit-dotnet\global.json 的 global.json 中指定的 .NET SDK 版本。
```



### .editorconfig 文件配置了文件头模板

```
严重性 代码 说明 项目 文件 行 抑制状态 详细信息 错误(活动) IDE0073 源文件缺少所需的标题。 (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0073) WpfApp D:\Users\84574\Documents\GitHub\CommunityToolkit-dotnet\src\WpfApp\CommandWindow.xaml.cs 1
```



这个错误是由于代码风格分析器检测到您的源文件缺少必需的文件头注释。根据错误信息，您在 WPF 项目中创建了一个 [CommandWindow.xaml.cs](file://d:\Users\84574\Documents\GitHub\CommunityToolkit-dotnet\src\WpfApp\CommandWindow.xaml.cs) 文件，但该文件缺少项目要求的文件头注释。

我需要先查看项目的代码风格配置文件，了解所需的文件头格式。



现在我明白了问题所在。根据 .editorconfig 文件，项目要求所有 C# 文件都必须包含特定的文件头注释。我需要查看您提到的文件，然后添加正确的文件头。



现在我需要根据 .editorconfig 文件中的配置为这个文件添加正确的文件头注释。根据 .editorconfig 文件，`file_header_template` 设置为：

```
Licensed to the .NET Foundation under one or more agreements.\nThe .NET Foundation licenses this file to you under the MIT license.\nSee the LICENSE file in the project root for more information.
```

我将为该文件添加必需的文件头注释：

```csharp|CODE_EDIT_BLOCK|d:\Users\84574\Documents\GitHub\CommunityToolkit-dotnet\src\WpfApp\CommandWindow.xaml.cs
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
// ... existing code ...
```

这个修改添加了项目要求的文件头注释，应该可以解决 IDE0073 错误。


---



# 源生成器代码无法生成

## 问题描述

在本地引用 CommunityToolkit.Mvvm 项目时遇到源生成器代码无法生成的问题。

```
C:\Users\84574>dotnet --info
.NET SDK:
 Version:           9.0.301
 Commit:            a596cd22e2
 Workload version:  9.0.300-manifests.07067f5b
 MSBuild version:   17.14.5+edd3bbf37
```



我理解您在本地引用 CommunityToolkit.Mvvm 项目时遇到源生成器代码无法生成的问题。这通常是因为源生成器项目没有正确引用或配置。让我帮您分析并解决这个问题。

首先，我需要了解项目的结构和配置，以便找出问题所在。

让我尝试使用 list_dir 来查看项目根目录的文件结构。

我看到这是一个解决方案，包含 src 和 tests 目录。让我查看一下解决方案文件和一些项目文件。

现在让我查看 CommunityToolkit.Mvvm 项目的项目文件，以了解源生成器是如何配置的。

现在让我查看源生成器项目的项目文件，了解其配置。

让我查看源生成器的共享项目文件。

现在，让我查看一下构建目录中的 targets 文件，这可能是您之前提到的代码段的来源。

让我检查 src 目录中是否有 CommunityToolkit.Mvvm 的 targets 文件。


让我在 src/CommunityToolkit.Mvvm 目录中查找 targets 文件。


现在让我查看这些 targets 文件，了解它们的内容。

现在我了解了问题所在。您之前贴出的代码段确实是 CommunityToolkit.Mvvm.targets 文件的内容。现在让我查看源生成器的 targets 文件。

现在，让我创建一个测试项目来演示如何正确引用 CommunityToolkit.Mvvm 并使其源生成器正常工作。

首先，让我查看一个使用了 MVVM Toolkit 的测试项目作为示例。

现在我了解了项目结构。让我为您解释在本地引用 CommunityToolkit.Mvvm 项目时如何解决源生成器不工作的问题。





## 问题分析

在本地引用 CommunityToolkit.Mvvm 项目时，源生成器无法正常工作的原因主要有：

1. 当直接引用 CommunityToolkit.Mvvm 项目时，源生成器项目（CommunityToolkit.Mvvm.SourceGenerators）可能没有被正确引用或配置

2. 源生成器项目是多目标的，针对不同版本的 Roslyn 编译器，需要确保正确的版本被使用

## 解决方案

```xml
	<ItemGroup>
		<ProjectReference Include="..\..\..\..\CommunityToolkit-dotnet\src\CommunityToolkit.Mvvm\CommunityToolkit.Mvvm.csproj" />
		<ProjectReference Include="..\..\..\..\CommunityToolkit-dotnet\src\CommunityToolkit.Mvvm.SourceGenerators.Roslyn4120\CommunityToolkit.Mvvm.SourceGenerators.Roslyn4120.csproj" ReferenceOutputAssembly="false" OutputItemType="Analyzer" />
	</ItemGroup>
```





要让源生成器在本地项目中正常工作，您需要确保以下几点：

### 1. 在您的项目中正确引用 CommunityToolkit.Mvvm 项目

如果您在本地解决方案中引用 CommunityToolkit.Mvvm，需要确保同时引用源生成器项目：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- 引用主 MVVM Toolkit 项目 -->
    <ProjectReference Include="..\..\src\CommunityToolkit.Mvvm\CommunityToolkit.Mvvm.csproj" />
    
    <!-- 为确保源生成器工作，可能还需要引用对应的源生成器项目 -->
    <ProjectReference Include="..\..\src\CommunityToolkit.Mvvm.SourceGenerators.Roslyn4001\CommunityToolkit.Mvvm.SourceGenerators.Roslyn4001.csproj" 
                      ReferenceOutputAssembly="false" OutputItemType="Analyzer" />
  </ItemGroup>
</Project>
```



### 2. 或者，使用 NuGet 包方式引用

更推荐的方式是使用 NuGet 包引用，这样源生成器会自动配置：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
  </ItemGroup>
</Project>
```



### 3. 确保使用支持源生成器的 .NET SDK 版本

源生成器需要 .NET 5 或更高版本的 SDK，且需要 C# 语言版本为 9.0 或更高。



### 4. 示例测试代码

以下是一个测试代码示例，验证源生成器是否正常工作：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace YourTestApp
{
    // 这个类使用 ObservableObject 属性，源生成器应该生成 INotifyPropertyChanged 代码
    public partial class MyViewModel : ObservableObject
    {
        // 这个属性使用 ObservableProperty 属性，源生成器应该生成属性更改通知代码
        [ObservableProperty]
        private string? name;
        
        // 这个命令使用 RelayCommand 属性，源生成器应该生成命令实现
        [RelayCommand]
        private void DoSomething()
        {
            // 命令实现
        }
    }
}
```



### 5. 如果您正在开发 MVVM Toolkit 本身

如果您正在对 MVVM Toolkit 进行开发和测试，您需要：

1. 构建整个解决方案：
   ```bash
   dotnet build dotnet.slnx
   ```

2. 确保所有源生成器项目都已成功构建

3. 在测试项目中使用项目引用，而不是 NuGet 包



### 6. 常见问题排查

- 确保使用的是最新版本的 .NET SDK
- 确保项目是 SDK 风格的项目（使用 PackageReference）
- 如果使用 Visual Studio，确保重启 VS 以确保源生成器加载
- 检查输出窗口是否有任何与源生成器相关的错误

通过以上配置，您的本地项目应该能够正确使用 CommunityToolkit.Mvvm 的源生成器功能。

要验证源生成器是否正常工作，您可以在 IDE 中查看生成的代码（通常在 `obj` 目录下的 [.g.cs](file://d:\Users\84574\Documents\GitHub\CommunityToolkit-dotnet\src\CommunityToolkit.Diagnostics\Generated\TypeInfo.g.cs) 文件中）或者观察 `[ObservableProperty]`、`[RelayCommand]` 等属性是否正确生成了相应的代码。





# [CommunityToolkit从入门到精通（详细版）](https://www.cnblogs.com/davisdabing/p/18196205)

## MVVM基础

![img](https://img2024.cnblogs.com/blog/3052701/202405/3052701-20240516144508383-1674732747.png)

-  **Model:**负责存储数据，以及对数据的处理。

- **View：**负责展示数据，以及与用户的交互。

- **ViewModel：**负责将 Model 的数据在 View 中显示出来，同时也负责将 View 中的事件传递给 Model。可以说，ViewModel 是 Model 和 View 之间的桥梁。

  

## 组件模型(ComponentModel)

### ObservableObject

**`ObservableObject`** 是工具包为我们提供的一个实现了 `INotifyPropertyChanged` 接口的基类。

拥有两个子类：

- `ObservableValidator`
- `ObservableRecipient`

ObservableObject 类提供的方法：

`ObservableObject` 除了实现了 `INotifyPropertyChanged` 接口之外，还提供了一些方法来帮助我们实现通知功能。

- `OnPropertyChanged`（拥有两个方法重载）
- `SetProperty`（包含六个方法重载）
- `SetPropertyAndNotifyOnCompletion`（包含五个重载，用于 `TaskNotifier`）

#### OnPropertyChanged

简易版：

```c#
class MyViewModel : ObservableObject
{
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }
}
```

完整版：

```c#
class MyViewModel : ObservableObject
{
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            // 检查值是否发生变化
            if (_name == value ) return;

            // 触发属性值即将发生变化的事件
            OnPropertyChanging();

            // 设置属性值
            _name = value;

            // 触发属性值已经发生变化的事件
            OnPropertyChanged();
        }
    }
}
```



#### SetProperty

`SetProperty` 方法用于设置属性值，并在属性值发生变化时触发 `PropertyChanged` 事件。该方法的底层其实就是在调用 `OnPropertyChanged`。这里是一个最简单的 `SetProperty` 方法的实现：

```c#
protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
{
    // 采用默认的 Comparer 来比较新旧值是否相等
    if (EqualityComparer<T>.Default.Equals(field, newValue))
    {
        return false;
    }

    OnPropertyChanging(propertyName);
    field = newValue;
    OnPropertyChanged(propertyName);
    return true;
}
```

`SetProperty` 方法的返回值是一个 `bool` 类型的值，用于表示属性值是否发生变化。如果属性值发生变化，则返回 `true`，否则返回 `false`。所以，如果希望在属性值发生变化时执行一些额外的操作，可以通过判断 `SetProperty` 方法的返回值来实现。比如：

```c#
private string _name;

public string Name
{
    get => _name;
    set
    {
        if (SetProperty(ref _name, value, nameof(Name)))
        {
            // 属性值发生变化时，执行一些额外的操作
        }
    }
}
```





## ObservableRecipient



## ObservableValidator



## 中继指令（RelayCommand）

WPF 等框架中，如果希望为一个按钮控件绑定一个命令，正确的做法是为它的 `Command` 属性绑定一个 `ICommand` 接口类型的对象。但是，`ICommand` 接口的实现类通常需要自己实现 `CanExecute` 和 `Execute` 方法，这样会导致代码冗余。为了解决这个问题，于是有了“中继指令”（RelayCommand）这个概念。

`ICommand` 接口的定义如下：

```c#
public interface ICommand
{
    bool CanExecute(object parameter);
    void Execute(object parameter);
    event EventHandler CanExecuteChanged;
}
```

但是工具包并不满足于此，而是又给出了几个额外的接口，包括：

- `IRelayCommand`
- `IRelayCommand<T>`
- `IAsyncRelayCommand`
- `IAsyncRelayCommand<T>`

其中，泛型版本为表示该中继指令可以传入一个参数，通常也就是按钮等控件的 `CommandParameter` 属性。

`IRelayCommand` 与 `IRelayCommmand<T>` 接口的定义分别为:

```c#
public interface IRelayCommand : ICommand
{
    void NotifyCanExecuteChanged();
}

public interface IRelayCommand<in T> : IRelayCommand
{
    bool CanExecute(T? parameter);

    void Execute(T? parameter);
}
```

它在 `ICommand` 的基础上增加了一个 `NotifyCanExecuteChanged` 方法，用于通知命令的可执行状态发生了变化。这样，我们就可以更便捷地实现这一功能了。比如我们可以在某个属性的 `setter` 中调用这个方法，这样当属性值发生变化时，命令的可执行状态也会及时得到更新。

IAsyncRelayCommand接口定义如下：

```c#
public interface IAsyncRelayCommand : IRelayCommand, INotifyPropertyChanged
{
    Task? ExecutionTask { get; }

    bool CanBeCanceled { get; }

    bool IsCancellationRequested { get; }

    bool IsRunning { get; }

    Task ExecuteAsync(object? parameter);

    void Cancel();
}
```

它在 `IRelayCommand` 的基础上增加了一些异步操作相关的属性和方法，比如 `ExecutionTask` 属性用于获取当前的执行任务，`Cancel` 方法用于取消当前的执行任务，`IsRunning` 属性用于判断当前异步任务是否正在执行等。



### RelayCommand

工具包为我们提供了一个 `RelayCommand` 类，用于快捷地实现中继指令。它最朴素的用法是：

```c#
public class ViewModel : ObservableObject
{
    public IRelayCommand MyCommand1 { get; } //通常我们会将类型声明为 IRelayCommand，而不是 RelayCommand，从而让使用者只关注接口暴露的方法，而不关心其实现；并且通常我们会在 VM 的构造函数中初始化这些属性，所以只提供 getter 即可
    public IRelayCommand MyCommand2 { get; }

    public ViewModel()
    {
        MyCommand1 = new RelayCommand(Execute, CanExecute); //RelayCommand 的构造函数可以接受两个参数：Execute 和 CanExecute，分别表示执行和判断是否可执行的方法
        MyCommand2 = new (() => {
            DoJob();
            Debug.WriteLine();
        }); //RelayCommand 的构造函数也支持只传入一个参数，表示执行的方法，且不提供 CanExecute 的逻辑。而且这里除了直接传入符合委托类型的方法，还可以使用一个 Lambda 表达式简单地进行实现。
    }

    private void Execute()
    {
        DoJob();
    }

    private bool CanExecute()
    {
        return true;
    }

    private void DoJob() { }
}
```

但是在实际使用工具包进行开发时，我们基本不会采用上述朴素的方式进行实现，而是会借助**源生成器**。方式如下：

```c#
public partial class ViewModel : ObservableObject
{
    [ObservableProperty]
    private string name;

    private bool CanSubmit() => !string.IsNullOrEmpty(name);

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Submit()
    {
        Debug.WriteLine($"Name: {name}");
    }
}
```

后台生成的代码为：

```c#
partial class MainViewModel
{
    private RelayCommand? submitCommand;
    public IRelayCommand SubmitCommand
        => submitCommand ??= new RelayCommand(new Action(Submit), CanSubmit);
}
```



### AsyncRelayCommand

异步中继指令更加强大，因为它背后可以是一个异步任务。这个异步任务的状态默认会对按钮控件的可用性进行控制，并且异步任务有办法取消。

源生成器基本用法：

```c#
partial class MainViewModel : ObservableValidator
{
    [ObservableProperty]
    private string name = "";

    private bool CanSubmit() => !string.IsNullOrEmpty(Name);

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        await Task.Delay(1000);
    }
}
```

后台会为我们生成名为 **`SubmitCommand` 的属性**。工具包在生成这个属性时，会自动去掉原始方法名的 **`Async` 结尾**。

取消任务

如果我们想要为这个异步中继指令添加取消功能，除了手动实现，我们还可以借助源生成器。只需要将上例修改为：

```c#
[RelayCommand(CanExecute = nameof(CanSubmit), IncludeCancelCommand = true)]
private async Task SubmitAsync(CancellationToken token)
{
    try
    {
        await Task.Delay(2000, token);
    }
    catch (OperationCanceledException)
    {

    }
}
```

即可。此时后台会为我们额外生成一个名为 `SubmitCancelCommand` 的中继指令。此时只需要将该中继指令绑定到一个按钮，即可使用。并且运行程序可以发现，这个取消按钮只有在异步任务执行时才会处于可用状态。

此时XAML的内容如：

```xml
<StackPanel>
    <TextBox Text="{Binding Name"} />
    <Button Content="Submit" Command="{Binding SubmitCommand}" />
    <Button Content="Cancel" Command="{Binding SubmitCancelCommand}" />
</StackPanel>
```

异步任务可以多次发起

如果我们希望异步任务可以多次发起，也就是按钮不需要等待上一个任务结束才能继续点击，那么只需要将特性的 `AllowConcurrentExecutions` 参数设为 `true` 即可。

## 源生成器（Source Generator）

源生成器对项目有一定的要求：

1. 项目文件（.csproj）必须是 SDK 风格的项目文件
2. .NET 版本必须是 .NET 5.0+，或 .NET Standard 2.1+

### 适用于字段（Field）的特性

如果希望一个字段拥有一个名称对应的包含通知功能的属性，只需要为字段添加一个 `ObservableProperty` 特性即可。

```c#
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = "";
}

```

下面是编译器在后台生成的代码。**这里展示的代码是完整版。后面为了简洁，会省略或简化一些行。**

```c#
/// <inheritdoc cref="name"/>
[global::System.CodeDom.Compiler.GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public string Name
{
    get => name;
    [global::System.Diagnostics.CodeAnalysis.MemberNotNull("name")]
    set
    {
        if (!global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(name, value))
        {
            OnNameChanging(value);
            OnNameChanging(default, value);
            OnPropertyChanging(global::CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Name);
            name = value;
            OnNameChanged(value);
            OnNameChanged(default, value);
            OnPropertyChanged(global::CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Name);
        }
    }
}
```

源生成器可以非常智能地识别字段的名称，并生成相应名称的属性。它支持的命名方式有：

- 小写字母开头：`firstName`
- 下划线开头：`_firstName`
- 其他：`m_firstName`

#### 希望通知另一个熟悉

如果希望在字段的值发生变化时，额外通知另一个属性，可以使用 `NotifyPropertyChangedFor` 特性。

```c#
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))] //支持传入多个属性的名称
    private string firstName = ""; //setter 中会生成形如 OnPropertyChanged("FullName") 的代码

    public string FullName => firstName + " " + lastName;
}
```

#### 通知中继指令更新其可用状态

如果某个中级指令（RelayCommand）的 `CanExecute` 与属性有关，并且希望属性的值发生变化后能够通知中继指令更新状态，那么可以使用 `NotifyCanExecuteChangedFor` 特性：

```c#
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FooCommand))] //支持传入多个中继指令的名称
    private bool isFooEnabled; //会在 setter 中添加 FooCommand.NotifyCanExecuteChanged();

    public IRelayCommand FooCommand { get; }
}
```

#### 广播属性值的变化

如果希望在一个 `ObservableRecipient` 对象中广播广播属性值的变化（即调用 `Broadcast` 方法），可以为字段添加 `NotifyPropertyChangedRecipients` 特性：

```c#
public partial class MainViewModel : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private string name = ""; //会在 setter 中添加形如 Broadcast(oldValue, value, "FirstName") 的代码
}
```

#### 验证属性的值是否有效

如果希望在一个 `ObservableValidator` 对象中验证属性的值是否有效，可以为字段添加 `NotifyDataErrorInfo` 特性：

```c#
public partial class SignUpViewModel : ObservableValidator
{
    [Required]
    [EmailAddress]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string email = ""; //会在 setter 中添加 ValidateProperty(value, "Email") 的代码

```

#### 为生成的属性添加特性

如果希望为生成的属性添加额外的特性，尤其比如 `JsonIgnore` 等，从而在序列化时忽略这个属性，可以使用下面这个特殊语法：

```c#
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [property: JsonIgnore]
    private string name = "";
}
```

#### 希望在属性值发生变化后执行额外的逻辑

如果希望在属性值发生变化后执行额外的逻辑，还可以借助分部方法来实现。从 [基本用法](https://mvvm.coldwind.top/Source Generator/FieldAttributes/#基本用法) 的中给出的后台生成的完整代码中可以发现，`setter` 中存在一些分部方法，名称为 `OnChanged。借助这些分部方法，我们就可以实现这个需求。形如：

```c#
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = "";

    partial void OnNameChanged(string value)
    {
        // Do something
    }

    partial void OnNameChanged(string oldValue, string newValue)
    {
        // Do something
    }
}
```

工具包在比较新的版本中为我们提供了可以传入旧值新值的分部方法，它最典型的使用场景是，我们希望在这个属性发生改变后执行一些资源释放相关的逻辑，从而保证旧值可以被 GC 回收。比如：

```c#
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private SubViewModel selectedItem;

    partial void OnSelectedItemChanged(SubViewModel oldValue, SubViewModel newValue)
    {
        // 这里可以对旧值执行一些逻辑，常见的比如：
        oldValue?.PropertyChanged -= OnSelectedItemPropertyChanged;
        oldValue?.Dispose();

        // 然后可以对新值执行一些逻辑，比如：
        newValue?.PropertyChanged += OnSelectedItemPropertyChanged;
    }
}
```



### 适用于方法（Method）的特性

工具包提供的能够为方法添加的特性只有 `RelayCommand`。关于它的用法详见：

- RelayCommand
- AsyncRelayCommand



### 适用于类（Class）的特性

虽然并不多，但是工具包依然为我们提供了一些可以用于（分部）类的特性，包括：

- `ObservableObject`
- `ObservableRecipient`
- `INotifyPropertyChanged`

它们看起来与工具包的几个基类，以及 INPC 接口完全同名（除了它们实际的名称是以 `Attribute` 结尾的）。它们的作用也非常直白，就是让当前类实现 INPC 接口，并为当前类添加这些类的功能，比如：

- `ObservableObject` 会使当前类拥有 `OnPropertyChanged`、`SetProperty` 等方法
- `ObservableRecipient` 会使当前类拥有 `Broadcast` 等方法
- `INotifyPropertyChanged` 会使当前类添加最基本的 `OnPropertyChanged` 方法

（目前工具包并未提供 `ObservableValidator` 特性）

这些特性基本只适用于一种情形：**为一个已经继承了某个基类的类添加通知功能**。比如现在已经有一个 `Student` 类继承了 `Person` 类，但 `Person` 类并没有继承自 `ObservableObject`，此时还希望 `Student` 能够拥有通知功能。这时可以给 `Student` 类改为分部类，并添加这个特性即可。



## 消息中继（Messengers）

Messenger 是一个消息传递机制，它可以让对象之间进行通信，而不需要它们之间有直接的引用关系。从设计模式的角度来说，Messenger 其实是中介者模式的一种实现。

![img](https://img2024.cnblogs.com/blog/3052701/202405/3052701-20240516162623752-792468003.png)

可以看出，在使用中介者模式之后，个模块之间不再呈现出网状结构，而是变成了星状结构。这样一来，模块之间的耦合就会大大降低，代码也会变得更加简洁。

Messenger 在很多 MVVM 框架中均能找到它的身影。虽然叫法不同，但是背后的原理及逻辑都是相通的。这里列举几个常见框架的叫法：

- CommunityToolkit.Mvvm：Messenger（信使）
- Prism：EventAggregator（事件聚合）
- ReactiveUI：MessageBus（消息总线）

设想一下，平常如果想要实现两个 ViewModel 之间的通信（比如 A 调用 B 的方法，或 A 获取 B 中某个属性的值），我们会采用哪些做法呢？一般来说无外乎下面几种：

1. 想办法将 B 的实例的引用传递给 A，比如将 B 的实例保存为 A 的成员（属性或字段），然后在 A 中调用访问这个成员（引用的传递通常在 A 的构造函数中完成）
2. 在 A 中定义一个事件，然后在 B 中订阅这个事件（这里仍然需要想办法传递引用），当 A 中触发这个事件时，B 中的方法就会被调用
3. 为 B 实现单例模式，然后在 A 中通过 `B.Instance` 来调用 B 的方法或获取 B 的属性值

但是这些方法均存在一个问题：**模块之间存在耦合**。比如，如果我们想要将 B 替换成 C，那么就需要修改 A 的代码；更糟的是，如果我们想让 A 能够与更多的类通信，那么就需要在 A 中添加更多的属性、事件或方法。这样一来，类与类之间的耦合就会越来越严重，代码也会变得越来越难以维护。



### Messenger背后的原理

Messenger 背后的原理可以想象成一个字典，它的键是消息的类型，值是一个委托列表。在注册时，我们会将类的对象和一个回调函数（委托）作为值添加到字典中；在发送消息时，我们会根据消息的类型从字典中取出对应的委托列表，然后依次调用这些委托。



### IMessenger

#### IMessenger

`IMessenger` 是工具包中的一个接口，规定了 Messenger 需要实现的一些方法，比如：

- `Register`：注册消息
- `Unregister`：取消注册
- `UnregisterAll`：取消所有注册
- `Send`：发送消息



#### 实现了这一接口的类

工具包中提供了两个实现了 `IMessenger` 接口的类：

- `WeakReferenceMessenger`：使用弱引用，避免内存泄漏
- `StrongReferenceMessenger`：使用强引用，通常用于希望消息接收者不被 GC 回收的场景

一般来说，我们使用 `WeakReferenceMessenger` 即可。



#### 基本用法

Messenger 的使用方法基本上分三步走：

1. 声明一个消息类型，或使用工具包内置的几种类型
2. 注册消息（`IMessneger.Register`）
3. 发送消息（`IMessenger.Send`）

**MessageSender**

```c#
class MessageSender
{
    private readonly IMessenger _messenger;

    public MessageSender(IMessenger messenger)
    {
        _messenger = messenger;
    }

    public void SendMessage()
    {
        // 发送消息
        _messenger.Send(new MyMessage { Content = "Hello, world!" });
    }
}
```

**MessageReceiver**

```c#
class MessageReceiver
{
    private readonly IMessenger _messenger;

    public MessageReceiver(IMessenger messenger)
    {
        _messenger = messenger;

        // 注册指定类型的消息
        _messenger.Register<MyMessage>(this, OnMessageReceived);
    }

    void OnMessageReceived(object recipient, MyMessage message)
    {
        Console.WriteLine($"Message received from {recipient.GetType().Name}: {message.Content}");
    }
}
```

**MyMessage**

```c#
// 一个自定义消息类型
class MyMessage
{
    public string Content { get; init; } = "";
}
```

实例化这些类：

```c#
IMessenger messenger = WeakReferenceMessenger.Default;
var receiver = new MessageReceiver(messenger);
var sender = new MessageSender(messenger);

sender.SendMessage();
```

这里我们使用了依赖注入的方式，将 `IMessenger` 注入到了 `MessageSender` 和 `MessageReceiver` 中。这样做的好处是，我们可以很方便地将 `IMessenger` 替换为其他实现了 `IMessenger` 接口的类，比如 `StrongReferenceMessenger`；同时，在类中，我们也不需要关心 `IMessenger` 的具体实现，只需要知道它是一个 `IMessenger`，以及它提供的方法即可。

### WeakReferenceMessenger

#### 什么是 WeakReference？

在说 `WeakReferenceMessenger` 之前，我们先要搞清楚什么是弱引用（`WeakReference`）。

在 C# 中，我们可以通过 `new WeakReference(obj)` 来创建一个弱引用，这个弱引用会指向 `obj`，但是不会阻止 `obj` 被 GC 回收。

```c#
var obj = new object();
var weakReference = new WeakReference(obj);
```

这一特性在 MVVM 中显得尤为重要。因为我们如果想要借助 `Messenger` 来实现 MVVM 中的消息传递，那么我们就需要在 `ViewModel` 中注册事件。一旦我们没有及时取消注册，那么这个 `ViewModel` 就无法被 GC 回收，这就会导致内存泄漏。而使用 `WeakReferenceMessenger` 就可以避免这个问题。



#### WeakReferenceMessenger

在前面的例子中，我们使用 `WeakReferenceMessenger.Default` 来获取一个默认的 `WeakReferenceMessenger` 实例。这个实例是一个单例，我们可以在任何地方使用它。如果我们的项目比较简单，比如不需要考虑其他 Messenger 类型，且不使用 IoC 容器，那么我们可以直接使用这个单例。

但是，在使用传统的强引用时，我们需要注意内存泄漏的问题。我们需要在代码中合适的位置调用 `Unregister` 方法，以取消注册。



### Messages

#### 常用消息类型Messages

工具包为我们提供了几个常用的消息类型，包括：

- `ValueChangedMessage`
- `PropertyChangedMessage`
- `RequestMessage`



#### 自定义Message

工具包中的 `IMessenger` 接口约定的 `Send`、`Register` 等泛型方法，并没有要求消息的种类满足任何条件（比如继承自某个基类），所以完全可以自由地实现任何消息类型。

比如这里，我们用 C# 9.0 为我们带来的 `record` 类型快速声明一个消息类型，并使用：

```c#
// 声明一个消息类型
public record MyMessage(string Content);

// 发送消息
WeakReferenceMessenger.Default.Send(new MyMessage("Hello World!"));

// 接收消息
WeakReferenceMessenger.Default.Register<MyMessage>(this, message =>
{
    Console.WriteLine(message.Content);
});
```

#### ValueChangedMessage

`ValueChangedMessage` 是一个最基本的消息类型，它包含一个 `Value` 属性，用于存储消息的值。如果希望发送一个用于通知某个值发生变化的消息，可以使用 `ValueChangedMessage`。

此外，还可以继承这个类，从而实现自己的消息类型。比如：

```c#
public class MyMessage : ValueChangedMessage<string>
{
    public MyMessage(string value) : base(value)
    {
    }
}

// 发送消息
WeakReferenceMessenger.Default.Send(new MyMessage("Hello World!"));

// 接收消息
WeakReferenceMessenger.Default.Register<MyMessage>(this, message =>
{
    Console.WriteLine(message.Value);
});
```

#### RequestMessage

这是一个特殊的消息类型。如果 `Send` 方法发送的是这个类（或它的子类），那么 `Send` 方法将拥有一个返回值，这个返回值就是消息的接收者回复的消息。此时，消息的接收者也能够通过消息上的 `Reply` 方法回复消息的发送者。比如：

```c#
// 注册消息接收者
WeakReferenceMessenger.Default.Register<MyRequestMessage>(
    new object(),
    (_, m) =>
    {
        // 收到消息后，会进行简单的判断，并回复消息
        if (m.Content == "Nice to meet you!")
            m.Reply("Nice to meet you, too!");
        else
            m.Reply("Yes?");
    }
);

// 发送消息并查看回复的消息内容
var res1 = WeakReferenceMessenger.Default.Send(new MyRequestMessage("Hello!"));
Console.WriteLine(res1.Response);

var res2 = WeakReferenceMessenger.Default.Send(new MyRequestMessage("Nice to meet you!"));
Console.WriteLine(res2.Response);

// 声明一个自定义消息类型
public class MyRequestMessage : RequestMessage<string>
{
    public string Content { get; init; }

    public MyRequestMessage(string content)
    {
        Content = content;
    }
}
```

#### PropertyChangedMessage

`PropertyChangedMessage` 是一个用于通知属性发生变化的消息类型，它包含一个 `PropertyName` 属性，用于存储属性的名称。如果希望发送一个用于通知某个属性发生变化的消息，可以使用 `PropertyChangedMessage`。这个类通常配合 `ObservableRecipient` 的 `Broadcast` 方法使用。



### Token

#### 使用方法

频道的使用方法非常简单，我们只需要给 `Send` 与 `Register` 两个方法分别添加一个参数即可。这里，我们选择了 `string` 类型的参数，也就是下面代码中出现的 `"token"` 参数。

```c#
var vm = new ViewModel();
WeakReferenceMessenger.Default.Send(new Message("Hello"), "token");
class ViewModel
{
    public ViewModel()
    {
        WeakReferenceMessenger.Default.Register<Message, string>(this, "token",
            (_, m) => { Console.WriteLine(m.Content); });
    }
}
class Message
{
    public Message(string content)
    {
        Content = content;
    }

    public string Content { get; }
}
```

### IRecipient

`IRecipient<>` 接口是一个泛型接口，它的泛型参数是一个 `TMessage` 类型的消息。这个接口定义了一个 `Receive` 方法，用于接收消息。我们可以通过实现这个接口来接收消息，形如：

```c#
public class MyViewModel : ObservableObject, IRecipient<MyMessage>
{
    public void Receive(MyMessage message)
    {
        // 处理消息
    }
}
```

如果这个类需要接收多种 `TMessage` 类型的消息，那么可以实现多个 `IRecipient<>` 接口。

在实现了这样的接口后，我们不需要使用传统的方式去注册这些接收消息的方法，而是可以简单地调用 `Messenger.RegisterAll(this)` 方法，将当前类实现的所有 `IRecipient<>` 接口的方法注册到信使对象中。类似地，我们可以调用 `Messenger.UnregisterAll(this)` 方法来注销这些方法。

例如，这里我们使用 `WeakReferenceMessenger.Default` 作为信使对象。那么，我们的 `ViewModel` 类可以这样实现：

```c#
public class ViewModel : ObservableRecipient, IRecipient<Message>
{
    public ViewModel()
    {
        Messenger.RegisterAll(this);
    }

    public void Receive(Message message)
    {
        // 处理消息
    }
}
```

`RegisterAll` 方法还包含一个重载，允许传入一个 **`Token`** 对象，用于指定消息的频道。类似地，`UnregisterAll` 方法也包含一个重载，允许传入一个 **`Token`** 对象。但如果不传入 `Token` 对象，则默认为不考虑频道，注销当前对象注册的全部消息。

`RegisterAll` 方法写在 `IMessengerExtensions.cs` 中，是对 `IMessenger` 对象的扩展。它的大致逻辑是，通过反射，找到传入的对象中实现了 `IRecipient<>` 接口的所有方法，并将它们注册到信使对象中。

`UnregisterAll` 方法的实现逻辑与 `RegisterAll` 类似，只是将注册改为注销。这个方法是写在具体的类的实现中的，比如 `WeakReferenceMessenger` 类。



# CommunityToolkit.Mvvm 项目详细分析

## 项目概述

CommunityToolkit.Mvvm 是一个 .NET 社区工具包中的 MVVM (Model-View-ViewModel) 框架库，为 .NET 应用程序提供了一系列帮助程序，包括：

- [ObservableObject]: 实现 [INotifyPropertyChanged] 接口的基类
- [ObservableRecipient]: 支持 IMessenger 服务的可观察对象基类
- [ObservableValidator]: 实现 `INotifyDataErrorInfo` 接口的基类
- [RelayCommand]: 实现 `ICommand` 接口的简单委托命令
- [AsyncRelayCommand]: 支持异步操作和取消的委托命令
- [WeakReferenceMessenger]: 通过松散耦合对象交换消息的消息系统
- [StrongReferenceMessenger]: 高性能消息系统，以速度换取弱引用
- [Ioc]: 配置依赖注入服务容器的帮助类



## 项目架构与核心组件

### 1. ObservableObject 基类

[ObservableObject] 是 MVVM 模式中的基础类，实现了 [INotifyPropertyChanged] 和 `INotifyPropertyChanging` 接口。它提供了：

- [OnPropertyChanged] 和 [OnPropertyChanging] 方法来引发事件
- 多个 [SetProperty] 重载方法，用于比较值并自动引发属性更改通知
- 支持任务完成通知的 [SetPropertyAndNotifyOnCompletion] 方法
- [TaskNotifier] 类，用于包装 Task 对象并监听其完成状态



### 2. ObservableProperty 特性

`[ObservableProperty]` 是一个源生成器特性，可以自动为字段生成属性。例如：

```csharp
partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string? name;
}
```

这将自动生成如下代码：
```csharp
public string? Name
{
    get => name;
    set => SetProperty(ref name, value);
}
```



### 3. RelayCommand 命令系统

提供了同步和异步的命令实现：

- [RelayCommand] 和 `RelayCommand<T>`: 用于同步操作
- [AsyncRelayCommand] 和 `AsyncRelayCommand<T>`: 用于异步操作
- 支持 [CanExecute] 逻辑来控制命令是否可执行



### 4. Messenger 消息系统

提供了两种消息传递机制：

- [WeakReferenceMessenger]: 使用弱引用，避免内存泄漏

- [StrongReferenceMessenger]: 高性能但可能导致内存泄漏

- 支持使用令牌（tokens）进行消息通道管理

- 通过 `IRecipient<T>` 接口实现类型安全的消息接收



### 5. ObservableValidator 验证系统

[ObservableValidator] 继承自 [ObservableObject]，并实现了 `INotifyDataErrorInfo` 接口，提供：

- 数据验证支持
- 与 `System.ComponentModel.DataAnnotations` 集成
- 自动验证属性更改
- 支持 [ValidateAllProperties] 方法验证所有属性



### 6. ObservableRecipient 基类

[ObservableRecipient] 继承自 [ObservableObject]，并集成了 [IMessenger] 功能：

- 自动消息订阅和取消订阅
- 提供 [Broadcast] 方法发送属性更改消息
- 支持跨组件通信



## 源代码生成器

项目包含多个源代码生成器，显著提升了开发效率：

1. **RelayCommandGenerator**: 为标记 `[RelayCommand]` 的方法生成命令属性
2. **ObservablePropertyGenerator**: 为字段生成可观察属性
3. **IMessengerRegisterAllGenerator**: 为实现 `IRecipient<T>` 的类型生成批量注册代码
4. **ObservableValidatorValidateAllPropertiesGenerator**: 为 [ObservableValidator] 子类生成 [ValidateAllProperties] 方法



## 高级功能

### 1. 属性依赖通知

- `[NotifyPropertyChangedFor]` 特性: 当属性更改时通知其他属性
- `[NotifyCanExecuteChangedFor]` 特性: 当属性更改时通知命令更新其可执行状态

### 2. 跨平台支持

项目支持多个目标框架：
- .NET Standard 2.0/2.1
- .NET 8.0
- .NET 8.0 Windows (支持 AOT)

### 3. 性能优化

- 使用 `ConditionalWeakTable` 管理实体验证委托
- 通过源生成器避免反射开销
- 提供多个 [SetProperty] 重载以优化性能



## 项目结构

```
CommunityToolkit.Mvvm/
├── ComponentModel/         # ObservableObject、ObservableValidator 等
├── Input/                  # RelayCommand 相关
├── Messaging/              # Messenger 相关
├── Collections/            # 可观察集合
├── Properties/             # 项目属性
└── SourceGenerators/       # 源代码生成器
```



## 特性和优势

1. **现代化**: 使用 C# 8.0+ 特性，支持可空引用类型
2. **高性能**: 通过源生成器减少运行时反射开销
3. **类型安全**: 编译时检查，减少运行时错误
4. **内存友好**: 使用弱引用避免内存泄漏
5. **易用性**: 通过特性简化 MVVM 模式的实现
6. **可扩展**: 支持自定义验证和消息处理



## 适用场景

CommunityToolkit.Mvvm 特别适合：
- WPF、WinUI、MAUI 等 XAML 框架的 MVVM 应用
- 需要复杂数据验证的业务应用
- 需要组件间通信的应用
- 需要命令模式的用户界面
- 要求高性能和类型安全的企业级应用

这个库通过现代化的 C# 特性和源代码生成技术，大大简化了 MVVM 模式的实现，提高了开发效率和代码质量。



---



# CommunityToolkit.Mvvm 项目详细分析

CommunityToolkit.Mvvm 是一个快速、模块化、平台无关的 MVVM 库，是 MvvmLight 的官方继任者。它被广泛用于 Microsoft Store 和其他第一方应用程序中。



### 1. 核心组件

#### ObservableObject
这是 MVVM 模式中实现 [INotifyPropertyChanged] 接口的基类，提供了属性变更通知功能。它包含以下关键方法：
- [OnPropertyChanged()] - 引发属性变更事件
- [OnPropertyChanging()] - 引发属性变更前事件
- [SetProperty()] - 比较并设置属性值，同时触发变更通知
- 支持多种重载，包括强类型比较、自定义比较器等



#### ObservableRecipient
继承自 [ObservableObject]，并添加了消息传递功能：
- 自动管理 [IMessenger] 实例
- 支持 [IsActive] 属性，可激活/停用消息订阅
- 提供 [OnActivated()] 和 [OnDeactivated()] 方法来管理消息订阅
- 提供 [Broadcast()] 方法来广播属性变更消息



#### ObservableValidator
继承自 [ObservableObject]，添加了数据验证功能：
- 实现 `INotifyDataErrorInfo` 接口
- 支持属性级和实体级验证
- 提供 [ValidateProperty()] 方法来验证属性
- 支持 [ValidateAllProperties()] 方法来验证所有属性



### 2. 命令系统

#### RelayCommand
实现了 `ICommand` 接口的命令类：
- 支持同步命令执行
- 可选的 [canExecute] 条件检查
- 支持泛型和非泛型版本
- 提供 [NotifyCanExecuteChanged()] 方法来通知命令执行状态变更

#### AsyncRelayCommand
支持异步操作的命令类：
- 实现 [IAsyncRelayCommand] 接口
- 支持取消操作
- 提供 [ExecutionTask] 属性跟踪异步操作
- 支持 [IsRunning]、[CanBeCanceled]、[IsCancellationRequested] 等状态属性
- 可配置并发执行和异常处理选项



### 3. 消息传递系统

#### IMessenger 接口
提供对象间消息交换功能：
- 支持弱引用和强引用消息传递器
- 可使用令牌在特定通道上发送消息
- 支持泛型消息类型

#### WeakReferenceMessenger
- 默认使用弱引用的消息传递器
- 防止内存泄漏
- 自动垃圾回收清理

#### StrongReferenceMessenger
- 使用强引用的消息传递器
- 更高性能，但需要手动管理生命周期



### 4. 源生成器和特性

#### ObservablePropertyAttribute
- 用于字段或属性，自动生成通知逻辑
- 源生成器会在编译时生成相应的属性和通知代码

#### RelayCommandAttribute
- 用于方法，自动生成命令属性
- 支持配置 [CanExecute] 方法、并发执行、异常处理等选项



### 5. 项目结构

项目包含以下主要模块：

- **Collections**: 包含 [ObservableGroupedCollection] 等集合类型
- **ComponentModel**: 包含 [ObservableObject]、[ObservableRecipient]、[ObservableValidator] 等核心类型
- **Input**: 包含 [RelayCommand]、[AsyncRelayCommand] 等命令类型
- **Messaging**: 包含消息传递系统
- **DependencyInjection**: 包含依赖注入辅助工具



### 6. 特性

- **多目标框架支持**: 支持 .NET Standard 2.0, .NET Standard 2.1, .NET 8.0 等
- **AOT 友好**: 针对 AOT 编译环境进行了优化
- **源生成器支持**: 通过源生成器减少运行时反射开销
- **内存效率**: 使用弱引用消息传递器避免内存泄漏
- **类型安全**: 大量使用泛型确保类型安全
- **高性能**: 优化的内部实现，避免不必要的内存分配



### 7. 使用场景

这个库特别适合：
- WPF、WinUI、MAUI 等 XAML UI 框架
- 需要解耦的模块化应用程序
- 需要属性变更通知的数据绑定场景
- 需要命令模式的 UI 交互场景
- 需要松耦合通信的消息传递场景

CommunityToolkit.Mvvm 提供了一个完整的 MVVM 框架，使开发者能够构建结构清晰、易于维护的应用程序，同时保持高性能和类型安全。




---



# 源生成器实现机制详解

源生成器是一种编译时技术，它可以在编译时生成代码，从而提高代码的效率和可维护性。
我将详细分析 CommunityToolkit.Mvvm 的源生成器是如何实现的，包括它们在编译时如何生成属性和通知代码。

### 1. ObservableProperty 源生成器

CommunityToolkit.Mvvm 使用 Roslyn 源生成器技术在编译时自动生成代码。以下是其实现机制：

#### ObservablePropertyGenerator 类
- 这是处理 [ObservablePropertyAttribute](.\CommunityToolkit.Mvvm\ComponentModel\Attributes\ObservablePropertyAttribute.cs#L69-L72) 的主要源生成器,命名空间CommunityToolkit.Mvvm.ComponentModel
- 实现 `IIncrementalGenerator` 接口，利用 Roslyn 的增量生成机制
- 通过 [RelayCommandGenerator.Initialize](.\CommunityToolkit.Mvvm.SourceGenerators\Input\RelayCommandGenerator.cs#L22-L74) 方法注册处理逻辑,命名空间CommunityToolkit.Mvvm.SourceGenerators

#### 工作流程：
1. **发现阶段**：扫描所有带有 `[ObservableProperty]` 特性的字段或属性
2. **验证阶段**：检查语法和语义的有效性（如字段类型、宿主类等）
3. **信息提取阶段**：从源代码中提取必要信息，如字段名、类型、相关属性等
4. **代码生成阶段**：根据提取的信息生成完整的属性代码

#### 生成的代码结构：
当开发者在字段上使用 `[ObservableProperty]` 特性时，例如：

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string? name;
}
```

源生成器会自动生成以下代码：

```csharp
partial class MyViewModel
{
    public string? Name
    {
        get => name;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string?>.Default.Equals(name, value))
            {
                string? __oldValue = name;
                OnNameChanging(value);
                OnNameChanging(__oldValue, value);
                name = value;
                OnNameChanged(value);
                OnNameChanged(__oldValue, value);
            }
        }
    }
    
    partial void OnNameChanging(string? value);
    partial void OnNameChanging(string? oldValue, string? newValue);
    partial void OnNameChanged(string? value);
    partial void OnNameChanged(string? oldValue, string? newValue);
}
```

### 2. RelayCommand 源生成器

#### RelayCommandGenerator 类
- 处理 `[RelayCommand]` 特性的方法
- 生成对应的命令属性和命令实现

#### 生成的命令代码：
当开发者在方法上使用 `[RelayCommand]` 特性时，例如：

```csharp
public partial class MyViewModel : ObservableObject
{
    [RelayCommand]
    private void Increment()
    {
        Counter++;
    }
}
```

源生成器会生成：

```csharp
partial class MyViewModel
{
    private global::CommunityToolkit.Mvvm.Input.RelayCommand? incrementCommand;

    public global::CommunityToolkit.Mvvm.Input.IRelayCommand IncrementCommand => incrementCommand ??= new global::CommunityToolkit.Mvvm.Input.RelayCommand(new global::System.Action(this.Increment));
}
```

### 3. 核心实现技术

#### 增量生成
- 使用 `IIncrementalGenerator` 接口实现增量编译
- 只在源代码实际改变时重新生成代码
- 提高编译效率

#### 语法树操作
- 使用 Roslyn 的 `SyntaxFactory` 类创建语法节点
- 生成符合 C# 语言规范的代码
- 保持与手动编写代码相同的性能

#### 诊断报告
- 提供详细的编译时错误和警告信息
- 帮助开发者快速识别和修复问题
- 实现自定义分析器检查代码模式

### 4. 特殊功能支持

#### 属性依赖通知
- 自动检测 `[NotifyPropertyChangedFor]` 特性
- 生成对其他属性变更的通知代码

#### 命令依赖通知
- 处理 `[NotifyCanExecuteChangedFor]` 特性
- 在属性变更时自动调用 [NotifyCanExecuteChanged()](.\CommunityToolkit.Mvvm\Input\RelayCommand{T}.cs#L69-L72)

#### 数据验证支持
- 与 [ObservableValidator](.\CommunityToolkit.Mvvm\ComponentModel\ObservableValidator.cs#L21-L816) 集成
- 生成验证相关代码

### 5. 编译时优化

#### 避免运行时反射
- 所有代码在编译时生成
- 运行时无需反射调用
- 性能接近手写代码

#### 预编译常量
- 使用预生成的参数对象减少重复创建
- 优化 [OnPropertyChanging](.\CommunityToolkit.Mvvm\ComponentModel\ObservableObject.cs#L55-L66) 和 [OnPropertyChanged](.\CommunityToolkit.Mvvm\ComponentModel\ObservableObject.cs#L43-L48) 调用

通过这种方式，CommunityToolkit.Mvvm 在保持代码简洁性的同时，提供了强大的 MVVM 模式支持，而且没有运行时性能损失。开发者只需添加特性，源生成器会自动生成完整的属性通知和命令实现代码。



---


# CommunityToolkit.Mvvm 引用了多个不同 Roslyn 版本

我注意到在项目文件中，CommunityToolkit.Mvvm 引用了多个不同 Roslyn 版本的源生成器项目。这表明该库为了兼容不同版本的编译器而采取了多目标策略。

CommunityToolkit.Mvvm 项目使用了一种高级的多目标策略来支持不同版本的 Roslyn 编译器。让我总结一下这个机制：

## Roslyn 多版本支持机制

CommunityToolkit.Mvvm 项目通过以下方式实现对不同版本 Roslyn 编译器的支持：

### 1. 多版本源生成器项目
项目为不同的 Roslyn 版本创建了独立的项目：
- `CommunityToolkit.Mvvm.SourceGenerators.Roslyn4001` (对应 Roslyn 4.0.1)
- `CommunityToolkit.Mvvm.SourceGenerators.Roslyn4031` (对应 Roslyn 4.0.31)
- `CommunityToolkit.Mvvm.SourceGenerators.Roslyn4120` (对应 Roslyn 4.12.0)
- `CommunityToolkit.Mvvm.SourceGenerators.Roslyn5000` (对应 Roslyn 5.0.0)

### 2. 共享源代码
所有这些项目都共享相同的源代码文件（通过 [.projitems](.\CommunityToolkit.Mvvm.SourceGenerators\CommunityToolkit.Mvvm.SourceGenerators.projitems) 文件）：
```xml
<Import Project="..\CommunityToolkit.Mvvm.SourceGenerators\CommunityToolkit.Mvvm.SourceGenerators.projitems" Label="Shared" />
```

### 3. 版本特定的配置
通过 [CommunityToolkit.Mvvm.SourceGenerators.props](.\CommunityToolkit.Mvvm.SourceGenerators\CommunityToolkit.Mvvm.SourceGenerators.props) 文件，项目实现了版本特定的配置：

- **版本检测**：从项目名称中提取 Roslyn 版本号
- **条件编译**：定义 `ROSLYN_4_3_1_OR_GREATER`、`ROSLYN_4_12_0_OR_GREATER`、`ROSLYN_5_0_0_OR_GREATER` 等预处理器符号
- **包引用**：使用匹配版本的 `Microsoft.CodeAnalysis.CSharp` 包

### 4. 条件编译在源代码中的应用

在源代码中，使用条件编译来支持不同版本的 Roslyn 功能，例如：

```csharp
#if ROSLYN_4_12_0_OR_GREATER
    // 针对 Roslyn 4.12.0 及以上版本的代码
    // 支持部分属性等新功能
#endif
```

### 5. 优势

这种多版本支持策略提供了以下优势：

- **向后兼容性**：确保在旧版本的 Visual Studio 或 .NET SDK 中也能正常工作
- **向前兼容性**：利用新版本 Roslyn 的功能和性能改进
- **统一代码库**：所有版本共享相同的源代码逻辑，只在必要时进行条件编译
- **优化性能**：针对不同版本的编译器进行特定优化

### 6. NuGet 包结构

在最终的 NuGet 包中，这些不同版本的源生成器会被放置在对应的 Roslyn 版本文件夹中，NuGet 会根据消费者的编译器版本自动选择合适的源生成器版本。

这种设计确保了 CommunityToolkit.Mvvm 可以在各种开发环境中正常工作，无论开发者使用的是哪个版本的 Visual Studio 或 .NET SDK。