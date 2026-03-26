using System.Windows;
using System.Windows.Controls;
using ClassDiagramEditor.Models;

namespace ClassDiagramEditor.Dialogs;

/// <summary>
/// メソッド追加・編集ダイアログ
/// </summary>
public partial class AddMethodDialog : Window
{
    public MethodModel? Result { get; private set; }

    // [2026-03-26 追加] 編集対象の既存メソッド（新規追加時はnull）
    private readonly MethodModel? _editTarget;

    public AddMethodDialog()
    {
        InitializeComponent();
        NameTextBox.Focus();
    }

    // [2026-03-26 追加] 編集モード用コンストラクタ。既存メソッドの値をフォームに反映する
    public AddMethodDialog(MethodModel editTarget) : this()
    {
        _editTarget = editTarget;
        Title = "メソッドを編集";

        NameTextBox.Text = editTarget.Name;
        ReturnTypeComboBox.Text = editTarget.ReturnType;

        // アクセス修飾子の初期選択をeditTargetに合わせる
        foreach (ComboBoxItem item in AccessModifierComboBox.Items)
        {
            if (string.Equals(item.Content?.ToString(), editTarget.AccessModifier.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                AccessModifierComboBox.SelectedItem = item;
                break;
            }
        }
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

        // [2026-03-26 修正] 編集モード時は既存オブジェクトを直接更新してResultに設定する
        if (_editTarget != null)
        {
            _editTarget.Name = name;
            _editTarget.ReturnType = returnType;
            _editTarget.AccessModifier = accessModifier;
            Result = _editTarget;
        }
        else
        {
            Result = new MethodModel
            {
                Name = name,
                ReturnType = returnType,
                AccessModifier = accessModifier
            };
        }

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