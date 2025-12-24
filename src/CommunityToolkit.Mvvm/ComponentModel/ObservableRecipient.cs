// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is inspired from the MvvmLight library (lbugnion/MvvmLight),
// more info in ThirdPartyNotices.txt in the root of the project.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// 一个可观察对象的基类，同时也作为消息接收者。此类是 <see cref="ObservableObject"/> 的扩展，
/// 还提供了使用 <see cref="IMessenger"/> 类型的内置支持。
/// </summary>
public abstract class ObservableRecipient : ObservableObject
{
    /// <summary>
    /// 初始化 <see cref="ObservableRecipient"/> 类的新实例。
    /// </summary>
    /// <remarks>
    /// 此构造函数将生成一个使用 <see cref="WeakReferenceMessenger.Default"/> 实例
    /// 来执行请求操作的实例。它也将通过 <see cref="Messenger"/> 属性本地可用。
    /// </remarks>
    protected ObservableRecipient()
        : this(WeakReferenceMessenger.Default)
    {
    }

    /// <summary>
    /// 初始化 <see cref="ObservableRecipient"/> 类的新实例。
    /// </summary>
    /// <param name="messenger">用于发送消息的 <see cref="IMessenger"/> 实例。</param>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="messenger"/> 为 <see langword="null"/> 则抛出。</exception>
    protected ObservableRecipient(IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(messenger);

        Messenger = messenger;
    }

    /// <summary>
    /// 获取正在使用的 <see cref="IMessenger"/> 实例。
    /// </summary>
    protected IMessenger Messenger { get; }

    private bool isActive;

    /// <summary>
    /// 获取或设置一个值，指示当前视图模型是否处于活动状态。
    /// </summary>
    public bool IsActive
    {
        get => this.isActive;

/*
        [RequiresUnreferencedCode(
            "当此属性设置为 true 时，将调用 OnActivated() 方法，该方法将为此接收者注册所有必要的消息处理程序。 " +
            "此方法要求生成的 CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions 类型不被移除以使用快速路径。 " +
            "如果此类型被链接器移除，或者如果目标接收者是动态创建的并且被源生成器遗漏，则将使用编译的 LINQ 表达式的较慢回退路径。 " +
            "这将在任何给定接收者类型的第一次调用中产生更多开销。 " +
            "或者，可以手动重写 OnActivated()，并为该接收者单独注册每个必需的消息。")]
        [RequiresDynamicCode(
            "当此属性设置为 true 时，将调用 OnActivated() 方法，该方法将为此接收者注册所有必要的消息处理程序。 " +
            "此方法要求生成的 CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions 类型不被移除以使用快速路径。 " +
            "如果存在，则该方法是 AOT 安全的，因为调用的唯一方法将是源生成器生成的消息注册方法。 " +
            "如果没有，此方法将需要动态创建通用方法来注册消息，这在运行时可能不可用。 " +
            "或者，可以手动重写 OnActivated()，并为该接收者单独注册每个必需的消息。")]
			*/
        [RequiresUnreferencedCode(
            "When this property is set to true, the OnActivated() method will be invoked, which will register all necessary message handlers for this recipient. " +
            "This method requires the generated CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions type not to be removed to use the fast path. " +
            "If this type is removed by the linker, or if the target recipient was created dynamically and was missed by the source generator, a slower fallback " +
            "path using a compiled LINQ expression will be used. This will have more overhead in the first invocation of this method for any given recipient type. " +
            "Alternatively, OnActivated() can be manually overwritten, and registration can be done individually for each required message for this recipient.")]
        [RequiresDynamicCode(
            "When this property is set to true, the OnActivated() method will be invoked, which will register all necessary message handlers for this recipient. " +
            "This method requires the generated CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions type not to be removed to use the fast path. " +
            "If that is present, the method is AOT safe, as the only methods being invoked to register the messages will be the ones produced by the source generator. " +
            "If it isn't, this method will need to dynamically create the generic methods to register messages, which might not be available at runtime. " +
            "Alternatively, OnActivated() can be manually overwritten, and registration can be done individually for each required message for this recipient.")]

        set
        {
            // 检查属性值是否改变，如果改变则触发相应的激活或取消激活方法
            if (SetProperty(ref this.isActive, value, true))
            {
                if (value)
                {
                    OnActivated();
                }
                else
                {
                    OnDeactivated();
                }
            }
        }
    }

