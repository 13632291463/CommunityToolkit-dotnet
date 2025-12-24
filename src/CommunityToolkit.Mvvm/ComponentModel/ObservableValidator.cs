// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// 实现 <see cref="INotifyDataErrorInfo"/> 接口对象的基类。此类还继承自 <see cref="ObservableObject"/>，
/// 因此也可用于可观察项。
/// </summary>
public abstract class ObservableValidator : ObservableObject, INotifyDataErrorInfo
{
    /// <summary>
    /// 用于跟踪实体验证编译委托的 <see cref="ConditionalWeakTable{TKey,TValue}"/> 实例。
    /// </summary>
    private static readonly ConditionalWeakTable<Type, Action<object>> EntityValidatorMap = new();

    /// <summary>
    /// 用于跟踪待验证属性显示名称的 <see cref="ConditionalWeakTable{TKey, TValue}"/> 实例。
    /// </summary>
    /// <remarks>
    /// 这是必要的，因为我们要为所有验证重用相同的 <see cref="ValidationContext"/> 实例，但又要提供
    /// 与新实例相同格式名称的行为。问题是当设置 <see cref="ValidationContext.MemberName"/> 时
    /// <see cref="ValidationContext.DisplayName"/> 属性不会刷新，所以我们需要复制相同的逻辑来检索
    /// 要验证的属性的正确显示名称，并在将上下文传递给 <see cref="Validator"/> 之前手动更新该属性，
    /// 然后继续执行正常的逻辑。
    /// </remarks>
    private static readonly ConditionalWeakTable<Type, Dictionary<string, string>> DisplayNamesMap = new();

    /// <summary>
    /// <see cref="HasErrors"/> 的缓存 <see cref="PropertyChangedEventArgs"/>。
    /// </summary>
    private static readonly PropertyChangedEventArgs HasErrorsChangedEventArgs = new(nameof(HasErrors));

    /// <summary>
    /// 当前使用的 <see cref="ValidationContext"/> 实例。
    /// </summary>
    private readonly ValidationContext validationContext;

    /// <summary>
    /// 用于存储先前验证结果的 <see cref="Dictionary{TKey,TValue}"/> 实例。
    /// </summary>
    private readonly Dictionary<string, List<ValidationResult>> errors = new();

    /// <summary>
    /// 指示有错误的属性总数（不是错误总数）。
    /// 这用于使 <see cref="HasErrors"/> 可以在 O(1) 时间内运行，因为它只需检查此值是否非0，
    /// 而无需遍历 <see cref="errors"/>。
    /// </summary>
    private int totalErrors;

    /// <inheritdoc/>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// 初始化 <see cref="ObservableValidator"/> 类的新实例。
    /// 此构造函数将创建一个新的 <see cref="ValidationContext"/>，
    /// 该上下文将用于验证所有属性，它将引用当前实例，不包含额外的服务或验证属性和设置。
	/// RequiresUnreferencedCode:当前实例的类型无法静态发现
    /// </summary>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected ObservableValidator()
    {
        this.validationContext = new ValidationContext(this);
    }

    /// <summary>
    /// 初始化 <see cref="ObservableValidator"/> 类的新实例。
    /// 此构造函数将创建一个新的 <see cref="ValidationContext"/>，
    /// 该上下文将用于验证所有属性，它将引用当前实例。
    /// </summary>
    /// <param name="items">要提供给使用者的一组键/值对。</param>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected ObservableValidator(IDictionary<object, object?>? items)
    {
        this.validationContext = new ValidationContext(this, items);
    }

    /// <summary>
    /// 初始化 <see cref="ObservableValidator"/> 类的新实例。
    /// 此构造函数将创建一个新的 <see cref="ValidationContext"/>，
    /// 该上下文将用于验证所有属性，它将引用当前实例。
    /// </summary>
    /// <param name="serviceProvider">在验证期间可用的 <see cref="IServiceProvider"/> 实例。</param>
    /// <param name="items">要提供给使用者的一组键/值对。</param>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected ObservableValidator(IServiceProvider? serviceProvider, IDictionary<object, object?>? items)
    {
        this.validationContext = new ValidationContext(this, serviceProvider, items);
    }

