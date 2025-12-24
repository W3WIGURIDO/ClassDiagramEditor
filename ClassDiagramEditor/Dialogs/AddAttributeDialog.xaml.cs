using System.Windows;
using System.Windows.Controls;
using ClassDiagramEditor.Models;

namespace ClassDiagramEditor.Dialogs;

/// <summary>
/// 属性追加ダイアログ
/// </summary>
public partial class AddAttributeDialog : Window
{
    public AttributeModel? Result { get; private set; }

    public AddAttributeDialog()
    {
        InitializeComponent();
        NameTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("属性名を入力してください", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dataType = DataTypeComboBox.Text.Trim();
        if (string.IsNullOrEmpty(dataType))
        {
            dataType = "object";
        }

        var accessModifier = ParseAccessModifier(
            (AccessModifierComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString());

        Result = new AttributeModel
        {
            Name = name,
            DataType = dataType,
            AccessModifier = accessModifier
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static AccessModifier ParseAccessModifier(string? text)
    {
        return text?.ToLower() switch
        {
            "public" => AccessModifier.Public,
            "protected" => AccessModifier.Protected,
            "internal" => AccessModifier.Internal,
            _ => AccessModifier.Private
        };
    }
}