using ClassDiagramEditor.Commands;
using ClassDiagramEditor.Controls;
using ClassDiagramEditor.Models;
using ClassDiagramEditor.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

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
    private readonly HashSet<ClassModel> _selectedClasses = new();
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

    // 後方互換性のため SelectedClass プロパティを維持
    public ClassModel? SelectedClass
    {
        get => _selectedClasses.FirstOrDefault();
        set
        {
            _selectedClasses.Clear();
            if (value != null)
            {
                _selectedClasses.Add(value);
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedClasses));
            SelectedClassChanged?.Invoke(this, value);
            (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    // ← 複数選択用プロパティを追加
    public IReadOnlyCollection<ClassModel> SelectedClasses => _selectedClasses;


    // ← 複数選択関連メソッドを追加
    public void SelectSingleClass(ClassModel classModel)
    {
        _selectedClasses.Clear();
        _selectedClasses.Add(classModel);
        OnPropertyChanged(nameof(SelectedClass));
        OnPropertyChanged(nameof(SelectedClasses));
        SelectedClassChanged?.Invoke(this, classModel);
        (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    public void ToggleClassSelection(ClassModel classModel)
    {
        if (_selectedClasses.Contains(classModel))
        {
            _selectedClasses.Remove(classModel);
        }
        else
        {
            _selectedClasses.Add(classModel);
        }
        OnPropertyChanged(nameof(SelectedClass));
        OnPropertyChanged(nameof(SelectedClasses));
        SelectedClassChanged?.Invoke(this, SelectedClass);
        (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    public void ClearSelection()
    {
        _selectedClasses.Clear();
        OnPropertyChanged(nameof(SelectedClass));
        OnPropertyChanged(nameof(SelectedClasses));
        SelectedClassChanged?.Invoke(this, null);
        (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    public bool IsClassSelected(ClassModel classModel)
    {
        return _selectedClasses.Contains(classModel);
    }

    public void SelectClassesInRectangle(Rect selectionRect, Dictionary<Guid, ClassBoxVisual> classVisuals)
    {
        foreach (var classModel in Diagram.Classes)
        {
            if (classVisuals.TryGetValue(classModel.Id, out var visual))
            {
                var classRect = new Rect(
                    classModel.Position,
                    new Size(visual.Width, visual.Height)
                );

                if (selectionRect.IntersectsWith(classRect))
                {
                    if (!_selectedClasses.Contains(classModel))
                    {
                        _selectedClasses.Add(classModel);
                    }
                }
            }
        }

        OnPropertyChanged(nameof(SelectedClass));
        OnPropertyChanged(nameof(SelectedClasses));
        (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    // ← 複数クラス移動用メソッドを追加
    public void MoveMultipleClasses(List<(ClassModel classModel, Point oldPosition, Point newPosition)> moves)
    {
        var command = new MoveMultipleClassesCommand(_diagram, moves);
        _commandManager.ExecuteCommand(command);
    }

    public void SelectAllClasses()
    {
        _selectedClasses.Clear();
        foreach (var classModel in Diagram.Classes)
        {
            _selectedClasses.Add(classModel);
        }
        OnPropertyChanged(nameof(SelectedClass));
        OnPropertyChanged(nameof(SelectedClasses));
        StatusMessage = $"{_selectedClasses.Count}個のクラスを選択";
        (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    public event EventHandler<ClassModel?>? SelectedClassChanged;

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
    // [2026-03-24 追加] 名前を付けて保存コマンド
    public ICommand SaveAsCommand { get; private set; } = null!;
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
    public ICommand AddAggregationCommand { get; private set; } = null!;
    public ICommand AddCompositionCommand { get; private set; } = null!;

    public ICommand UndoCommand { get; private set; } = null!;
    public ICommand RedoCommand { get; private set; } = null!;

    public ICommand ZoomInCommand { get; private set; } = null!;
    public ICommand ZoomOutCommand { get; private set; } = null!;
    public ICommand ZoomResetCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        NewDiagramCommand = new RelayCommand(NewDiagram);
        SaveCommand = new RelayCommand(SaveDiagram);
        // [2026-03-24 追加] 名前を付けて保存コマンドを初期化
        SaveAsCommand = new RelayCommand(SaveDiagramAs);
        LoadCommand = new RelayCommand(LoadDiagram);
        ExportCommand = new RelayCommand(ExportDiagram);

        AddClassCommand = new RelayCommand(() => AddClass(ClassType.Class));
        AddInterfaceCommand = new RelayCommand(() => AddClass(ClassType.Interface));
        AddAbstractClassCommand = new RelayCommand(() => AddClass(ClassType.AbstractClass));
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => _selectedClasses.Count > 0);

        AddInheritanceCommand = new RelayCommand(() => StartAddingRelation(RelationType.Inheritance));
        AddImplementationCommand = new RelayCommand(() => StartAddingRelation(RelationType.Implementation));
        AddAssociationCommand = new RelayCommand(() => StartAddingRelation(RelationType.Association));
        AddDependencyCommand = new RelayCommand(() => StartAddingRelation(RelationType.Dependency));
        AddAggregationCommand = new RelayCommand(() => StartAddingRelation(RelationType.Aggregation));
        AddCompositionCommand = new RelayCommand(() => StartAddingRelation(RelationType.Composition));

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
        // [2026-03-24 修正] フィールド直接代入からプロパティ経由に変更し、PropertyChangedを発火させる
        Diagram = new DiagramModel();
        _commandManager.Clear();
        SelectedClass = null;
        SelectedRelation = null;
        _currentFilePath = null;
        OnPropertyChanged(nameof(Classes));
        OnPropertyChanged(nameof(Relations));
        StatusMessage = "New diagram created";

        // [2026-03-24 追加] 新規作成時にウィンドウタイトルを更新
        UpdateWindowTitle();
    }

    // [2026-03-24 修正] 既存ファイルがある場合は上書き保存、ない場合はダイアログを表示
    private void SaveDiagram()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                // ファイルパスが未設定の場合はダイアログを表示
                SaveDiagramAs();
                return;
            }

            _fileService.SaveDiagram(_diagram, _currentFilePath);
            StatusMessage = $"Saved: {Path.GetFileName(_currentFilePath)}";

            // [2026-03-24 追加] 保存後にウィンドウタイトルを更新
            UpdateWindowTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save diagram: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Save failed";
        }
    }

    // [2026-03-24 追加] 名前を付けて保存：常にダイアログを表示して保存先を指定する
    private void SaveDiagramAs()
    {
        try
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
                _fileService.SaveDiagram(_diagram, _currentFilePath);
                StatusMessage = $"Saved: {Path.GetFileName(_currentFilePath)}";

                // [2026-03-24 追加] 保存後にウィンドウタイトルを更新
                UpdateWindowTitle();
            }
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

                // [2026-03-24 修正] フィールド直接代入からプロパティ経由に変更し、PropertyChangedを発火させる
                Diagram = loadedDiagram;
                _currentFilePath = dialog.FileName;
                _commandManager.Clear();
                SelectedClass = null;
                SelectedRelation = null;

                OnPropertyChanged(nameof(Classes));
                OnPropertyChanged(nameof(Relations));

                StatusMessage = $"Loaded: {Path.GetFileName(dialog.FileName)}";

                // [2026-03-24 追加] 読み込み後にウィンドウタイトルを更新
                UpdateWindowTitle();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load diagram: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Load failed";
        }
    }

    // [2026-03-24 修正] ExportCommandをダイアログ経由の本実装に変更
    private void ExportDiagram()
    {
        // MainWindow側でダイアログを開くためにイベントを発火
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    // [2026-03-24 追加] エクスポートダイアログ表示要求イベント
    public event EventHandler? ExportRequested;

    // [2026-03-25 修正] 透過背景フラグを廃止。PNG・クリップボードは常にグリッド透過で出力
    public void ExportToPng(UIElement canvas, string filePath, Rect bounds, double padding)
    {
        try
        {
            _exportService.ExportToPng(canvas, filePath, bounds, padding, transparentBackground: true);
            StatusMessage = $"Exported: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Export failed";
        }
    }

    // [2026-03-24 追加] バウンディングボックスを受け取ってSVG出力
    public void ExportToSvg(string filePath, Rect bounds, double padding)
    {
        try
        {
            _exportService.ExportToSvg(null!, filePath, bounds, padding,
                _diagram.Classes, _diagram.Relations);
            StatusMessage = $"Exported: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Export failed";
        }
    }

    // [2026-03-25 修正] 透過背景フラグを廃止。PNG・クリップボードは常にグリッド透過で出力
    public void ExportToClipboard(UIElement canvas, Rect bounds, double padding)
    {
        try
        {
            _exportService.CopyToClipboard(canvas, bounds, padding, transparentBackground: true);
            StatusMessage = "クリップボードにコピーしました";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Copy failed";
        }
    }

    // [2026-03-24 追加] 現在の図のバウンディングボックスを計算して返す
    // クラスが1つもない場合は Empty を返す
    public Rect GetDiagramBounds()
    {
        if (_diagram.Classes.Count == 0) return Rect.Empty;

        // 暫定サイズ（ClassBoxVisualの計算と合わせる）
        const double classMinWidth = 150;
        const double classHeaderH = 35;
        // [2026-03-26 追加] ステレオタイプ表示時の追加ヘッダー高さ（ClassBoxVisualと合わせる）
        const double stereotypeExtraH = 14;
        const double classLineH = 20;
        const double classPad = 10;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var cls in _diagram.Classes)
        {
            // [2026-03-26 修正] ステレオタイプがある場合はヘッダー高さを増やす
            bool hasStereotype = cls.Type != Models.ClassType.Class;
            double headerH = classHeaderH + (hasStereotype ? stereotypeExtraH : 0);

            double h = headerH;
            if (cls.Attributes.Count > 0) h += classPad + cls.Attributes.Count * classLineH;
            if (cls.Methods.Count > 0) h += classPad + cls.Methods.Count * classLineH;
            h += classPad;

            minX = Math.Min(minX, cls.Position.X);
            minY = Math.Min(minY, cls.Position.Y);
            maxX = Math.Max(maxX, cls.Position.X + classMinWidth);
            maxY = Math.Max(maxY, cls.Position.Y + h);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
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
        if (_selectedClasses.Count > 0)
        {
            var classesToDelete = _selectedClasses.ToList();

            foreach (var classModel in classesToDelete)
            {
                var command = new RemoveClassCommand(_diagram, classModel);
                _commandManager.ExecuteCommand(command);
            }

            StatusMessage = $"{classesToDelete.Count}個のクラスを削除しました";
            ClearSelection();
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

    // [2026-03-24 追加] ウィンドウタイトルを現在のファイル状態に応じて更新する
    // ファイル未開時は「新規作成」、開いている場合はファイル名を表示
    public void UpdateWindowTitle()
    {
        var fileName = string.IsNullOrEmpty(_currentFilePath)
            ? "新規作成"
            : Path.GetFileName(_currentFilePath);

        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.Title = $"Class Diagram Editor - {fileName}";
        }
    }

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

    /// <summary>
    /// 関係を削除する
    /// </summary>
    public void RemoveRelation(RelationModel relation)
    {
        var command = new RemoveRelationCommand(_diagram, relation);
        _commandManager.ExecuteCommand(command);

        var sourceClass = _diagram.Classes.FirstOrDefault(c => c.Id == relation.SourceClassId);
        var targetClass = _diagram.Classes.FirstOrDefault(c => c.Id == relation.TargetClassId);

        string message = $"{relation.Type}関係を削除しました";
        if (sourceClass != null && targetClass != null)
        {
            message += $" ({sourceClass.Name} → {targetClass.Name})";
        }

        StatusMessage = message;
    }

    public event EventHandler<RelationType>? RelationModeRequested;

    #endregion
}