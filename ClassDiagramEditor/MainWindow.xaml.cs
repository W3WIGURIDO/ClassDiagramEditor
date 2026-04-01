using ClassDiagramEditor.Dialogs;
using ClassDiagramEditor.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClassDiagramEditor;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;

        KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel == null) return;

        if (e.Key == Key.A && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _viewModel.SelectAllClasses();
            DiagramCanvas.InvalidateVisual();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.ClearSelection();
            DiagramCanvas.InvalidateVisual();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            if (_viewModel.SelectedClasses.Count > 0)
            {
                _viewModel.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.RelationModeRequested += (s, type) =>
                DiagramCanvas.StartAddingRelation(type);

            _viewModel.SelectedClassChanged += (s, classModel) =>
                DiagramCanvas.InvalidateVisual();

            _viewModel.ExportRequested += ViewModel_ExportRequested;
            // [2026-04-01 追加] 関係線右クリックイベントを購読してコンテキストメニューを構築する
            DiagramCanvas.RelationRightClicked += DiagramCanvas_RelationRightClicked;
        }
    }

    private void ViewModel_ExportRequested(object? sender, EventArgs e)
    {
        if (_viewModel == null) return;

        // [2026-03-26 修正] 実描画サイズを取得してBounds計算・SVGエクスポートに使用
        var classSizes = DiagramCanvas.GetClassSizes();
        var bounds = _viewModel.GetDiagramBounds(classSizes);

        if (bounds.IsEmpty)
        {
            MessageBox.Show("エクスポートするクラスがありません。", "エクスポート",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Dialogs.ExportDialog { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result == null) return;

        var result = dialog.Result;
        double pad = result.Padding;

        switch (result.Format)
        {
            case Dialogs.ExportFormat.Png:
                {
                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "PNG Image (*.png)|*.png",
                        DefaultExt = ".png",
                        FileName = _viewModel.Diagram.Name
                    };
                    if (saveDialog.ShowDialog() == true)
                    {
                        RunExportWithTransparentBackground(() =>
                            _viewModel.ExportToPng(DiagramCanvas, saveDialog.FileName, bounds, pad),
                            transparent: true);
                    }
                    break;
                }
            case Dialogs.ExportFormat.Svg:
                {
                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "SVG Image (*.svg)|*.svg",
                        DefaultExt = ".svg",
                        FileName = _viewModel.Diagram.Name
                    };
                    if (saveDialog.ShowDialog() == true)
                        // [2026-03-26 修正] classSizesを渡す
                        _viewModel.ExportToSvg(saveDialog.FileName, bounds, pad, classSizes);
                    break;
                }
            case Dialogs.ExportFormat.Clipboard:
                RunExportWithTransparentBackground(() =>
                    _viewModel.ExportToClipboard(DiagramCanvas, bounds, pad),
                    transparent: true);
                break;
        }
    }

    // [2026-04-01 追加] 関係線右クリック時のコンテキストメニューを構築して表示する
    private void DiagramCanvas_RelationRightClicked(object? sender, ClassDiagramEditor.Models.RelationModel relation)
    {
        if (_viewModel == null) return;

        var contextMenu = new ContextMenu();

        // 関係線削除
        var deleteRelationMenuItem = new MenuItem
        {
            Header = "🗑️ この関係を削除",
            FontSize = 13
        };
        deleteRelationMenuItem.Click += (s, args) =>
        {
            _viewModel.RemoveRelation(relation);
        };
        contextMenu.Items.Add(deleteRelationMenuItem);

        contextMenu.Items.Add(new Separator());

        // [2026-04-01 追加] ラベル追加・編集
        var labelEditMenuItem = new MenuItem
        {
            Header = string.IsNullOrEmpty(relation.Label)
                ? "🏷️ ラベルを追加"
                : "🏷️ ラベルを編集",
            FontSize = 13
        };
        labelEditMenuItem.Click += (s, args) =>
        {
            DiagramCanvas.StartLabelEdit(relation);
        };
        contextMenu.Items.Add(labelEditMenuItem);

        // [2026-04-01 追加] ラベル削除（ラベルがある場合のみ有効）
        var deleteLabelMenuItem = new MenuItem
        {
            Header = "🏷️ ラベルを削除",
            FontSize = 13,
            IsEnabled = !string.IsNullOrEmpty(relation.Label)
        };
        deleteLabelMenuItem.Click += (s, args) =>
        {
            _viewModel.EditRelationLabel(relation, relation.Label, string.Empty);
            DiagramCanvas.InvalidateVisual();
        };
        contextMenu.Items.Add(deleteLabelMenuItem);

        contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        contextMenu.IsOpen = true;
    }

    private void AddAttribute_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedClass == null)
            return;

        var dialog = new AddAttributeDialog
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _viewModel.SelectedClass.Attributes.Add(dialog.Result);
            _viewModel.Diagram.MarkAsModified();
            _viewModel.StatusMessage = $"属性 '{dialog.Result.Name}' を追加しました";
        }
    }

    private void AddMethod_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedClass == null)
            return;

        var dialog = new AddMethodDialog
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _viewModel.SelectedClass.Methods.Add(dialog.Result);
            _viewModel.Diagram.MarkAsModified();
            _viewModel.StatusMessage = $"メソッド '{dialog.Result.Name}' を追加しました";
        }
    }

    // [2026-03-26 追加] 属性リストのダブルクリックで編集ダイアログを開く
    private void AttributeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is Models.AttributeModel attr)
        {
            OpenEditAttributeDialog(attr);
        }
    }

    // [2026-03-26 追加] 属性の右クリックメニュー「編集」から編集ダイアログを開く
    private void EditAttribute_Click(object sender, RoutedEventArgs e)
    {
        if (AttributeListBox.SelectedItem is Models.AttributeModel attr)
        {
            OpenEditAttributeDialog(attr);
        }
    }

    // [2026-03-26 追加] 属性編集ダイアログを開いて結果を反映する共通処理
    private void OpenEditAttributeDialog(Models.AttributeModel attr)
    {
        if (_viewModel?.SelectedClass == null) return;

        var dialog = new AddAttributeDialog(attr)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            // 編集モードでは既存オブジェクトを直接更新しているため
            // CollectionChangedは発火しない。InvalidateVisualで再描画する
            _viewModel.Diagram.MarkAsModified();
            _viewModel.StatusMessage = $"属性 '{attr.Name}' を編集しました";
            DiagramCanvas.InvalidateVisual();
        }
    }

    // [2026-03-26 追加] 属性の右クリックメニュー「削除」で選択中の属性を削除する
    private void DeleteAttribute_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedClass == null) return;
        if (AttributeListBox.SelectedItem is not Models.AttributeModel attr) return;

        var result = MessageBox.Show(
            $"属性 '{attr.Name}' を削除しますか？",
            "属性の削除",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.OK)
        {
            _viewModel.SelectedClass.Attributes.Remove(attr);
            _viewModel.Diagram.MarkAsModified();
            _viewModel.StatusMessage = $"属性 '{attr.Name}' を削除しました";
        }
    }

    // [2026-03-26 追加] メソッドリストのダブルクリックで編集ダイアログを開く
    private void MethodListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is Models.MethodModel method)
        {
            OpenEditMethodDialog(method);
        }
    }

    // [2026-03-26 追加] メソッドの右クリックメニュー「編集」から編集ダイアログを開く
    private void EditMethod_Click(object sender, RoutedEventArgs e)
    {
        if (MethodListBox.SelectedItem is Models.MethodModel method)
        {
            OpenEditMethodDialog(method);
        }
    }

    // [2026-03-26 追加] メソッド編集ダイアログを開いて結果を反映する共通処理
    private void OpenEditMethodDialog(Models.MethodModel method)
    {
        if (_viewModel?.SelectedClass == null) return;

        var dialog = new AddMethodDialog(method)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            // 編集モードでは既存オブジェクトを直接更新しているため
            // CollectionChangedは発火しない。InvalidateVisualで再描画する
            _viewModel.Diagram.MarkAsModified();
            _viewModel.StatusMessage = $"メソッド '{method.Name}' を編集しました";
            DiagramCanvas.InvalidateVisual();
        }
    }

    // [2026-03-26 追加] メソッドの右クリックメニュー「削除」で選択中のメソッドを削除する
    private void DeleteMethod_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedClass == null) return;
        if (MethodListBox.SelectedItem is not Models.MethodModel method) return;

        var result = MessageBox.Show(
            $"メソッド '{method.Name}' を削除しますか？",
            "メソッドの削除",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.OK)
        {
            _viewModel.SelectedClass.Methods.Remove(method);
            _viewModel.Diagram.MarkAsModified();
            _viewModel.StatusMessage = $"メソッド '{method.Name}' を削除しました";
        }
    }

    private void RunExportWithTransparentBackground(Action action, bool transparent)
    {
        if (!transparent)
        {
            action();
            return;
        }

        GridBackground.Visibility = Visibility.Hidden;

        try
        {
            DiagramCanvas.RunWithTransparentBackground(action, transparent);
        }
        finally
        {
            GridBackground.Visibility = Visibility.Visible;
        }
    }
}