    /// <summary>
    /// 初始化 <see cref="ObservableValidator"/> 类的新实例。
    /// 此构造函数将存储输入的 <see cref="ValidationContext"/> 实例，
    /// 并使用它来验证当前视图模型的所有属性。
    /// </summary>
    /// <param name="validationContext">
    /// 要用于验证属性的 <see cref="ValidationContext"/> 实例。
    /// <para>
    /// 此实例将传递给当前视图模型执行的所有 <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult})"/>
    /// 调用，并且在调用前将更新其 <see cref="ValidationContext.MemberName"/> 属性以设置正在验证的属性名称。
    /// 之后不会重置该值，因此 <see cref="ValidationContext.MemberName"/> 的值将始终指示最后验证的属性名称（如果有）。
    /// </para>
    /// </param>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="validationContext"/> 为 <see langword="null"/> 则抛出。</exception>
    protected ObservableValidator(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        this.validationContext = validationContext;
    }

    /// <inheritdoc/>
    [Display(AutoGenerateField = false)]
    public bool HasErrors => this.totalErrors > 0;

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，
    /// 引发 <see cref="ObservableObject.PropertyChanging"/> 事件，使用新值更新属性，
    /// 然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。
    /// </summary>
    /// <typeparam name="T">已更改属性的类型。</typeparam>
    /// <param name="field">存储属性值的字段。</param>
    /// <param name="newValue">变更后属性的值。</param>
    /// <param name="validate">如果为 <see langword="true"/>，则 <paramref name="newValue"/> 也将被验证。</param>
    /// <param name="propertyName">（可选）已更改属性的名称。</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
    /// <remarks>
    /// 此方法与 <see cref="ObservableObject.SetProperty{T}(ref T,T,string)"/> 类似，只是增加了
    /// <paramref name="validate"/> 参数。如果设置为 <see langword="true"/>，新值将被验证，
    /// 如需要将引发 <see cref="ErrorsChanged"/>。按照基方法的行为，
    /// 如果目标属性的当前值和新值相同，则不会引发 <see cref="ObservableObject.PropertyChanging"/> 和 <see cref="ObservableObject.PropertyChanged"/> 事件。
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="propertyName"/> 为 <see langword="null"/> 则抛出。</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, bool validate, [CallerMemberName] string propertyName = null!)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        bool propertyChanged = SetProperty(ref field, newValue, propertyName);

        if (propertyChanged && validate)
        {
            ValidateProperty(newValue, propertyName);
        }

        return propertyChanged;
    }

    /// <summary>
    /// 比较给定属性的当前值和新值。如果值已更改，
    /// 引发 <see cref="ObservableObject.PropertyChanging"/> 事件，使用新值更新属性，
    /// 然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。
    /// 有关此重载的更多说明，请参见 <see cref="SetProperty{T}(ref T,T,bool,string)"/>。
    /// </summary>
    /// <typeparam name="T">已更改属性的类型。</typeparam>
    /// <param name="field">存储属性值的字段。</param>
    /// <param name="newValue">变更后属性的值。</param>
    /// <param name="comparer">用于比较输入值的 <see cref="IEqualityComparer{T}"/> 实例。</param>
    /// <param name="validate">如果为 <see langword="true"/>，则 <paramref name="newValue"/> 也将被验证。</param>
    /// <param name="propertyName">（可选）已更改属性的名称。</param>
    /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
    /// <exception cref="System.ArgumentNullException">如果 <paramref name="comparer"/> 或 <paramref name="propertyName"/> 为 <see langword="null"/> 则抛出。</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, IEqualityComparer<T> comparer, bool validate, [CallerMemberName] string propertyName = null!)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(propertyName);

        bool propertyChanged = SetProperty(ref field, newValue, comparer, propertyName);

        if (propertyChanged && validate)
        {
            ValidateProperty(newValue, propertyName);
        }

        return propertyChanged;
    }

     
    
        /// <summary>
        /// 比较给定属性的当前值和新值。如果值已更改，
        /// 引发 <see cref="ObservableObject.PropertyChanging"/> 事件，使用新值更新属性，
        /// 然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。与
        /// <see cref="ObservableObject.SetProperty{T}(T,T,Action{T},string)"/> 方法类似，此重载仅应在
        /// <see cref="ObservableObject.SetProperty{T}(ref T,T,string)"/> 无法直接使用时使用。
        /// </summary>
        /// <typeparam name="T">已更改属性的类型。</typeparam>
        /// <param name="oldValue">当前属性值。</param>
        /// <param name="newValue">变更后属性的值。</param>
        /// <param name="callback">用于更新属性值的回调函数。</param>
        /// <param name="validate">如果为 <see langword="true"/>，则 <paramref name="newValue"/> 也将被验证。</param>
        /// <param name="propertyName">（可选）已更改属性的名称。</param>
        /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
        /// <remarks>
        /// 此方法与 <see cref="ObservableObject.SetProperty{T}(T,T,Action{T},string)"/> 相同，只是增加了
        /// <paramref name="validate"/> 参数。因此，遵循基方法的行为，
        /// 如果目标属性的当前值和新值相同，则不会引发 <see cref="ObservableObject.PropertyChanging"/> 和 <see cref="ObservableObject.PropertyChanged"/> 事件。
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">如果 <paramref name="callback"/> 或 <paramref name="propertyName"/> 为 <see langword="null"/> 则抛出。</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
        protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback, bool validate, [CallerMemberName] string propertyName = null!)
        {
            ArgumentNullException.ThrowIfNull(callback);
            ArgumentNullException.ThrowIfNull(propertyName);
    
            bool propertyChanged = SetProperty(oldValue, newValue, callback, propertyName);
    
            if (propertyChanged && validate)
            {
                ValidateProperty(newValue, propertyName);
            }
    
            return propertyChanged;
        }
    
        /// <summary>
        /// 比较给定属性的当前值和新值。如果值已更改，
        /// 引发 <see cref="ObservableObject.PropertyChanging"/> 事件，使用新值更新属性，
        /// 然后引发 <see cref="ObservableObject.PropertyChanged"/> 事件。
        /// 有关此重载的更多说明，请参见 <see cref="SetProperty{T}(T,T,Action{T},bool,string)"/>。
        /// </summary>
        /// <typeparam name="T">已更改属性的类型。</typeparam>
        /// <param name="oldValue">当前属性值。</param>
        /// <param name="newValue">变更后属性的值。</param>
        /// <param name="comparer">用于比较输入值的 <see cref="IEqualityComparer{T}"/> 实例。</param>
        /// <param name="callback">用于更新属性值的回调函数。</param>
        /// <param name="validate">如果为 <see langword="true"/>，则 <paramref name="newValue"/> 也将被验证。</param>
        /// <param name="propertyName">（可选）已更改属性的名称。</param>
        /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
        /// <exception cref="System.ArgumentNullException">如果 <paramref name="comparer"/>、<paramref name="callback"/> 或 <paramref name="propertyName"/> 为 <see langword="null"/> 则抛出。</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
        protected bool SetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, bool validate, [CallerMemberName] string propertyName = null!)
        {
            ArgumentNullException.ThrowIfNull(comparer);
            ArgumentNullException.ThrowIfNull(callback);
            ArgumentNullException.ThrowIfNull(propertyName);
    
            bool propertyChanged = SetProperty(oldValue, newValue, comparer, callback, propertyName);
    
            if (propertyChanged && validate)
            {
                ValidateProperty(newValue, propertyName);
            }
    
            return propertyChanged;
        }
    
        /// <summary>
        /// 比较给定嵌套属性的当前值和新值。如果值已更改，
        /// 引发 <see cref="ObservableObject.PropertyChanging"/> 事件，更新属性然后引发
        /// <see cref="ObservableObject.PropertyChanged"/> 事件。行为与
        /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,TModel,Action{TModel,T},string)"/> 相同，不同之处在于
        /// 此方法用于在当前实例中转发包装模型的属性。有关更多信息，请参见
        /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,TModel,Action{TModel,T},string)"/> 的文档。
        /// </summary>
        /// <typeparam name="TModel">要设置其属性（或字段）的模型类型。</typeparam>
        /// <typeparam name="T">要设置的属性（或字段）的类型。</typeparam>
        /// <param name="oldValue">当前属性值。</param>
        /// <param name="newValue">变更后属性的值。</param>
        /// <param name="model">模型对象。</param>
        /// <param name="callback">如果发生更改，用于设置目标属性值的回调函数。</param>
        /// <param name="validate">如果为 <see langword="true"/>，则 <paramref name="newValue"/> 也将被验证。</param>
        /// <param name="propertyName">（可选）已更改属性的名称。</param>
        /// <returns>如果属性已更改则为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
        /// <exception cref="System.ArgumentNullException">如果 <paramref name="model"/>、<paramref name="callback"/> 或 <paramref name="propertyName"/> 为 <see langword="null"/> 则抛出。</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
        protected bool SetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, bool validate, [CallerMemberName] string propertyName = null!)
            where TModel : class
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(callback);
            ArgumentNullException.ThrowIfNull(propertyName);
    
            bool propertyChanged = SetProperty(oldValue, newValue, model, callback, propertyName);
    
            if (propertyChanged && validate)
            {
                ValidateProperty(newValue, propertyName);
            }
    
            return propertyChanged;
        }
    
    /// <summary>
    /// Compares the current and new values for a given nested property. If the value has changed,
    /// raises the <see cref="ObservableObject.PropertyChanging"/> event, updates the property and then raises the
    /// <see cref="ObservableObject.PropertyChanged"/> event. The behavior mirrors that of
    /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,IEqualityComparer{T},TModel,Action{TModel,T},string)"/>,
    /// with the difference being that this method is used to relay properties from a wrapped model in the
    /// current instance. For more info, see the docs for
    /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,IEqualityComparer{T},TModel,Action{TModel,T},string)"/>.
    /// </summary>
    /// <typeparam name="TModel">The type of model whose property (or field) to set.</typeparam>
    /// <typeparam name="T">The type of property (or field) to set.</typeparam>
    /// <param name="oldValue">The current property value.</param>
    /// <param name="newValue">The property's value after the change occurred.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> instance to use to compare the input values.</param>
    /// <param name="model">The model </param>
    /// <param name="callback">The callback to invoke to set the target property value, if a change has occurred.</param>
    /// <param name="validate">If <see langword="true"/>, <paramref name="newValue"/> will also be validated.</param>
    /// <param name="propertyName">(optional) The name of the property that changed.</param>
    /// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="comparer"/>, <paramref name="model"/>, <paramref name="callback"/> or <paramref name="propertyName"/> are <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected bool SetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, bool validate, [CallerMemberName] string propertyName = null!)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(propertyName);

        bool propertyChanged = SetProperty(oldValue, newValue, comparer, model, callback, propertyName);

        if (propertyChanged && validate)
        {
            ValidateProperty(newValue, propertyName);
        }

        return propertyChanged;
    }

    /// <summary>
    /// Tries to validate a new value for a specified property. If the validation is successful,
    /// <see cref="ObservableObject.SetProperty{T}(ref T,T,string?)"/> is called, otherwise no state change is performed.
    /// </summary>
    /// <typeparam name="T">The type of the property that changed.</typeparam>
    /// <param name="field">The field storing the property's value.</param>
    /// <param name="newValue">The property's value after the change occurred.</param>
    /// <param name="errors">The resulting validation errors, if any.</param>
    /// <param name="propertyName">(optional) The name of the property that changed.</param>
    /// <returns>Whether the validation was successful and the property value changed as well.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="propertyName"/> is <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected bool TrySetProperty<T>(ref T field, T newValue, out IReadOnlyCollection<ValidationResult> errors, [CallerMemberName] string propertyName = null!)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        return TryValidateProperty(newValue, propertyName, out errors) &&
               SetProperty(ref field, newValue, propertyName);
    }

    /// <summary>
    /// Tries to validate a new value for a specified property. If the validation is successful,
    /// <see cref="ObservableObject.SetProperty{T}(ref T,T,IEqualityComparer{T},string?)"/> is called, otherwise no state change is performed.
    /// </summary>
    /// <typeparam name="T">The type of the property that changed.</typeparam>
    /// <param name="field">The field storing the property's value.</param>
    /// <param name="newValue">The property's value after the change occurred.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> instance to use to compare the input values.</param>
    /// <param name="errors">The resulting validation errors, if any.</param>
    /// <param name="propertyName">(optional) The name of the property that changed.</param>
    /// <returns>Whether the validation was successful and the property value changed as well.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="comparer"/> or <paramref name="propertyName"/> are <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected bool TrySetProperty<T>(ref T field, T newValue, IEqualityComparer<T> comparer, out IReadOnlyCollection<ValidationResult> errors, [CallerMemberName] string propertyName = null!)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(propertyName);

        return TryValidateProperty(newValue, propertyName, out errors) &&
               SetProperty(ref field, newValue, comparer, propertyName);
    }

    /// <summary>
    /// Tries to validate a new value for a specified property. If the validation is successful,
    /// <see cref="ObservableObject.SetProperty{T}(T,T,Action{T},string?)"/> is called, otherwise no state change is performed.
    /// </summary>
    /// <typeparam name="T">The type of the property that changed.</typeparam>
    /// <param name="oldValue">The current property value.</param>
    /// <param name="newValue">The property's value after the change occurred.</param>
    /// <param name="callback">A callback to invoke to update the property value.</param>
    /// <param name="errors">The resulting validation errors, if any.</param>
    /// <param name="propertyName">(optional) The name of the property that changed.</param>
    /// <returns>Whether the validation was successful and the property value changed as well.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="callback"/> or <paramref name="propertyName"/> are <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected bool TrySetProperty<T>(T oldValue, T newValue, Action<T> callback, out IReadOnlyCollection<ValidationResult> errors, [CallerMemberName] string propertyName = null!)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(propertyName);

        return TryValidateProperty(newValue, propertyName, out errors) &&
               SetProperty(oldValue, newValue, callback, propertyName);
    }

    /// <summary>
    /// Tries to validate a new value for a specified property. If the validation is successful,
    /// <see cref="ObservableObject.SetProperty{T}(T,T,IEqualityComparer{T},Action{T},string?)"/> is called, otherwise no state change is performed.
    /// </summary>
    /// <typeparam name="T">The type of the property that changed.</typeparam>
    /// <param name="oldValue">The current property value.</param>
    /// <param name="newValue">The property's value after the change occurred.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> instance to use to compare the input values.</param>
    /// <param name="callback">A callback to invoke to update the property value.</param>
    /// <param name="errors">The resulting validation errors, if any.</param>
    /// <param name="propertyName">(optional) The name of the property that changed.</param>
    /// <returns>Whether the validation was successful and the property value changed as well.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="comparer"/>, <paramref name="callback"/> or <paramref name="propertyName"/> are <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected bool TrySetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, out IReadOnlyCollection<ValidationResult> errors, [CallerMemberName] string propertyName = null!)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(propertyName);

        return TryValidateProperty(newValue, propertyName, out errors) &&
               SetProperty(oldValue, newValue, comparer, callback, propertyName);
    }

    /// <summary>
    /// Tries to validate a new value for a specified property. If the validation is successful,
    /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,TModel,Action{TModel,T},string?)"/> is called, otherwise no state change is performed.
    /// </summary>
    /// <typeparam name="TModel">The type of model whose property (or field) to set.</typeparam>
    /// <typeparam name="T">The type of property (or field) to set.</typeparam>
    /// <param name="oldValue">The current property value.</param>
    /// <param name="newValue">The property's value after the change occurred.</param>
    /// <param name="model">The model </param>
    /// <param name="callback">The callback to invoke to set the target property value, if a change has occurred.</param>
    /// <param name="errors">The resulting validation errors, if any.</param>
    /// <param name="propertyName">(optional) The name of the property that changed.</param>
    /// <returns>Whether the validation was successful and the property value changed as well.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="model"/>, <paramref name="callback"/> or <paramref name="propertyName"/> are <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected bool TrySetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, out IReadOnlyCollection<ValidationResult> errors, [CallerMemberName] string propertyName = null!)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(propertyName);

        return TryValidateProperty(newValue, propertyName, out errors) &&
               SetProperty(oldValue, newValue, model, callback, propertyName);
    }

    /// <summary>
    /// Tries to validate a new value for a specified property. If the validation is successful,
    /// <see cref="ObservableObject.SetProperty{TModel,T}(T,T,IEqualityComparer{T},TModel,Action{TModel,T},string?)"/> is called, otherwise no state change is performed.
    /// </summary>
    /// <typeparam name="TModel">The type of model whose property (or field) to set.</typeparam>
    /// <typeparam name="T">The type of property (or field) to set.</typeparam>
    /// <param name="oldValue">The current property value.</param>
    /// <param name="newValue">The property's value after the change occurred.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> instance to use to compare the input values.</param>
    /// <param name="model">The model </param>
    /// <param name="callback">The callback to invoke to set the target property value, if a change has occurred.</param>
    /// <param name="errors">The resulting validation errors, if any.</param>
    /// <param name="propertyName">(optional) The name of the property that changed.</param>
    /// <returns>Whether the validation was successful and the property value changed as well.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="comparer"/>, <paramref name="model"/>, <paramref name="callback"/> or <paramref name="propertyName"/> are <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected bool TrySetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, out IReadOnlyCollection<ValidationResult> errors, [CallerMemberName] string propertyName = null!)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(propertyName);

        return TryValidateProperty(newValue, propertyName, out errors) &&
               SetProperty(oldValue, newValue, comparer, model, callback, propertyName);
    }

    /// <summary>
    /// 清除指定属性或整个实体的验证错误。
    /// </summary>
    /// <param name="propertyName">
    /// 要清除验证错误的属性名称。
    /// 如果使用 null 或空名称，则清除所有实体级错误。
    /// </param>
    protected void ClearErrors(string? propertyName = null)
    {
        // 当目标属性为 null 或空时清除实体级错误
        if (string.IsNullOrEmpty(propertyName))
        {
            ClearAllErrors();
        }
        else
        {
            ClearErrorsForProperty(propertyName!);
        }
    }

    /// <inheritdoc cref="INotifyDataErrorInfo.GetErrors(string)"/>
    public IEnumerable<ValidationResult> GetErrors(string? propertyName = null)
    {
        // 当目标属性为 null 或空时获取实体级错误
        if (string.IsNullOrEmpty(propertyName))
        {
            // 用于收集所有实体级错误的本地函数
            [MethodImpl(MethodImplOptions.NoInlining)]
            IEnumerable<ValidationResult> GetAllErrors()
            {
                return this.errors.Values.SelectMany(static errors => errors);
            }

            return GetAllErrors();
        }

        // 属性级错误（如果有）
        if (this.errors.TryGetValue(propertyName!, out List<ValidationResult>? errors))
        {
            return errors;
        }

        // INotifyDataErrorInfo.GetErrors 方法没有明确指定当输入属性名称无效时返回什么，
        // 但由于返回类型被标记为非空引用类型，因此这里我们返回一个空数组以遵守契约。
        // 这也与检索有效属性错误时此方法的行为相匹配。
        return Array.Empty<ValidationResult>();
    }

    /// <inheritdoc/>
    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => GetErrors(propertyName);

    /// <summary>
    /// 验证当前实例中的所有属性并更新所有跟踪的错误。
    /// 如果检测到任何更改，将引发 <see cref="ErrorsChanged"/> 事件。
    /// </summary>
    /// <remarks>
    /// 仅验证应用了至少一个 <see cref="ValidationAttribute"/> 的公共实例属性（不包括自定义索引器）。
    /// 当前实例中的所有其他成员将被忽略。所有处理的属性都不会被修改 - 它们只会被用于检索其值并验证它们。
    /// </remarks>
	/*
    [RequiresUnreferencedCode(
        "此方法需要生成的 CommunityToolkit.Mvvm.ComponentModel.__Internals.__ObservableValidatorExtensions 类型不能被删除才能使用快速路径。 " +
        "如果此类型被链接器删除，或者如果目标接收者是动态创建的且被源生成器遗漏，则将使用编译的 LINQ 表达式的较慢回退路径。 " +
        "这将使此方法的首次调用对于任何给定接收者类型具有更多开销。此外，由于使用了验证 API，当前实例的类型不能静态发现。")]
		*/
    [RequiresUnreferencedCode(
        "This method requires the generated CommunityToolkit.Mvvm.ComponentModel.__Internals.__ObservableValidatorExtensions type not to be removed to use the fast path. " +
        "If this type is removed by the linker, or if the target recipient was created dynamically and was missed by the source generator, a slower fallback " +
        "path using a compiled LINQ expression will be used. This will have more overhead in the first invocation of this method for any given recipient type. " +
        "Additionally, due to the usage of validation APIs, the type of the current instance cannot be statically discovered.")]
    protected void ValidateAllProperties()
    {
        // 尝试从生成的类型特定方法创建委托的快速路径。这是为了使此方法更适合 AOT，
        // 并且更快，因为没有动态代码。
        [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
        static Action<object> GetValidationAction(Type type)
        {
            if (type.Assembly.GetType("CommunityToolkit.Mvvm.ComponentModel.__Internals.__ObservableValidatorExtensions") is Type extensionsType &&
                extensionsType.GetMethod("CreateAllPropertiesValidator", new[] { type }) is MethodInfo methodInfo)
            {
                return (Action<object>)methodInfo.Invoke(null, new object?[] { null })!;
            }

            return GetValidationActionFallback(type);
        }

        // 使用编译的 LINQ 表达式创建委托的回退方法
        static Action<object> GetValidationActionFallback(Type type)
        {
            // 获取要验证的所有属性的集合
            (string Name, MethodInfo GetMethod)[] validatableProperties = (
                from property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                where property.GetIndexParameters().Length == 0 &&
                      property.GetCustomAttributes<ValidationAttribute>(true).Any()
                let getMethod = property.GetMethod
                where getMethod is not null
                select (property.Name, getMethod)).ToArray();

            // 没有要验证的属性的短路径
            if (validatableProperties.Length == 0)
            {
                return static _ => { };
            }

            // MyViewModel inst0 = (MyViewModel)arg0;
            ParameterExpression arg0 = Expression.Parameter(typeof(object));
            UnaryExpression inst0 = Expression.Convert(arg0, type);

            // 获取对 ValidateProperty(object, string) 的引用
            MethodInfo validateMethod = typeof(ObservableValidator).GetMethod(nameof(ValidateProperty), BindingFlags.Instance | BindingFlags.NonPublic)!;

            // 我们希望一个单一的编译 LINQ 表达式，该表达式在执行视图模型的实际类型时验证所有属性。
            // 我们通过创建一个包含所有要验证的属性的展开调用的块表达式来实现这一点。
            // 本质上，body 将包含以下代码：
            // ===============================================================================
            // {
            //     inst0.ValidateProperty(inst0.Property0, nameof(MyViewModel.Property0));
            //     inst0.ValidateProperty(inst0.Property1, nameof(MyViewModel.Property1));
            //     ...
            //     inst0.ValidateProperty(inst0.PropertyN, nameof(MyViewModel.PropertyN));
            // }
            // ===============================================================================
            // 我们还添加了一个显式对象转换来表示装箱，如果给定属性是值类型。如果值已经是引用类型，这将只是一个无操作。
            // 请注意，此生成的代码在技术上是从 ObservableValidator 访问受保护的方法，但这没关系，
            // 因为 IL 实际上没有成员可见性的概念，这只是 C# 编译时特性。
            BlockExpression body = Expression.Block(
                from property in validatableProperties
                select Expression.Call(inst0, validateMethod, new Expression[]
                {
                    Expression.Convert(Expression.Call(inst0, property.GetMethod), typeof(object)),
                    Expression.Constant(property.Name)
                }));

            return Expression.Lambda<Action<object>>(body, arg0).Compile();
        }

        // 获取或计算要验证的缓存属性列表。这里我们使用静态 lambda 以确保 C# 编译器缓存
        // 委托，参见 https://github.com/dotnet/roslyn/issues/5835 上的相关问题。
        EntityValidatorMap.GetValue(
            GetType(),
            [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")] static (t) => GetValidationAction(t))(this);
    }

    /// <summary>
    /// 验证具有指定名称和给定输入值的属性。
    /// 如果检测到任何更改，将引发 <see cref="ErrorsChanged"/> 事件。
    /// </summary>
    /// <param name="value">要测试指定属性的值。</param>
    /// <param name="propertyName">要验证的属性的名称。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="propertyName"/> 为 <see langword="null"/> 时引发。</exception>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    protected internal void ValidateProperty(object? value, [CallerMemberName] string propertyName = null!)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        // 检查属性是否已预先验证，如果是，则从错误字典中检索
        // 可重用的验证错误列表。此列表用于添加下面的新验证错误（如果有）。
        // 如果属性不在字典中，则现在添加它以避免分配。
        if (!this.errors.TryGetValue(propertyName, out List<ValidationResult>? propertyErrors))
        {
            propertyErrors = new List<ValidationResult>();

            this.errors.Add(propertyName, propertyErrors);
        }

        bool errorsChanged = false;

        // 清除指定属性的错误（如果有的话）
        if (propertyErrors.Count > 0)
        {
            propertyErrors.Clear();

            errorsChanged = true;
        }

        // 通过向现有列表添加新错误来验证属性
        this.validationContext.MemberName = propertyName;
        this.validationContext.DisplayName = GetDisplayNameForProperty(propertyName);

        bool isValid = Validator.TryValidateProperty(value, this.validationContext, propertyErrors);

        // 更新共享错误总数计数器，并在必要时引发属性更改事件。
        // 如果当前属性有效但在此验证之前无效，则减少错误总数；
        // 如果验证失败且之前是正确的，则增加错误总数。
        // 当错误总数减少到 0 或增加到 1 时，将引发属性更改事件。
        if (isValid)
        {
            if (errorsChanged)
            {
                this.totalErrors--;

                if (this.totalErrors == 0)
                {
                    OnPropertyChanged(HasErrorsChangedEventArgs);
                }
            }
        }
        else if (!errorsChanged)
        {
            this.totalErrors++;

            if (this.totalErrors == 1)
            {
                OnPropertyChanged(HasErrorsChangedEventArgs);
            }
        }

        // 如果需要，只引发一次事件。这要么发生在目标属性有现有错误但现在有效，
        // 要么验证失败且有新的错误需要广播，而不管属性之前的验证状态。
        if (errorsChanged || !isValid)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 尝试验证具有指定名称和给定输入值的属性，并返回计算的错误（如果有）。
    /// 如果属性有效，则假定其值即将在当前对象中设置。否则，不会修改任何可观察的本地状态。
    /// </summary>
    /// <param name="value">要测试指定属性的值。</param>
    /// <param name="propertyName">要验证的属性的名称。</param>
    /// <param name="errors">结果验证错误（如果有）。</param>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    private bool TryValidateProperty(object? value, string propertyName, out IReadOnlyCollection<ValidationResult> errors)
    {
        // 为后续使用添加缓存的错误列表。
        if (!this.errors.TryGetValue(propertyName!, out List<ValidationResult>? propertyErrors))
        {
            propertyErrors = new List<ValidationResult>();

            this.errors.Add(propertyName!, propertyErrors);
        }

        bool hasErrors = propertyErrors.Count > 0;

        List<ValidationResult> localErrors = new();

        // 通过向本地列表添加新错误来验证属性
        this.validationContext.MemberName = propertyName;
        this.validationContext.DisplayName = GetDisplayNameForProperty(propertyName!);

        bool isValid = Validator.TryValidateProperty(value, this.validationContext, localErrors);

        // 我们只在属性有效且之前无效时修改状态。在这种情况下，我们
        // 清除缓存的错误列表（这是对使用者可见的）并引发必要的事件。
        if (isValid && hasErrors)
        {
            propertyErrors.Clear();

            this.totalErrors--;

            if (this.totalErrors == 0)
            {
                OnPropertyChanged(HasErrorsChangedEventArgs);
            }

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        errors = localErrors;

        return isValid;
    }

    /// <summary>
    /// 清除整个实体的所有当前错误。
    /// </summary>
    private void ClearAllErrors()
    {
        if (this.totalErrors == 0)
        {
            return;
        }

        // 清除所有具有至少一个错误的属性的错误，并为这些属性引发
        // ErrorsChanged 事件。其他属性将被忽略。
        foreach (KeyValuePair<string, List<ValidationResult>> propertyInfo in this.errors)
        {
            bool hasErrors = propertyInfo.Value.Count > 0;

            propertyInfo.Value.Clear();

            if (hasErrors)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyInfo.Key));
            }
        }

        this.totalErrors = 0;

        OnPropertyChanged(HasErrorsChangedEventArgs);
    }

    /// <summary>
    /// 清除目标属性的所有当前错误。
    /// </summary>
    /// <param name="propertyName">要清除错误的属性名称。</param>
    private void ClearErrorsForProperty(string propertyName)
    {
        if (!this.errors.TryGetValue(propertyName!, out List<ValidationResult>? propertyErrors) ||
            propertyErrors.Count == 0)
        {
            return;
        }

        propertyErrors.Clear();

        this.totalErrors--;

        if (this.totalErrors == 0)
        {
            OnPropertyChanged(HasErrorsChangedEventArgs);
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 获取给定属性的显示名称。它可能是自定义名称或只是属性名称。
    /// </summary>
    /// <param name="propertyName">正在验证的目标属性名称。</param>
    /// <returns>属性的显示名称。</returns>
    [RequiresUnreferencedCode("The type of the current instance cannot be statically discovered.")]
    private string GetDisplayNameForProperty(string propertyName)
    {
        static Dictionary<string, string> GetDisplayNames(Type type)
        {
            Dictionary<string, string> displayNames = new();

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute attribute &&
                    attribute.GetName() is string displayName)
                {
                    displayNames.Add(property.Name, displayName);
                }
            }

            return displayNames;
        }

        // 此方法复制了 DisplayName 和 ValidationContext 类中的 GetDisplayName 的逻辑。
        // 有关更多详细信息，请参阅 BCL 中的原始源代码。
        _ = DisplayNamesMap.GetValue(GetType(), static t => GetDisplayNames(t)).TryGetValue(propertyName, out string? displayName);

        return displayName ?? propertyName;
    }
}