    /// <summary>
    /// 当 <see cref="IsActive"/> 属性设置为 <see langword="true"/> 时调用。
    /// 使用此方法来注册消息并为该实例执行其他初始化操作。
    /// </summary>
    /// <remarks>
    /// 基类实现注册了所有已通过 <see cref="IRecipient{TMessage}"/> 接口明确声明的接收者消息，
    /// 使用默认通道。有关详细信息，请参见 <see cref="IMessengerExtensions.RegisterAll"/> 方法。
    /// 如果需要更精细的控制、希望单独注册消息或只喜欢使用 lambda 风格语法进行消息注册，请重写此方法并手动注册。
    /// </remarks>
	/*
    [RequiresUnreferencedCode(
        "此方法要求生成的 CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions 类型不被移除以使用快速路径。 " +
        "如果此类型被链接器移除，或者如果目标接收者是动态创建的并且被源生成器遗漏，则将使用编译的 LINQ 表达式的较慢回退路径。 " +
        "这将在任何给定接收者类型的第一次调用中产生更多开销。 " +
        "或者，可以手动重写 OnActivated()，并为该接收者单独注册每个必需的消息。")]
    [RequiresDynamicCode(
        "此方法要求生成的 CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions 类型不被移除以使用快速路径。 " +
        "如果存在，则该方法是 AOT 安全的，因为调用的唯一方法将是源生成器生成的消息注册方法。 " +
        "如果没有，此方法将需要动态创建通用方法来注册消息，这在运行时可能不可用。 " +
        "或者，可以手动重写 OnActivated()，并为该接收者单独注册每个必需的消息。")]
		*/
    [RequiresUnreferencedCode(
        "This method requires the generated CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions type not to be removed to use the fast path. " +
        "If this type is removed by the linker, or if the target recipient was created dynamically and was missed by the source generator, a slower fallback " +
        "path using a compiled LINQ expression will be used. This will have more overhead in the first invocation of this method for any given recipient type. " +
        "Alternatively, OnActivated() can be manually overwritten, and registration can be done individually for each required message for this recipient.")]
    [RequiresDynamicCode(
        "This method requires the generated CommunityToolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions type not to be removed to use the fast path. " +
        "If that is present, the method is AOT safe, as the only methods being invoked to register the messages will be the ones produced by the source generator. " +
        "If it isn't, this method will need to dynamically create the generic methods to register messages, which might not be available at runtime. " +
        "Alternatively, OnActivated() can be manually overwritten, and registration can be done individually for each required message for this recipient.")]
    protected virtual void OnActivated()
    {
        Messenger.RegisterAll(this);
    }

    /// <summary>
    /// 当 <see cref="IsActive"/> 属性设置为 <see langword="false"/> 时调用。
    /// 使用此方法取消注册消息并为该实例执行一般清理操作。
    /// </summary>
    /// <remarks>
    /// 基类实现取消注册该接收者的所有消息。通过调用 <see cref="IMessenger.UnregisterAll"/> 实现，
    /// 它会移除给定订阅者的所有已注册处理程序，无论使用了什么令牌。也就是说，所有订阅通道上的所有已注册处理程序都将被移除。
    /// </remarks>
    protected virtual void OnDeactivated()
    {
        Messenger.UnregisterAll(this);
    }

