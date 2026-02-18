using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Views.Components;

public class EnumComboBox : ComboBox
{
    private static readonly Dictionary<Type, EnumItem[]> EnumItemsCache = new();
    private static readonly Lock EnumItemsCacheLock = new();

    public static readonly DependencyProperty EnumTypeProperty = DependencyProperty.Register("EnumType", typeof(Type),
        typeof(EnumComboBox), new PropertyMetadata(null, OnEnumTypeChanged));

    public Type EnumType
    {
        get => (Type)GetValue(EnumTypeProperty);
        set => SetValue(EnumTypeProperty, value);
    }

    private static void OnEnumTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var enumComboBox = (EnumComboBox)d;
        var enumType = enumComboBox.EnumType;
        if (enumType?.IsEnum == true)
        {
            enumComboBox.ItemsSource = GetOrCreateEnumItems(enumType);
            enumComboBox.DisplayMemberPath = "Display";
            enumComboBox.SelectedValuePath = "Value";
        }
        else
        {
            enumComboBox.ItemsSource = null;
        }
    }

    private static EnumItem[] GetOrCreateEnumItems(Type enumType)
    {
        using (EnumItemsCacheLock.EnterScope())
        {
            if (EnumItemsCache.TryGetValue(enumType, out var items))
            {
                return items;
            }

            items = (from Enum enumValue in Enum.GetValues(enumType)
                select new EnumItem(enumValue, GetEnumDescription(enumValue))).ToArray();
            EnumItemsCache[enumType] = items;
            return items;
        }
    }

    private static string GetEnumDescription(Enum value)
    {
        var descriptionAttribute =
            value.GetType().GetField(value.ToString())?.GetCustomAttribute<DescriptionAttribute>();
        if (descriptionAttribute != null)
        {
            return descriptionAttribute.Description;
        }
        return value.ToString();
    }

    private class EnumItem(Enum value, string display)
    {
        public Enum Value { get; set; } = value;

        public string Display { get; set; } = display;
    }
}
