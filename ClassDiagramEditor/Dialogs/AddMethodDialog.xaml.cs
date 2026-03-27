using System.Collections.ObjectModel;
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

    // 編集対象の既存メソッド（新規追加時はnull）
    private readonly MethodModel? _editTarget;

    // [2026-03-27 追加] パラメータ編集用の一時リスト。ItemsControlにバインドする
    private readonly ObservableCollection<ParameterModel> _parameters = [];

    public AddMethodDialog()
    {
        InitializeComponent();
        // [2026-03-27 追加] ItemsControlのItemsSourceに一時リストを設定
        ParameterItemsControl.ItemsSource = _parameters;
        NameTextBox.Focus();
    }

    // 編集モード用コンストラクタ。既存メソッドの値をフォームに反映する
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

        // [2026-03-27 追加] 既存パラメータを一時リストにコピーして編集可能にする
        foreach (var param in editTarget.Parameters)
        {
            _parameters.Add(new ParameterModel
            {
                Name = param.Name,
                DataType = param.DataType
            });
        }
    }

    // [2026-03-27 追加] 「➕ パラメータを追加」ボタン押下：空行を1件追加
    private void AddParameter_Click(object sender, RoutedEventArgs e)
    {
        _parameters.Add(new ParameterModel
        {
            Name = string.Empty,
            DataType = "object"
        });
    }

    // [2026-03-27 追加] 「🗑️」ボタン押下：対象パラメータ行を削除
    private void DeleteParameter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ParameterModel param)
        {
            _parameters.Remove(param);
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

        // [2026-03-27 追加] 名前が空の行を除外し、型が空の場合は"object"で補完する
        var validParams = _parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new ParameterModel
            {
                Name = p.Name.Trim(),
                DataType = string.IsNullOrWhiteSpace(p.DataType) ? "object" : p.DataType.Trim()
            })
            .ToList();

        if (_editTarget != null)
        {
            // 編集モード：既存オブジェクトを直接更新
            _editTarget.Name = name;
            _editTarget.ReturnType = returnType;
            _editTarget.AccessModifier = accessModifier;

            // [2026-03-27 追加] パラメータを洗い替え（既存をクリアして再追加）
            _editTarget.Parameters.Clear();
            foreach (var p in validParams)
            {
                _editTarget.Parameters.Add(p);
            }

            Result = _editTarget;
        }
        else
        {
            // 新規追加モード
            var method = new MethodModel
            {
                Name = name,
                ReturnType = returnType,
                AccessModifier = accessModifier
            };

            // [2026-03-27 追加] バリデーション済みパラメータを新規メソッドに追加
            foreach (var p in validParams)
            {
                method.Parameters.Add(p);
            }

            Result = method;
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