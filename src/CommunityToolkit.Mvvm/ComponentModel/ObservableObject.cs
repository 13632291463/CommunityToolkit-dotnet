// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is inspired from the MvvmLight library (lbugnion/MvvmLight),
// more info in ThirdPartyNotices.txt in the root of the project.

// ================================== NOTE ==================================
// This file is mirrored in the trimmed-down INotifyPropertyChanged file in
// the source generator project, to be used with the [INotifyPropertyChanged],
// attribute, along with the ObservableObject annotated copy (for debugging info).
// If any changes are made to this file, they should also be appropriately
// ported to that file as well to keep the behavior consistent.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

#pragma warning disable CS0618

namespace CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// 一个基类，用于需要属性可观察的对象
/// ObservableObject 除了实现了 INotifyPropertyChanged 接口之外，还提供了一些方法来帮助我们实现通知功能。
///  - OnPropertyChanged（拥有两个方法重载）
///  - SetProperty（包含六个方法重载）
///  - SetPropertyAndNotifyOnCompletion（包含五个重载，用于 TaskNotifier）
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc cref="INotifyPropertyChanging.PropertyChanging"/>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <summary>
    /// 引发 <see cref="PropertyChanged"/> 事件
    /// </summary>
    /// <param name="e">输入的 <see cref="PropertyChangedEventArgs"/> 实例</param>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="e"/> 为 <see langword="null"/> 时抛出</exception>
    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        PropertyChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 引发 <see cref="PropertyChanging"/> 事件
    /// </summary>
    /// <param name="e">输入的 <see cref="PropertyChangingEventArgs"/> 实例</param>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="e"/> 为 <see langword="null"/> 时抛出</exception>
    protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // 当支持被禁用时，什么都不做
        if (!FeatureSwitches.EnableINotifyPropertyChangingSupport)
        {
            return;
        }

        PropertyChanging?.Invoke(this, e);
    }

    /// <summary>
    /// 引发 <see cref="PropertyChanged"/> 事件
    /// </summary>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 引发 <see cref="PropertyChanging"/> 事件
    /// </summary>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    protected void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        // 当支持被禁用时，完全避免实例化事件参数
        if (!FeatureSwitches.EnableINotifyPropertyChangingSupport)
        {
            return;
        }

        OnPropertyChanging(new PropertyChangingEventArgs(propertyName));
    }

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，引发 <see cref="PropertyChanging"/> 事件，
    /// 用新值更新属性，然后引发 <see cref="PropertyChanged"/> 事件
    /// </summary>
    /// <typeparam name="T">发生更改的属性类型</typeparam>
    /// <param name="field">存储属性值的字段</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <remarks>
    /// 如果目标属性的当前值和新值相同，则不会引发 <see cref="PropertyChanging"/> 和 <see cref="PropertyChanged"/> 事件
    /// </remarks>
    protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
    {
        // 我们在这里复制代码而不是调用重载，因为我们不能保证
        // 调用的 SetProperty<T> 会被内联，并且我们需要 JIT
        // 能够看到完整的 EqualityComparer<T>.Default.Equals 调用，以便
        // 它会在可能时使用其内部版本，仅将整个调用替换为
        // 直接比较(例如对于基本数值类型)。
        // 这是最快的 SetProperty<T> 重载，所以我们特别关注
        // 这里的代码生成质量，而且代码很小且简单，所以
        // 复制它仍然不会使整个类更难以维护
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }

        OnPropertyChanging(propertyName);

        field = newValue;

        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，引发 <see cref="PropertyChanging"/> 事件，
    /// 用新值更新属性，然后引发 <see cref="PropertyChanged"/> 事件
    /// 有关此重载的更多说明，请参见 <see cref="SetProperty{T}(ref T,T,string)"/>
    /// </summary>
    /// <typeparam name="T">发生更改的属性类型</typeparam>
    /// <param name="field">存储属性值的字段</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="comparer">用于比较输入值的 <see cref="IEqualityComparer{T}"/> 实例</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="comparer"/> 为 <see langword="null"/> 时抛出</exception>
    protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, IEqualityComparer<T> comparer, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(comparer);

        if (comparer.Equals(field, newValue))
        {
            return false;
        }

        OnPropertyChanging(propertyName);

        field = newValue;

        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，引发 <see cref="PropertyChanging"/> 事件，
    /// 用新值更新属性，然后引发 <see cref="PropertyChanged"/> 事件
    /// 此重载比 <see cref="SetProperty{T}(ref T,T,string)"/> 效率低得多，仅应在前者不可行时使用
    /// (例如，当目标属性不直接暴露可以通过引用传递的后备字段时)
    /// 出于性能原因，建议尽可能使用状态回调，通过 <see cref="SetProperty{TModel,T}(T,T,TModel,Action{TModel,T},string?)"/>
    /// 而不是此重载，因为这将允许 C# 编译器缓存输入回调并减少内存分配
    /// 有关该重载的更多信息，请参见相关的 XML 文档
    /// 此重载在此是为了完整性，并在不适用上述情况时使用
    /// </summary>
    /// <typeparam name="T">发生更改的属性类型</typeparam>
    /// <param name="oldValue">当前属性值</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="callback">用于更新属性值的回调</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <remarks>
    /// 如果目标属性的当前值和新值相同，则不会引发 <see cref="PropertyChanging"/> 和 <see cref="PropertyChanged"/> 事件
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="callback"/> 为 <see langword="null"/> 时抛出</exception>
    protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(callback);

        // 我们避免再次调用重载以确保比较被内联
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            return false;
        }

        OnPropertyChanging(propertyName);

        callback(newValue);

        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，引发 <see cref="PropertyChanging"/> 事件，
    /// 用新值更新属性，然后引发 <see cref="PropertyChanged"/> 事件
    /// 有关此重载的更多说明，请参见 <see cref="SetProperty{T}(T,T,Action{T},string)"/>
    /// </summary>
    /// <typeparam name="T">发生更改的属性类型</typeparam>
    /// <param name="oldValue">当前属性值</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="comparer">用于比较输入值的 <see cref="IEqualityComparer{T}"/> 实例</param>
    /// <param name="callback">用于更新属性值的回调</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="comparer"/> 或 <paramref name="callback"/> 为 <see langword="null"/> 时抛出</exception>
    protected bool SetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(callback);

        if (comparer.Equals(oldValue, newValue))
        {
            return false;
        }

        OnPropertyChanging(propertyName);

        callback(newValue);

        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// 比较给定嵌套属性的当前值和新值。如果值已更改，引发 <see cref="PropertyChanging"/> 事件，
    /// 更新属性，然后引发 <see cref="PropertyChanged"/> 事件
    /// 行为与 <see cref="SetProperty{T}(ref T,T,string)"/> 相同，不同之处在于此方法用于中继
    /// 当前实例中包装模型的属性
    /// 此类型在创建对不支持通知的模型进行操作的包装、可绑定对象时很有用(例如，用于 CRUD 操作)
    /// 假设我们有这个模型(例如，表中数据库行):
    /// <code>
    /// public class Person
    /// {
    ///     public string Name { get; set; }
    /// }
    /// </code>
    /// 然后我们可以使用属性将此类型实例包装到我们的可观察模型中(支持通知)，
    /// 将通知注入到该模型的属性，如下所示:
    /// <code>
    /// public class BindablePerson : ObservableObject
    /// {
    ///     public Model { get; }
    ///
    ///     public BindablePerson(Person model)
    ///     {
    ///         Model = model;
    ///     }
    ///
    ///     public string Name
    ///     {
    ///         get => Model.Name;
    ///         set => Set(Model.Name, value, Model, (model, name) => model.Name = name);
    ///     }
    /// }
    /// </code>
    /// 这样我们就可以在应用程序中使用包装对象，所有这些"代理"属性也将在更改时引发通知
    /// 请注意，此方法不是 <see cref="SetProperty{T}(ref T,T,string)"/> 的替代品，
    /// 它应该仅在中继不支持通知的模型的属性时使用，
    /// 并且仅在您无法直接为该模型实现通知时使用(例如，通过让其继承自 <see cref="ObservableObject"/>)
    /// 语法依赖于传递目标模型和无状态回调，以允许 C# 编译器缓存函数，
    /// 从而获得更好的性能和无内存使用
    /// </summary>
    /// <typeparam name="TModel">其属性(或字段)要设置的模型类型</typeparam>
    /// <typeparam name="T">要设置的属性(或字段)类型</typeparam>
    /// <param name="oldValue">当前属性值</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="model">包含要更新的属性的模型</param>
    /// <param name="callback">在发生更改时调用以设置目标属性值的回调</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <remarks>
    /// 如果目标属性的当前值和新值相同，则不会引发 <see cref="PropertyChanging"/> 和 <see cref="PropertyChanged"/> 事件
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="model"/> 或 <paramref name="callback"/> 为 <see langword="null"/> 时抛出</exception>
    protected bool SetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, [CallerMemberName] string? propertyName = null)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(callback);

        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            return false;
        }

        OnPropertyChanging(propertyName);

        callback(model, newValue);

        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// 比较给定嵌套属性的当前值和新值。如果值已更改，引发 <see cref="PropertyChanging"/> 事件，
    /// 更新属性，然后引发 <see cref="PropertyChanged"/> 事件
    /// 行为与 <see cref="SetProperty{T}(ref T,T,string)"/> 相同，不同之处在于此方法用于中继
    /// 当前实例中包装模型的属性
    /// 有关此重载的更多说明，请参见 <see cref="SetProperty{TModel,T}(T,T,TModel,Action{TModel,T},string)"/>
    /// </summary>
    /// <typeparam name="TModel">其属性(或字段)要设置的模型类型</typeparam>
    /// <typeparam name="T">要设置的属性(或字段)类型</typeparam>
    /// <param name="oldValue">当前属性值</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="comparer">用于比较输入值的 <see cref="IEqualityComparer{T}"/> 实例</param>
    /// <param name="model">包含要更新的属性的模型</param>
    /// <param name="callback">在发生更改时调用以设置目标属性值的回调</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="comparer"/>、<paramref name="model"/> 或 <paramref name="callback"/> 为 <see langword="null"/> 时抛出</exception>
    protected bool SetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, [CallerMemberName] string? propertyName = null)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(callback);

        if (comparer.Equals(oldValue, newValue))
        {
            return false;
        }

        OnPropertyChanging(propertyName);

        callback(model, newValue);

        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// 比较给定字段的当前值和新值(应该是属性的后备字段)
    /// 如果值已更改，引发 <see cref="PropertyChanging"/> 事件，更新字段，
    /// 然后引发 <see cref="PropertyChanged"/> 事件
    /// 行为与 <see cref="SetProperty{T}(ref T,T,string)"/> 相同，不同之处在于此方法还将
    /// 监控属性的新值(一个泛型 <see cref="Task"/>)，并在其完成时再次为该目标属性引发 <see cref="PropertyChanged"/>
    /// 这可以用于更新绑定到该 <see cref="Task"/> 或其任何属性的绑定
    /// 此方法及其重载专门依赖 <see cref="TaskNotifier"/> 类型，需要在目标 <see cref="Task"/> 属性的后备字段中使用
    /// 该字段不需要初始化，因为此方法将自动处理
    /// <see cref="TaskNotifier"/> 类型还包括一个隐式运算符，因此可以直接分配给任何 <see cref="Task"/> 实例
    /// 以下是使用此方法的属性声明示例:
    /// <code>
    /// private TaskNotifier myTask;
    ///
    /// public Task MyTask
    /// {
    ///     get => myTask;
    ///     private set => SetAndNotifyOnCompletion(ref myTask, value);
    /// }
    /// </code>
    /// </summary>
    /// <param name="taskNotifier">要修改的字段通知器</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <remarks>
    /// 如果目标属性的当前值和新值相同，则不会引发 <see cref="PropertyChanging"/> 和 <see cref="PropertyChanged"/> 事件
    /// 返回值为 <see langword="true"/> 仅表示分配给 <paramref name="taskNotifier"/> 的新值与前一个值不同，
    /// 并不意味着作为参数传递的新 <see cref="Task"/> 实例处于任何特定状态
    /// </remarks>
    protected bool SetPropertyAndNotifyOnCompletion([NotNull] ref TaskNotifier? taskNotifier, Task? newValue, [CallerMemberName] string? propertyName = null)
    {
        // 我们在此调用带回调的重载以避免代码重复，只需传递一个空回调
        // C# 编译器将此处的 lambda 表达式转换为空闭包类，
        // 该类包含一个包含闭包实例的静态单例字段，
        // 以及另一个缓存 Action<TTask> 实例的字段
        // 这将在首次调用此方法时产生分配成本，但此后不会
        // 泛型类型。我们将支付委托的虚拟调用成本，但这不是性能关键代码，
        // 而且该开销仍然远低于方法的其余部分，所以没关系
        return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier(), newValue, null, propertyName);
    }

    /// <summary>
    /// 比较给定字段的当前值和新值(应该是属性的后备字段)
    /// 如果值已更改，引发 <see cref="PropertyChanging"/> 事件，更新字段，
    /// 然后引发 <see cref="PropertyChanged"/> 事件
    /// 此方法与 <see cref="SetPropertyAndNotifyOnCompletion(ref TaskNotifier,Task,string)"/> 相同，
    /// 不同之处在于额外的 <see cref="Action{T}"/> 参数，
    /// 该参数包含一个回调，在新任务已完成或为 <see langword="null"/> 时立即调用，或在完成时调用
    /// </summary>
    /// <param name="taskNotifier">要修改的字段通知器</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="callback">用于更新属性值的回调</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <remarks>
    /// 如果目标属性的当前值和新值相同，则不会引发 <see cref="PropertyChanging"/> 和 <see cref="PropertyChanged"/> 事件
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="callback"/> 为 <see langword="null"/> 时抛出</exception>
    protected bool SetPropertyAndNotifyOnCompletion([NotNull] ref TaskNotifier? taskNotifier, Task? newValue, Action<Task?> callback, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier(), newValue, callback, propertyName);
    }

    /// <summary>
    /// 比较给定字段的当前值和新值(应该是属性的后备字段)
    /// 如果值已更改，引发 <see cref="PropertyChanging"/> 事件，更新字段，
    /// 然后引发 <see cref="PropertyChanged"/> 事件
    /// 行为与 <see cref="SetProperty{T}(ref T,T,string)"/> 相同，不同之处在于此方法还将
    /// 监控属性的新值(一个泛型 <see cref="Task"/>)，并在其完成时再次为该目标属性引发 <see cref="PropertyChanged"/>
    /// 这可以用于更新绑定到该 <see cref="Task"/> 或其任何属性的绑定
    /// 此方法及其重载专门依赖 <see cref="TaskNotifier{T}"/> 类型，需要在目标 <see cref="Task"/> 属性的后备字段中使用
    /// 该字段不需要初始化，因为此方法将自动处理
    /// <see cref="TaskNotifier{T}"/> 类型还包括一个隐式运算符，因此可以直接分配给任何 <see cref="Task"/> 实例
    /// 以下是使用此方法的属性声明示例:
    /// <code>
    /// private TaskNotifier&lt;int&gt; myTask;
    ///
    /// public Task&lt;int&gt; MyTask
    /// {
    ///     get => myTask;
    ///     private set => SetAndNotifyOnCompletion(ref myTask, value);
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T"><see cref="Task{TResult}"/> 的结果类型，用于设置和监视</typeparam>
    /// <param name="taskNotifier">要修改的字段通知器</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <remarks>
    /// 如果目标属性的当前值和新值相同，则不会引发 <see cref="PropertyChanging"/> 和 <see cref="PropertyChanged"/> 事件
    /// 返回值为 <see langword="true"/> 仅表示分配给 <paramref name="taskNotifier"/> 的新值与前一个值不同，
    /// 并不意味着作为参数传递的新 <see cref="Task{TResult}"/> 实例处于任何特定状态
    /// </remarks>
    protected bool SetPropertyAndNotifyOnCompletion<T>([NotNull] ref TaskNotifier<T>? taskNotifier, Task<T>? newValue, [CallerMemberName] string? propertyName = null)
    {
        return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier<T>(), newValue, null, propertyName);
    }

    /// <summary>
    /// 比较给定字段的当前值和新值(应该是属性的后备字段)
    /// 如果值已更改，引发 <see cref="PropertyChanging"/> 事件，更新字段，
    /// 然后引发 <see cref="PropertyChanged"/> 事件
    /// 此方法与 <see cref="SetPropertyAndNotifyOnCompletion{T}(ref TaskNotifier{T},Task{T},string)"/> 相同，
    /// 不同之处在于额外的 <see cref="Action{T}"/> 参数，
    /// 该参数包含一个回调，在新任务已完成或为 <see langword="null"/> 时立即调用，或在完成时调用
    /// </summary>
    /// <typeparam name="T"><see cref="Task{TResult}"/> 的结果类型，用于设置和监视</typeparam>
    /// <param name="taskNotifier">要修改的字段通知器</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="callback">用于更新属性值的回调</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    /// <remarks>
    /// 如果目标属性的当前值和新值相同，则不会引发 <see cref="PropertyChanging"/> 和 <see cref="PropertyChanged"/> 事件
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">当 <paramref name="callback"/> 为 <see langword="null"/> 时抛出</exception>
    protected bool SetPropertyAndNotifyOnCompletion<T>([NotNull] ref TaskNotifier<T>? taskNotifier, Task<T>? newValue, Action<Task<T>?> callback, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier<T>(), newValue, callback, propertyName);
    }

    /// <summary>
    /// 为相关方法实现通知逻辑
    /// </summary>
    /// <typeparam name="TTask">要设置和监视的 <see cref="Task"/> 类型</typeparam>
    /// <param name="taskNotifier">字段通知器</param>
    /// <param name="newValue">更改发生后属性的值</param>
    /// <param name="callback">(可选) 用于更新属性值的回调</param>
    /// <param name="propertyName">(可选) 发生更改的属性名称</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    private bool SetPropertyAndNotifyOnCompletion<TTask>(ITaskNotifier<TTask> taskNotifier, TTask? newValue, Action<TTask?>? callback, [CallerMemberName] string? propertyName = null)
        where TTask : Task
    {
        if (ReferenceEquals(taskNotifier.Task, newValue))
        {
            return false;
        }

        // 检查新任务的状态，然后将其分配给目标字段
        // 这样，如果任务为 null 或已完成，
        // 我们可以避免监视其完成的开销
        bool isAlreadyCompletedOrNull = newValue?.IsCompleted ?? true;

        OnPropertyChanging(propertyName);

        taskNotifier.Task = newValue;

        OnPropertyChanged(propertyName);

        // 如果输入任务为 null 或已完成，我们不需要执行
        // 监视其完成的额外逻辑，因此我们可以绕过
        // 方法的其余部分并返回字段已更改
        // 这不会指示任务本身已完成，而只是
        // 属性值本身已更改(即引用的任务实例已更改)
        // 这与所有其他同步 Set 方法的返回值相匹配
        if (isAlreadyCompletedOrNull)
        {
            if (callback is not null)
            {
                callback(newValue);
            }

            return true;
        }

        // 我们在此处使用本地异步函数，以便主方法可以
        // 保持同步并返回可以立即使用的值
        // 由调用者。这与 Set<T>(ref T, T, string) 相匹配
        // 我们使用异步 void 函数而不是返回任务的函数
        // 因此，如果绑定更新导致的属性更改通知
        // 导致崩溃，它会在应用程序中立即报告，而不是
        // 异常被忽略(因为返回的任务不会被等待)，
        // 这会导致用户的混淆行为
        async void MonitorTask()
        {
            // 等待任务并忽略任何异常
            await newValue!.GetAwaitableWithoutEndValidation();

            // 仅在属性未更改时通知
            if (ReferenceEquals(taskNotifier.Task, newValue))
            {
                OnPropertyChanged(propertyName);
            }

            if (callback is not null)
            {
                callback(newValue);
            }
        }

        MonitorTask();

        return true;
    }

    /// <summary>
    /// 指定类型的任务通知器接口
    /// </summary>
    /// <typeparam name="TTask">要存储的值类型</typeparam>
    private interface ITaskNotifier<TTask>
        where TTask : Task
    {
        /// <summary>
        /// 获取或设置包装的 <typeparamref name="TTask"/> 值
        /// </summary>
        TTask? Task { get; set; }
    }

    /// <summary>
    /// 可以保存 <see cref="Task"/> 值的包装类
    /// </summary>
    protected sealed class TaskNotifier : ITaskNotifier<Task>
    {
        /// <summary>
        /// 初始化 <see cref="TaskNotifier"/> 类的新实例
        /// </summary>
        internal TaskNotifier()
        {
        }

        private Task? task;

        /// <inheritdoc/>
        Task? ITaskNotifier<Task>.Task
        {
            get => this.task;
            set => this.task = value;
        }

        /// <summary>
        /// 解包当前实例中存储的 <see cref="Task"/> 值
        /// </summary>
        /// <param name="notifier">输入的 <see cref="TaskNotifier{TTask}"/> 实例</param>
        public static implicit operator Task?(TaskNotifier? notifier)
        {
            return notifier?.task;
        }
    }

    /// <summary>
    /// 可以保存 <see cref="Task{T}"/> 值的包装类
    /// </summary>
    /// <typeparam name="T">用于包装的 <see cref="Task{T}"/> 实例的值类型</typeparam>
    protected sealed class TaskNotifier<T> : ITaskNotifier<Task<T>>
    {
        /// <summary>
        /// 初始化 <see cref="TaskNotifier{TTask}"/> 类的新实例
        /// </summary>
        internal TaskNotifier()
        {
        }

        private Task<T>? task;

        /// <inheritdoc/>
        Task<T>? ITaskNotifier<Task<T>>.Task
        {
            get => this.task;
            set => this.task = value;
        }

        /// <summary>
        /// 解包当前实例中存储的 <see cref="Task{T}"/> 值
        /// </summary>
        /// <param name="notifier">输入的 <see cref="TaskNotifier{TTask}"/> 实例</param>
        public static implicit operator Task<T>?(TaskNotifier<T>? notifier)
        {
            return notifier?.task;
        }
    }
}