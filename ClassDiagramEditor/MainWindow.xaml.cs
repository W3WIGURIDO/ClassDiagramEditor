using System.Windows;
using ClassDiagramEditor.Dialogs;
using ClassDiagramEditor.ViewModels;

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
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.RelationModeRequested += (s, type) =>
                DiagramCanvas.StartAddingRelation(type);
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