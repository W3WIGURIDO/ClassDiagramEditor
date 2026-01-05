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
}