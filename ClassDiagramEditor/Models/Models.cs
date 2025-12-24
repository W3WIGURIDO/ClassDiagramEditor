using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ClassDiagramEditor.Models;

/// <summary>
/// メソッドのパラメータ
/// </summary>
public class ParameterModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _dataType = "object";

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string DataType
    {
        get => _dataType;
        set => SetProperty(ref _dataType, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public override string ToString() => $"{Name}: {DataType}";
}

/// <summary>
/// クラスの属性（フィールド）
/// </summary>
public class AttributeModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _dataType = "object";
    private AccessModifier _accessModifier = AccessModifier.Private;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string DataType
    {
        get => _dataType;
        set
        {
            if (SetProperty(ref _dataType, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public AccessModifier AccessModifier
    {
        get => _accessModifier;
        set
        {
            if (SetProperty(ref _accessModifier, value))
            {
                OnPropertyChanged(nameof(AccessModifierSymbol));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string AccessModifierSymbol => _accessModifier.ToSymbol();
    public string DisplayText => $"{AccessModifierSymbol} {Name}: {DataType}";

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
/// クラスのメソッド
/// </summary>
public class MethodModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _returnType = "void";
    private AccessModifier _accessModifier = AccessModifier.Public;
    private ObservableCollection<ParameterModel> _parameters;

    public MethodModel()
    {
        _parameters = new ObservableCollection<ParameterModel>();
        _parameters.CollectionChanged += (s, e) => OnPropertyChanged(nameof(DisplayText));
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string ReturnType
    {
        get => _returnType;
        set
        {
            if (SetProperty(ref _returnType, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public AccessModifier AccessModifier
    {
        get => _accessModifier;
        set
        {
            if (SetProperty(ref _accessModifier, value))
            {
                OnPropertyChanged(nameof(AccessModifierSymbol));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public ObservableCollection<ParameterModel> Parameters
    {
        get => _parameters;
        set
        {
            if (SetProperty(ref _parameters, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string AccessModifierSymbol => _accessModifier.ToSymbol();

    public string DisplayText
    {
        get
        {
            var parametersText = string.Join(", ", Parameters.Select(p => $"{p.Name}: {p.DataType}"));
            return $"{AccessModifierSymbol} {Name}({parametersText}): {ReturnType}";
        }
    }

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
/// クラス図のクラス
/// </summary>
public class ClassModel : INotifyPropertyChanged
{
    private Guid _id;
    private string _name = "NewClass";
    private ClassType _type = ClassType.Class;
    private Point _position;
    private ObservableCollection<AttributeModel> _attributes;
    private ObservableCollection<MethodModel> _methods;

    public ClassModel()
    {
        _id = Guid.NewGuid();
        _position = new Point(0, 0);
        _attributes = new ObservableCollection<AttributeModel>();
        _methods = new ObservableCollection<MethodModel>();
    }

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public ClassType Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
            {
                OnPropertyChanged(nameof(TypeDisplayText));
            }
        }
    }

    public Point Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    public ObservableCollection<AttributeModel> Attributes
    {
        get => _attributes;
        set => SetProperty(ref _attributes, value);
    }

    public ObservableCollection<MethodModel> Methods
    {
        get => _methods;
        set => SetProperty(ref _methods, value);
    }

    public string TypeDisplayText => Type switch
    {
        ClassType.Interface => "«interface»",
        ClassType.AbstractClass => "«abstract»",
        _ => string.Empty
    };

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

    public ClassModel Clone()
    {
        var clone = new ClassModel
        {
            Name = Name + "_Copy",
            Type = Type,
            Position = new Point(Position.X + 50, Position.Y + 50)
        };

        foreach (var attr in Attributes)
        {
            clone.Attributes.Add(new AttributeModel
            {
                Name = attr.Name,
                DataType = attr.DataType,
                AccessModifier = attr.AccessModifier
            });
        }

        foreach (var method in Methods)
        {
            var clonedMethod = new MethodModel
            {
                Name = method.Name,
                ReturnType = method.ReturnType,
                AccessModifier = method.AccessModifier
            };
            foreach (var param in method.Parameters)
            {
                clonedMethod.Parameters.Add(new ParameterModel
                {
                    Name = param.Name,
                    DataType = param.DataType
                });
            }
            clone.Methods.Add(clonedMethod);
        }

        return clone;
    }
}

/// <summary>
/// クラス間の関係
/// </summary>
public class RelationModel : INotifyPropertyChanged
{
    private Guid _id;
    private Guid _sourceClassId;
    private Guid _targetClassId;
    private RelationType _type;
    private string _label = string.Empty;

    public RelationModel()
    {
        _id = Guid.NewGuid();
    }

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public Guid SourceClassId
    {
        get => _sourceClassId;
        set => SetProperty(ref _sourceClassId, value);
    }

    public Guid TargetClassId
    {
        get => _targetClassId;
        set => SetProperty(ref _targetClassId, value);
    }

    public RelationType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

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
/// クラス図全体
/// </summary>
public class DiagramModel : INotifyPropertyChanged
{
    private string _name = "Untitled Diagram";
    private DateTime _createdDate;
    private DateTime _modifiedDate;
    private ObservableCollection<ClassModel> _classes;
    private ObservableCollection<RelationModel> _relations;

    public DiagramModel()
    {
        _createdDate = DateTime.Now;
        _modifiedDate = DateTime.Now;
        _classes = new ObservableCollection<ClassModel>();
        _relations = new ObservableCollection<RelationModel>();
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public DateTime CreatedDate
    {
        get => _createdDate;
        set => SetProperty(ref _createdDate, value);
    }

    public DateTime ModifiedDate
    {
        get => _modifiedDate;
        set => SetProperty(ref _modifiedDate, value);
    }

    public ObservableCollection<ClassModel> Classes
    {
        get => _classes;
        set => SetProperty(ref _classes, value);
    }

    public ObservableCollection<RelationModel> Relations
    {
        get => _relations;
        set => SetProperty(ref _relations, value);
    }

    public void MarkAsModified()
    {
        ModifiedDate = DateTime.Now;
    }

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