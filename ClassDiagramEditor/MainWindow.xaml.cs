using ClassDiagramEditor.Dialogs;
using ClassDiagramEditor.ViewModels;
using System.Windows;
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

        // ← キーボードイベントを追加
        KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel == null) return;

        // Ctrl+A: すべて選択
        if (e.Key == Key.A && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _viewModel.SelectAllClasses();
            DiagramCanvas.InvalidateVisual();
            e.Handled = true;
        }
        // Escape: 選択解除
        else if (e.Key == Key.Escape)
        {
            _viewModel.ClearSelection();
            DiagramCanvas.InvalidateVisual();
            e.Handled = true;
        }
        // Delete: 選択削除
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

            // [2026-03-24 追加] エクスポートダイアログ要求イベントを購読
            _viewModel.ExportRequested += ViewModel_ExportRequested;
        }
    }

    // [2026-03-24 追加] エクスポートダイアログを開いて形式に応じた処理を実行
    private void ViewModel_ExportRequested(object? sender, EventArgs e)
    {
        if (_viewModel == null) return;

        // クラスが1つもない場合は警告
        var bounds = _viewModel.GetDiagramBounds();
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

        // [2026-03-25 修正] グリッド背景も含めて透過処理をRunExportWithTransparentBackground経由に統一
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
                            _viewModel.ExportToPng(DiagramCanvas, saveDialog.FileName, bounds, pad,
                                                   result.TransparentBackground),
                            result.TransparentBackground);
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
                        _viewModel.ExportToSvg(saveDialog.FileName, bounds, pad);
                    break;
                }
            case Dialogs.ExportFormat.Clipboard:
                RunExportWithTransparentBackground(() =>
                    _viewModel.ExportToClipboard(DiagramCanvas, bounds, pad,
                                                 result.TransparentBackground),
                    result.TransparentBackground);
                break;
        }
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

    // [2026-03-25 修正] 透過エクスポート時にグリッド背景Rectangleも非表示にする
    private void RunExportWithTransparentBackground(Action action, bool transparent)
    {
        if (!transparent)
        {
            action();
            return;
        }

        // グリッド背景レイヤーを非表示
        GridBackground.Visibility = Visibility.Hidden;

        DiagramCanvas.RunWithTransparentBackground(() =>
        {
            action();
        }, transparent);

        // 元に戻す
        GridBackground.Visibility = Visibility.Visible;
    }
}