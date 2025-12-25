using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ClassDiagramEditor.Commands;
using ClassDiagramEditor.Models;
using ClassDiagramEditor.Services;
using Microsoft.Win32;

namespace ClassDiagramEditor.ViewModels;

/// <summary>
/// ViewModelの基底クラス
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// ICommandの汎用実装
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        // ✅ 完全修飾名で指定
        add => System.Windows.Input.CommandManager.RequerySuggested += value;
        remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => System.Windows.Input.CommandManager.InvalidateRequerySuggested();
}

/// <summary>
/// メインウィンドウのViewModel
/// </summary>
public class MainViewModel : ViewModelBase
{
    private DiagramModel _diagram;
    private DiagramCommandManager _commandManager;
    private FileService _fileService;
    private ExportService _exportService;
    private ClassModel? _selectedClass;
    private RelationModel? _selectedRelation;
    private double _zoomLevel = 1.0;
    private string _statusMessage = "Ready";
    private string? _currentFilePath;

    public MainViewModel()
    {
        _diagram = new DiagramModel();
        _commandManager = new DiagramCommandManager();
        _fileService = new FileService();
        _exportService = new ExportService();

        InitializeCommands();
    }

    #region Properties

    public DiagramModel Diagram
    {
        get => _diagram;
        set => SetProperty(ref _diagram, value);
    }

    public ObservableCollection<ClassModel> Classes => _diagram.Classes;
    public ObservableCollection<RelationModel> Relations => _diagram.Relations;

    public ClassModel? SelectedClass
    {
        get => _selectedClass;
        set => SetProperty(ref _selectedClass, value);
    }

    public RelationModel? SelectedRelation
    {
        get => _selectedRelation;
        set => SetProperty(ref _selectedRelation, value);
    }

    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (value is >= 0.1 and <= 3.0)
            {
                SetProperty(ref _zoomLevel, value);
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool CanUndo => _commandManager.CanUndo;
    public bool CanRedo => _commandManager.CanRedo;

    #endregion

    #region Commands

    public ICommand NewDiagramCommand { get; private set; } = null!;
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand LoadCommand { get; private set; } = null!;
    public ICommand ExportCommand { get; private set; } = null!;

    public ICommand AddClassCommand { get; private set; } = null!;
    public ICommand AddInterfaceCommand { get; private set; } = null!;
    public ICommand AddAbstractClassCommand { get; private set; } = null!;
    public ICommand DeleteSelectedCommand { get; private set; } = null!;

    public ICommand AddInheritanceCommand { get; private set; } = null!;
    public ICommand AddImplementationCommand { get; private set; } = null!;
    public ICommand AddAssociationCommand { get; private set; } = null!;
    public ICommand AddDependencyCommand { get; private set; } = null!;

    public ICommand UndoCommand { get; private set; } = null!;
    public ICommand RedoCommand { get; private set; } = null!;

    public ICommand ZoomInCommand { get; private set; } = null!;
    public ICommand ZoomOutCommand { get; private set; } = null!;
    public ICommand ZoomResetCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        NewDiagramCommand = new RelayCommand(NewDiagram);
        SaveCommand = new RelayCommand(SaveDiagram);
        LoadCommand = new RelayCommand(LoadDiagram);
        ExportCommand = new RelayCommand(ExportDiagram);

        AddClassCommand = new RelayCommand(() => AddClass(ClassType.Class));
        AddInterfaceCommand = new RelayCommand(() => AddClass(ClassType.Interface));
        AddAbstractClassCommand = new RelayCommand(() => AddClass(ClassType.AbstractClass));
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedClass != null);

        AddInheritanceCommand = new RelayCommand(() => StartAddingRelation(RelationType.Inheritance));
        AddImplementationCommand = new RelayCommand(() => StartAddingRelation(RelationType.Implementation));
        AddAssociationCommand = new RelayCommand(() => StartAddingRelation(RelationType.Association));
        AddDependencyCommand = new RelayCommand(() => StartAddingRelation(RelationType.Dependency));

        UndoCommand = new RelayCommand(Undo, () => CanUndo);
        RedoCommand = new RelayCommand(Redo, () => CanRedo);

        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        ZoomResetCommand = new RelayCommand(ZoomReset);

