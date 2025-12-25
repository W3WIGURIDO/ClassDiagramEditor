using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ClassDiagramEditor.Models;

namespace ClassDiagramEditor.Commands;

/// <summary>
/// Undo/Redo可能なコマンドのインターフェース
/// </summary>
public interface IDiagramCommand
{
    void Execute();
    void Undo();
    string Description { get; }
}

/// <summary>
/// Undo/Redoを管理するクラス
/// </summary>
public class DiagramCommandManager : INotifyPropertyChanged
{
    private readonly Stack<IDiagramCommand> _undoStack = new();
    private readonly Stack<IDiagramCommand> _redoStack = new();


    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void ExecuteCommand(IDiagramCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    public void Undo()
    {
        if (!CanUndo) return;

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// クラス追加コマンド
/// </summary>
public class AddClassCommand(DiagramModel diagram, ClassModel classModel) : IDiagramCommand
{
    private readonly DiagramModel _diagram = diagram;
    private readonly ClassModel _class = classModel;

    public string Description => $"Add class '{_class.Name}'";

    public void Execute()
    {
        _diagram.Classes.Add(_class);
        _diagram.MarkAsModified();
    }

    public void Undo()
    {
        _diagram.Classes.Remove(_class);
        _diagram.MarkAsModified();
    }
}

/// <summary>
/// クラス削除コマンド
/// </summary>
public class RemoveClassCommand(DiagramModel diagram, ClassModel classModel) : IDiagramCommand
{
    private readonly DiagramModel _diagram = diagram;
    private readonly ClassModel _class = classModel;
    private readonly List<RelationModel> _removedRelations = new();

    public string Description => $"Remove class '{_class.Name}'";

    public void Execute()
    {
        _removedRelations.Clear();
        var relationsToRemove = _diagram.Relations
            .Where(r => r.SourceClassId == _class.Id || r.TargetClassId == _class.Id)
            .ToList();

        foreach (var relation in relationsToRemove)
        {
            _removedRelations.Add(relation);
            _diagram.Relations.Remove(relation);
        }

        _diagram.Classes.Remove(_class);
        _diagram.MarkAsModified();
    }

    public void Undo()
    {
        _diagram.Classes.Add(_class);

        foreach (var relation in _removedRelations)
        {
            _diagram.Relations.Add(relation);
        }

        _diagram.MarkAsModified();
    }
}

/// <summary>
/// クラス移動コマンド
/// </summary>
public class MoveClassCommand(DiagramModel diagram, ClassModel classModel, Point oldPosition, Point newPosition) : IDiagramCommand
{
    private readonly DiagramModel _diagram = diagram;
    private readonly ClassModel _class = classModel;
    private readonly Point _oldPosition = oldPosition;
    private readonly Point _newPosition = newPosition;

    public string Description => $"Move class '{_class.Name}'";

    public void Execute()
    {
        _class.Position = _newPosition;
        _diagram.MarkAsModified();
    }

    public void Undo()
    {
        _class.Position = _oldPosition;
        _diagram.MarkAsModified();
    }
}

/// <summary>
/// 関係追加コマンド
/// </summary>
public class AddRelationCommand(DiagramModel diagram, RelationModel relation) : IDiagramCommand
{
    private readonly DiagramModel _diagram = diagram;
    private readonly RelationModel _relation = relation;

    public string Description => $"Add relation ({_relation.Type})";

    public void Execute()
    {
        _diagram.Relations.Add(_relation);
        _diagram.MarkAsModified();
    }

    public void Undo()
    {
        _diagram.Relations.Remove(_relation);
        _diagram.MarkAsModified();
    }
}