    /// <summary>
    /// 使用指定参数广播 <see cref="PropertyChangedMessage{T}"/>，
    /// 不使用任何特定令牌（因此使用默认通道）。
    /// </summary>
    /// <typeparam name="T">已更改属性的类型。</typeparam>
    /// <param name="oldValue">属性更改前的值。</param>
    /// <param name="newValue">属性更改后的值。</param>
    /// <param name="propertyName">已更改属性的名称。</param>
    /// <remarks>
    /// 如果希望自定义使用的通道（例如，如果需要使用特定令牌来访问通道），应该重写此方法。
    /// </remarks>
    protected virtual void Broadcast<T>(T oldValue, T newValue, string? propertyName)
    {
        // 创建属性更改消息
        PropertyChangedMessage<T> message = new(this, propertyName, oldValue, newValue);

        _ = Messenger.Send(message);
    }

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，则引发 <see cref="ObservableObject.PropertyChanging"/> 事件，
    /// 用新值更新属性，然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。
    /// </summary>
    /// <typeparam name="T">已更改属性的类型。</typeparam>
    /// <param name="field">存储属性值的字段。</param>
    /// <param name="newValue">变更后属性的值。</param>
    /// <param name="broadcast">如果为 <see langword="true"/>，也会调用 <see cref="Broadcast{T}"/>。</param>
    /// <param name="propertyName">（可选）已更改属性的名称。</param>
    /// <returns>如果属性已更改则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    /// <remarks>
    /// 此方法就像 <see cref="ObservableObject.SetProperty{T}(ref T,T,string)"/>，只是添加了
    /// <paramref name="broadcast"/> 参数。因此，按照基类方法的行为，如果目标属性的当前值和新值相同，
    /// 则不会引发 <see cref="ObservableObject.PropertyChanging"/> 和 <see cref="ObservableObject.PropertyChanged"/> 事件。
    /// </remarks>
    protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, bool broadcast, [CallerMemberName] string? propertyName = null)
    {
        // 保存旧值以供广播使用
        T oldValue = field;

        // 我们在这里重复基类中的代码，以便利用
        // EqualityComparer<T>.Default.Equals 的内联支持
        bool propertyChanged = SetProperty(ref field, newValue, propertyName);

        // 如果属性值改变且需要广播，则发送消息
        if (propertyChanged && broadcast)
        {
            Broadcast(oldValue, newValue, propertyName);
        }

        return propertyChanged;
    }

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，则引发 <see cref="ObservableObject.PropertyChanging"/> 事件，
    /// 用新值更新属性，然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。
    /// 关于此重载的其他说明，请参见 <see cref="SetProperty{T}(ref T,T,bool,string)"/>。
    /// </summary>
    /// <typeparam name="T">已更改属性的类型。</typeparam>
    /// <param name="field">存储属性值的字段。</param>
    /// <param name="newValue">变更后属性的值。</param>
    /// <param name="comparer">用于比较输入值的 <see cref="IEqualityComparer{T}"/> 实例。</param>
    /// <param name="broadcast">如果为 <see langword="true"/>，也会调用 <see cref="Broadcast{T}"/>。</param>
    /// <param name="propertyName">（可选）已更改属性的名称。</param>
    /// <returns>如果属性已更改则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="comparer"/> 为 <see langword="null"/> 则抛出。</exception>
    protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, IEqualityComparer<T> comparer, bool broadcast, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(comparer);

        // 保存旧值以供广播使用
        T oldValue = field;

        bool propertyChanged = SetProperty(ref field, newValue, comparer, propertyName);

        // 如果属性值改变且需要广播，则发送消息
        if (propertyChanged && broadcast)
        {
            Broadcast(oldValue, newValue, propertyName);
        }

        return propertyChanged;
    }

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，则引发 <see cref="ObservableObject.PropertyChanging"/> 事件，
    /// 用新值更新属性，然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。类似于
    /// <see cref="ObservableObject.SetProperty{T}(T,T,Action{T},string)"/> 方法，此重载仅应在
    /// <see cref="ObservableObject.SetProperty{T}(ref T,T,string)"/> 无法直接使用时才使用。
    /// </summary>
    /// <typeparam name="T">已更改属性的类型。</typeparam>
    /// <param name="oldValue">当前属性值。</param>
    /// <param name="newValue">变更后属性的值。</param>
    /// <param name="callback">用于更新属性值的回调。</param>
    /// <param name="broadcast">如果为 <see langword="true"/>，也会调用 <see cref="Broadcast{T}"/>。</param>
    /// <param name="propertyName">（可选）已更改属性的名称。</param>
    /// <returns>如果属性已更改则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    /// <remarks>
    /// 此方法就像 <see cref="ObservableObject.SetProperty{T}(T,T,Action{T},string)"/>，只是添加了
    /// <paramref name="broadcast"/> 参数。因此，按照基类方法的行为，如果目标属性的当前值和新值相同，
    /// 则不会引发 <see cref="ObservableObject.PropertyChanging"/> 和 <see cref="ObservableObject.PropertyChanged"/> 事件。
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="callback"/> 为 <see langword="null"/> 则抛出。</exception>
    protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback, bool broadcast, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(callback);

        bool propertyChanged = SetProperty(oldValue, newValue, callback, propertyName);

        // 如果属性值改变且需要广播，则发送消息
        if (propertyChanged && broadcast)
        {
            Broadcast(oldValue, newValue, propertyName);
        }

        return propertyChanged;
    }

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，则引发 <see cref="ObservableObject.PropertyChanging"/> 事件，
    /// 用新值更新属性，然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。
    /// 关于此重载的其他说明，请参见 <see cref="SetProperty{T}(T,T,Action{T},bool,string)"/>。
    /// </summary>
    /// <typeparam name="T">已更改属性的类型。</typeparam>
    /// <param name="oldValue">当前属性值。</param>
    /// <param name="newValue">变更后属性的值。</param>
    /// <param name="comparer">用于比较输入值的 <see cref="IEqualityComparer{T}"/> 实例。</param>
    /// <param name="callback">用于更新属性值的回调。</param>
    /// <param name="broadcast">如果为 <see langword="true"/>，也会调用 <see cref="Broadcast{T}"/>。</param>
    /// <param name="propertyName">（可选）已更改属性的名称。</param>
    /// <returns>如果属性已更改则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="comparer"/> 或 <paramref name="callback"/> 为 <see langword="null"/> 则抛出。</exception>
    protected bool SetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, bool broadcast, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(callback);

        bool propertyChanged = SetProperty(oldValue, newValue, comparer, callback, propertyName);

        // 如果属性值改变且需要广播，则发送消息
        if (propertyChanged && broadcast)
        {
            Broadcast(oldValue, newValue, propertyName);
        }

        return propertyChanged;
    }

    /// <summary>
    /// 比较给定嵌套属性的当前值和新值。如果值已更改，则引发 <see cref="ObservableObject.PropertyChanging"/> 事件，
    /// 更新属性然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。行为类似于
    /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,TModel,Action{TModel,T},string)"/>，
    /// 不同之处在于此方法用于从当前实例中的包装模型中中继属性。有关更多信息，请参见
    /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,TModel,Action{TModel,T},string)"/> 的文档。
    /// </summary>
    /// <typeparam name="TModel">其属性（或字段）要设置的模型类型。</typeparam>
    /// <typeparam name="T">要设置的属性（或字段）的类型。</typeparam>
    /// <param name="oldValue">当前属性值。</param>
    /// <param name="newValue">变更后属性的值。</param>
    /// <param name="model">模型</param>
    /// <param name="callback">如果发生更改则调用以设置目标属性值的回调。</param>
    /// <param name="broadcast">如果为 <see langword="true"/>，也会调用 <see cref="Broadcast{T}"/>。</param>
    /// <param name="propertyName">（可选）已更改属性的名称。</param>
    /// <returns>如果属性已更改则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="model"/> 或 <paramref name="callback"/> 为 <see langword="null"/> 则抛出。</exception>
    protected bool SetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, bool broadcast, [CallerMemberName] string? propertyName = null)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(callback);

        bool propertyChanged = SetProperty(oldValue, newValue, model, callback, propertyName);

        // 如果属性值改变且需要广播，则发送消息
        if (propertyChanged && broadcast)
        {
            Broadcast(oldValue, newValue, propertyName);
        }

        return propertyChanged;
    }

    /// <summary>
    /// 比较给定嵌套属性的当前值和新值。如果值已更改，则引发 <see cref="ObservableObject.PropertyChanging"/> 事件，
    /// 更新属性然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。行为类似于
    /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,IEqualityComparer{T},TModel,Action{TModel,T},string)"/>，
    /// 不同之处在于此方法用于从当前实例中的包装模型中中继属性。有关更多信息，请参见
    /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,IEqualityComparer{T},TModel,Action{TModel,T},string)"/> 的文档。
    /// </summary>
    /// <typeparam name="TModel">其属性（或字段）要设置的模型类型。</typeparam>
    /// <typeparam name="T">要设置的属性（或字段）的类型。</typeparam>
    /// <param name="oldValue">当前属性值。</param>
    /// <param name="newValue">变更后属性的值。</param>
    /// <param name="comparer">用于比较输入值的 <see cref="IEqualityComparer{T}"/> 实例。</param>
    /// <param name="model">模型</param>
    /// <param name="callback">如果发生更改则调用以设置目标属性值的回调。</param>
    /// <param name="broadcast">如果为 <see langword="true"/>，也会调用 <see cref="Broadcast{T}"/>。</param>
    /// <param name="propertyName">（可选）已更改属性的名称。</param>
    /// <returns>如果属性已更改则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="comparer"/>、<paramref name="model"/> 或 <paramref name="callback"/> 为 <see langword="null"/> 则抛出。</exception>
    protected bool SetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, bool broadcast, [CallerMemberName] string? propertyName = null)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(callback);

        bool propertyChanged = SetProperty(oldValue, newValue, comparer, model, callback, propertyName);

        // 如果属性值改变且需要广播，则发送消息
        if (propertyChanged && broadcast)
        {
            Broadcast(oldValue, newValue, propertyName);
        }

        return propertyChanged;
    }
}