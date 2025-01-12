﻿using Microsoft.UI.Xaml;
using System;

namespace TagsTree.Attributes;

/// <summary>
///     生成如下代码
///     <code>
/// <see langword="public static readonly"/> <see cref="DependencyProperty"/> Property = <see cref="DependencyProperty"/>.Register("Field", <see langword="typeof"/>(Type), <see langword="typeof"/>(TClass), <see langword="new"/> <see cref="PropertyMetadata"/>(DefaultValue, OnPropertyChanged));
/// <br/>
/// <see langword="public"/> <see cref="T:TagsTree.Attributes.DependencyPropertyAttribute`1"/> Field { <see langword="get"/> => (<see cref="T:TagsTree.Attributes.DependencyPropertyAttribute`1"/>)GetValue(Property); <see langword="set"/> => SetValue(Property, <see langword="value"/>); }
///     </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class DependencyPropertyAttribute<T> : Attribute
{
    public DependencyPropertyAttribute(string name, string propertyChanged = "")
    {
        Name = name;
        PropertyChanged = propertyChanged;
    }

    public string Name { get; }

    public string PropertyChanged { get; }

    public bool IsSetterPublic { get; init; } = true;

    public bool IsNullable { get; init; } = true;

    public string DefaultValue { get; init; } = "DependencyProperty.UnsetValue";
}