        _commandManager.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        };
    }

    #endregion

    #region Command Implementations

    private void NewDiagram()
    {
        _diagram = new DiagramModel();
        _commandManager.Clear();
        SelectedClass = null;
        SelectedRelation = null;
        _currentFilePath = null;
        OnPropertyChanged(nameof(Diagram));
        OnPropertyChanged(nameof(Classes));
        OnPropertyChanged(nameof(Relations));
        StatusMessage = "New diagram created";
    }

    private void SaveDiagram()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Class Diagram Files (*.cdf)|*.cdf|All Files (*.*)|*.*",
                    DefaultExt = ".cdf",
                    FileName = _diagram.Name
                };

                if (dialog.ShowDialog() == true)
                {
                    _currentFilePath = dialog.FileName;
                }
                else
                {
                    return;
                }
            }

            _fileService.SaveDiagram(_diagram, _currentFilePath);
            StatusMessage = $"Saved: {Path.GetFileName(_currentFilePath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save diagram: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Save failed";
        }
    }

    private void LoadDiagram()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Class Diagram Files (*.cdf)|*.cdf|All Files (*.*)|*.*",
                DefaultExt = ".cdf"
            };

            if (dialog.ShowDialog() == true)
            {
                var loadedDiagram = _fileService.LoadDiagram(dialog.FileName);
                _diagram = loadedDiagram;
                _currentFilePath = dialog.FileName;
                _commandManager.Clear();
                SelectedClass = null;
                SelectedRelation = null;

                OnPropertyChanged(nameof(Diagram));
                OnPropertyChanged(nameof(Classes));
                OnPropertyChanged(nameof(Relations));

                StatusMessage = $"Loaded: {Path.GetFileName(dialog.FileName)}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load diagram: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Load failed";
        }
    }

    private void ExportDiagram()
    {
        StatusMessage = "Export: Use File menu to export as PNG";
    }

    public void ExportToPng(UIElement canvas, string filePath)
    {
        try
        {
            _exportService.ExportToPng(canvas, filePath, 2000, 2000);
            StatusMessage = $"Exported: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Export failed";
        }
    }

    private void AddClass(ClassType type)
    {
        var newClass = new ClassModel
        {
            Name = $"New{type}",
            Type = type,
            Position = new Point(100, 100)
        };

        if (type is ClassType.Class or ClassType.AbstractClass)
        {
            newClass.Attributes.Add(new AttributeModel
            {
                Name = "field",
                DataType = "string",
                AccessModifier = AccessModifier.Private
            });

            newClass.Methods.Add(new MethodModel
            {
                Name = "Method",
                ReturnType = "void",
                AccessModifier = AccessModifier.Public
            });
        }
        else if (type == ClassType.Interface)
        {
            newClass.Methods.Add(new MethodModel
            {
                Name = "InterfaceMethod",
                ReturnType = "void",
                AccessModifier = AccessModifier.Public
            });
        }

        var command = new AddClassCommand(_diagram, newClass);
        _commandManager.ExecuteCommand(command);

        SelectedClass = newClass;
        StatusMessage = $"{type} added";
    }

    private void DeleteSelected()
    {
        if (SelectedClass != null)
        {
            var command = new RemoveClassCommand(_diagram, SelectedClass);
            _commandManager.ExecuteCommand(command);
            StatusMessage = $"Class '{SelectedClass.Name}' deleted";
            SelectedClass = null;
        }
    }

    private void StartAddingRelation(RelationType type)
    {
        StatusMessage = $"{type}関係を追加: 関係元のクラスをクリックしてください";
        RelationModeRequested?.Invoke(this, type);
    }

    private void Undo()
    {
        _commandManager.Undo();
        StatusMessage = "Undo";
    }

    private void Redo()
    {
        _commandManager.Redo();
        StatusMessage = "Redo";
    }

    private void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel + 0.1, 3.0);
        StatusMessage = $"Zoom: {ZoomLevel:P0}";
    }

    private void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel - 0.1, 0.1);
        StatusMessage = $"Zoom: {ZoomLevel:P0}";
    }

    private void ZoomReset()
    {
        ZoomLevel = 1.0;
        StatusMessage = "Zoom: 100%";
    }

    #endregion

    #region Public Methods

    public void MoveClass(ClassModel classModel, Point oldPosition, Point newPosition)
    {
        if (oldPosition != newPosition)
        {
            var command = new MoveClassCommand(_diagram, classModel, oldPosition, newPosition);
            _commandManager.ExecuteCommand(command);
        }
    }

    public void AddRelation(Guid sourceId, Guid targetId, RelationType type)
    {
        var relation = new RelationModel
        {
            SourceClassId = sourceId,
            TargetClassId = targetId,
            Type = type
        };

        var command = new AddRelationCommand(_diagram, relation);
        _commandManager.ExecuteCommand(command);
        StatusMessage = $"{type} relation added";
    }

    public event EventHandler<RelationType>? RelationModeRequested;

    #endregion
}