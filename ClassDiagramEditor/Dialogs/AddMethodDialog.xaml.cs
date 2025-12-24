using System.Windows;
using System.Windows.Controls;
using ClassDiagramEditor.Models;

namespace ClassDiagramEditor.Dialogs;

/// <summary>
/// メソッド追加ダイアログ
/// </summary>
public partial class AddMethodDialog : Window
{
    public MethodModel? Result { get; private set; }

    public AddMethodDialog()
    {
        InitializeComponent();
        NameTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("メソッド名を入力してください", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var returnType = ReturnTypeComboBox.Text.Trim();
        if (string.IsNullOrEmpty(returnType))
        {
            returnType = "void";
        }

        var accessModifier = ParseAccessModifier(
            (AccessModifierComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString());

        Result = new MethodModel
        {
            Name = name,
            ReturnType = returnType,
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
            "private" => AccessModifier.Private,
            "protected" => AccessModifier.Protected,
            "internal" => AccessModifier.Internal,
            _ => AccessModifier.Public
        };
    }
}