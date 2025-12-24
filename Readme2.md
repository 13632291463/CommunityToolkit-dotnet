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

---

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
├── ComponentModel/          # ObservableObject、ObservableValidator 等
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