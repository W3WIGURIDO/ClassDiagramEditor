using System.Windows;
using System.Windows.Controls;
using ClassDiagramEditor.Models;

namespace ClassDiagramEditor.Dialogs;

/// <summary>
/// 属性追加・編集ダイアログ
/// </summary>
public partial class AddAttributeDialog : Window
{
    public AttributeModel? Result { get; private set; }

    // [2026-03-26 追加] 編集対象の既存属性（新規追加時はnull）
    private readonly AttributeModel? _editTarget;

    public AddAttributeDialog()
    {
        InitializeComponent();
        NameTextBox.Focus();
    }

    // [2026-03-26 追加] 編集モード用コンストラクタ。既存属性の値をフォームに反映する
    public AddAttributeDialog(AttributeModel editTarget) : this()
    {
        _editTarget = editTarget;
        Title = "属性を編集";

        NameTextBox.Text = editTarget.Name;
        DataTypeComboBox.Text = editTarget.DataType;

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

        // [2026-03-26 修正] 編集モード時は既存オブジェクトを直接更新してResultに設定する
        if (_editTarget != null)
        {
            _editTarget.Name = name;
            _editTarget.DataType = dataType;
            _editTarget.AccessModifier = accessModifier;
            Result = _editTarget;
        }
        else
        {
            Result = new AttributeModel
            {
                Name = name,
                DataType = dataType,
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
            "public" => AccessModifier.Public,
            "protected" => AccessModifier.Protected,
            "internal" => AccessModifier.Internal,
            _ => AccessModifier.Private
        };
    }
}