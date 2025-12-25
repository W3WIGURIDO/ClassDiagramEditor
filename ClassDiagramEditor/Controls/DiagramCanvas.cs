using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClassDiagramEditor.Models;
using ClassDiagramEditor.ViewModels;

namespace ClassDiagramEditor.Controls;

/// <summary>
/// クラス図を描画するカスタムキャンバス
/// </summary>
public class DiagramCanvas : Canvas
{
    private MainViewModel? _viewModel;
    private ClassModel? _draggingClass;
    private Point _dragStartPoint;
    private Point _dragStartPosition;
    private bool _isDragging;

    // 関係追加モード
    private bool _isAddingRelation;
    private ClassModel? _relationSourceClass;
    private RelationType _pendingRelationType;
    private Point _currentMousePosition;

    private readonly Dictionary<Guid, ClassBoxVisual> _classVisuals = [];

    public DiagramCanvas()
    {
        Background = Brushes.White;
        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as MainViewModel;
        if (_viewModel != null)
        {
            _viewModel.Diagram.Classes.CollectionChanged += Classes_CollectionChanged;
            _viewModel.Diagram.Relations.CollectionChanged += Relations_CollectionChanged;

            foreach (var classModel in _viewModel.Diagram.Classes)
            {
                AddClassVisual(classModel);
            }
        }
    }

    private void Classes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (ClassModel classModel in e.NewItems)
            {
                AddClassVisual(classModel);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (ClassModel classModel in e.OldItems)
            {
                RemoveClassVisual(classModel);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _classVisuals.Clear();
            Children.Clear();
        }

        InvalidateVisual();
    }

    private void Relations_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void AddClassVisual(ClassModel classModel)
    {
        var visual = new ClassBoxVisual();
        _classVisuals[classModel.Id] = visual;

        classModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(ClassModel.Position) or
                nameof(ClassModel.Name) or nameof(ClassModel.Type))
            {
                InvalidateVisual();
            }
        };

        classModel.Attributes.CollectionChanged += (s, e) => InvalidateVisual();
        classModel.Methods.CollectionChanged += (s, e) => InvalidateVisual();
    }

    private void RemoveClassVisual(ClassModel classModel)
    {
        _classVisuals.Remove(classModel.Id);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        if (_viewModel == null) return;

        DrawRelations(dc);
        DrawClasses(dc);

        if (_isAddingRelation && _relationSourceClass != null)
        {
            DrawTemporaryRelationLine(dc);
        }
    }

    private void DrawClasses(DrawingContext dc)
    {
        foreach (var kvp in _classVisuals)
        {
            var classModel = _viewModel!.Diagram.Classes.FirstOrDefault(c => c.Id == kvp.Key);
            if (classModel != null)
            {
                kvp.Value.Draw(dc, classModel);
            }
        }
    }

    private void DrawRelations(DrawingContext dc)
    {
        if (_viewModel == null) return;

        foreach (var relation in _viewModel.Diagram.Relations)
        {
            var sourceClass = _viewModel.Diagram.Classes.FirstOrDefault(c => c.Id == relation.SourceClassId);
            var targetClass = _viewModel.Diagram.Classes.FirstOrDefault(c => c.Id == relation.TargetClassId);

            if (sourceClass != null && targetClass != null)
            {
                DrawRelationLine(dc, relation, sourceClass, targetClass);
            }
        }
    }

    private void DrawRelationLine(DrawingContext dc, RelationModel relation, ClassModel source, ClassModel target)
    {
        var sourceVisual = _classVisuals[source.Id];
        var targetVisual = _classVisuals[target.Id];

        // クラスボックスの中心点
        var sourceCenter = new Point(
            source.Position.X + sourceVisual.Width / 2,
            source.Position.Y + sourceVisual.Height / 2
        );

        var targetCenter = new Point(
            target.Position.X + targetVisual.Width / 2,
            target.Position.Y + targetVisual.Height / 2
        );

        // クラスボックスの境界での接続点を計算
        var sourceConnectionPoint = GetConnectionPoint(source.Position, sourceVisual.Width, sourceVisual.Height, sourceCenter, targetCenter);
        var targetConnectionPoint = GetConnectionPoint(target.Position, targetVisual.Width, targetVisual.Height, targetCenter, sourceCenter);

        // 線のスタイルと色を決定
        Pen pen;
        Brush arrowBrush;

        switch (relation.Type)
        {
            case RelationType.Inheritance:
                // 継承: 実線 + 白抜き三角
                pen = new Pen(Brushes.Black, 2);
                arrowBrush = Brushes.White;
                break;

            case RelationType.Implementation:
                // 実装: 破線 + 白抜き三角
                pen = new Pen(Brushes.Black, 2) { DashStyle = DashStyles.Dash };
                arrowBrush = Brushes.White;
                break;

            case RelationType.Association:
                // 関連: 実線のみ（矢印なし、または必要に応じて開いた矢印）
                pen = new Pen(Brushes.Black, 1.5);
                arrowBrush = Brushes.Black;
                break;

            case RelationType.Dependency:
                // 依存: 破線 + 開いた矢印
                pen = new Pen(Brushes.Black, 1.5) { DashStyle = DashStyles.Dash };
                arrowBrush = Brushes.Black;
                break;

            default:
                pen = new Pen(Brushes.Black, 1.5);
                arrowBrush = Brushes.Black;
                break;
        }

        // 線を描画（接続点間を結ぶ）
        dc.DrawLine(pen, sourceConnectionPoint, targetConnectionPoint);

        // 矢印を描画
        if (relation.Type == RelationType.Association)
        {
            // 関連: 両端に矢印を描画
            DrawArrowHead(dc, relation.Type, targetConnectionPoint, sourceConnectionPoint, arrowBrush); // ソース側の矢印
            DrawArrowHead(dc, relation.Type, sourceConnectionPoint, targetConnectionPoint, arrowBrush); // ターゲット側の矢印
        }
        else
        {
            // 継承・実装・依存: ターゲット側のみ矢印
            DrawArrowHead(dc, relation.Type, sourceConnectionPoint, targetConnectionPoint, arrowBrush);
        }

        // ラベルを描画
        if (!string.IsNullOrEmpty(relation.Label))
        {
            var midPoint = new Point(
                (sourceConnectionPoint.X + targetConnectionPoint.X) / 2,
                (sourceConnectionPoint.Y + targetConnectionPoint.Y) / 2
            );

            var formattedText = new FormattedText(
                relation.Label,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                12,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            dc.DrawText(formattedText, new Point(midPoint.X - formattedText.Width / 2, midPoint.Y - 15));
        }
    }

    /// <summary>
    /// クラスボックスの境界上の接続点を計算
    /// </summary>
    private Point GetConnectionPoint(Point boxPosition, double boxWidth, double boxHeight, Point fromCenter, Point toCenter)
    {
        // ボックスの中心からターゲットへの方向ベクトル
        var dx = toCenter.X - fromCenter.X;
        var dy = toCenter.Y - fromCenter.Y;

        // 角度を計算
        var angle = Math.Atan2(dy, dx);

        // ボックスの半分のサイズ
        var halfWidth = boxWidth / 2;
        var halfHeight = boxHeight / 2;

        // 接続点を決定（上下左右の4辺のうち、どの辺に接続するか）
        Point connectionPoint;

        // 角度に基づいて接続する辺を決定
        var absAngle = Math.Abs(angle);
        var threshold = Math.Atan2(halfHeight, halfWidth);

        if (absAngle < threshold)
        {
            // 右辺に接続
            connectionPoint = new Point(
                boxPosition.X + boxWidth,
                fromCenter.Y
            );
        }
        else if (absAngle > Math.PI - threshold)
        {
            // 左辺に接続
            connectionPoint = new Point(
                boxPosition.X,
                fromCenter.Y
            );
        }
        else if (angle > 0)
        {
            // 下辺に接続
            connectionPoint = new Point(
                fromCenter.X,
                boxPosition.Y + boxHeight
            );
        }
        else
        {
            // 上辺に接続
            connectionPoint = new Point(
                fromCenter.X,
                boxPosition.Y
            );
        }

        return connectionPoint;
    }

    private void DrawArrowHead(DrawingContext dc, RelationType type, Point start, Point end, Brush arrowBrush)
    {
        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        const double arrowSize = 15;
        const double arrowAngle = Math.PI / 7; // 約25度

        if (type is RelationType.Inheritance or RelationType.Implementation)
        {
            // 継承・実装: 白抜き三角形（▷）
            var arrowPoint1 = end;
            var arrowPoint2 = new Point(
                end.X - arrowSize * Math.Cos(angle - arrowAngle),
                end.Y - arrowSize * Math.Sin(angle - arrowAngle)
            );
            var arrowPoint3 = new Point(
                end.X - arrowSize * Math.Cos(angle + arrowAngle),
                end.Y - arrowSize * Math.Sin(angle + arrowAngle)
            );

            // 三角形を作成
            var triangleGeometry = new StreamGeometry();
            using (var ctx = triangleGeometry.Open())
            {
                ctx.BeginFigure(arrowPoint1, true, true);
                ctx.LineTo(arrowPoint2, true, false);
                ctx.LineTo(arrowPoint3, true, false);
            }

            // 白抜き三角形を描画（内側が白、外側が黒線）
            dc.DrawGeometry(arrowBrush, new Pen(Brushes.Black, 2), triangleGeometry);
        }
        else if (type == RelationType.Dependency)
        {
            // 依存: 開いた矢印（→）
            var arrowPoint1 = new Point(
                end.X - arrowSize * Math.Cos(angle - arrowAngle),
                end.Y - arrowSize * Math.Sin(angle - arrowAngle)
            );
            var arrowPoint2 = new Point(
                end.X - arrowSize * Math.Cos(angle + arrowAngle),
                end.Y - arrowSize * Math.Sin(angle + arrowAngle)
            );

            var pen = new Pen(Brushes.Black, 1.5);
            dc.DrawLine(pen, end, arrowPoint1);
            dc.DrawLine(pen, end, arrowPoint2);
        }
        else if (type == RelationType.Association)
        {
            bool enableAssociationArrow = false;
            if ((enableAssociationArrow))
            {

                // 関連: 矢印なし（オプション: 開いた矢印を描画する場合）
                // UML標準では関連に矢印は不要だが、方向性を示す場合は描画
                var arrowPoint1 = new Point(
                    end.X - arrowSize * 0.7 * Math.Cos(angle - arrowAngle),
                    end.Y - arrowSize * 0.7 * Math.Sin(angle - arrowAngle)
                );
                var arrowPoint2 = new Point(
                    end.X - arrowSize * 0.7 * Math.Cos(angle + arrowAngle),
                    end.Y - arrowSize * 0.7 * Math.Sin(angle + arrowAngle)
                );

                var pen = new Pen(Brushes.Black, 1.5);
                dc.DrawLine(pen, end, arrowPoint1);
                dc.DrawLine(pen, end, arrowPoint2);

            }
        }
    }

    private void DrawTemporaryRelationLine(DrawingContext dc)
    {
        if (_relationSourceClass == null) return;

        var sourceVisual = _classVisuals[_relationSourceClass.Id];

        var sourceCenter = new Point(
            _relationSourceClass.Position.X + sourceVisual.Width / 2,
            _relationSourceClass.Position.Y + sourceVisual.Height / 2
        );

        // ソース側の接続点を計算
        var sourceConnectionPoint = GetConnectionPoint(
            _relationSourceClass.Position,
            sourceVisual.Width,
            sourceVisual.Height,
            sourceCenter,
            _currentMousePosition
        );

        // 一時的な線のスタイル
        var pen = new Pen(Brushes.Gray, 1.5) { DashStyle = DashStyles.Dot };
        dc.DrawLine(pen, sourceConnectionPoint, _currentMousePosition);

        // 一時的な矢印も表示
        DrawArrowHead(dc, _pendingRelationType, sourceConnectionPoint, _currentMousePosition, Brushes.LightGray);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null) return;

        var clickPoint = e.GetPosition(this);
        var clickedClass = GetClassAtPoint(clickPoint);

        if (_isAddingRelation)
        {
            if (clickedClass != null)
            {
                if (_relationSourceClass == null)
                {
                    _relationSourceClass = clickedClass;
                    _viewModel.StatusMessage = "関係先のクラスをクリックしてください";
                }
                else
                {
                    if (_relationSourceClass != clickedClass)
                    {
                        _viewModel.AddRelation(_relationSourceClass.Id, clickedClass.Id, _pendingRelationType);
                    }
                    _isAddingRelation = false;
                    _relationSourceClass = null;
                    _viewModel.StatusMessage = "関係を追加しました";
                    InvalidateVisual();
                }
            }
        }
        else
        {
            if (clickedClass != null)
            {
                _viewModel.SelectedClass = clickedClass;
                _draggingClass = clickedClass;
                _dragStartPoint = clickPoint;
                _dragStartPosition = clickedClass.Position;
                _isDragging = false;
                CaptureMouse();
            }
            else
            {
                _viewModel.SelectedClass = null;
            }
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        _currentMousePosition = e.GetPosition(this);

        if (_isAddingRelation)
        {
            InvalidateVisual();
        }
        else if (_draggingClass != null && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _dragStartPoint;

            if (!_isDragging && (Math.Abs(offset.X) > 5 || Math.Abs(offset.Y) > 5))
            {
                _isDragging = true;
            }

            if (_isDragging)
            {
                var newPosition = new Point(
                    Math.Max(0, _dragStartPosition.X + offset.X),
                    Math.Max(0, _dragStartPosition.Y + offset.Y)
                );

                _draggingClass.Position = newPosition;
            }
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingClass != null && _isDragging)
        {
            var newPosition = _draggingClass.Position;
            if (newPosition != _dragStartPosition && _viewModel != null)
            {
                _viewModel.MoveClass(_draggingClass, _dragStartPosition, newPosition);
            }
        }

        _draggingClass = null;
        _isDragging = false;
        ReleaseMouseCapture();
    }

    private ClassModel? GetClassAtPoint(Point point)
    {
        if (_viewModel == null) return null;

        for (int i = _viewModel.Diagram.Classes.Count - 1; i >= 0; i--)
        {
            var classModel = _viewModel.Diagram.Classes[i];
            if (_classVisuals.TryGetValue(classModel.Id, out var visual))
            {
                var rect = new Rect(classModel.Position, new Size(visual.Width, visual.Height));
                if (rect.Contains(point))
                {
                    return classModel;
                }
            }
        }

        return null;
    }

    public void StartAddingRelation(RelationType type)
    {
        _isAddingRelation = true;
        _relationSourceClass = null;
        _pendingRelationType = type;
        if (_viewModel != null)
        {
            _viewModel.StatusMessage = $"{type}関係を追加: 関係元のクラスをクリックしてください";
        }
    }

    public void CancelAddingRelation()
    {
        _isAddingRelation = false;
        _relationSourceClass = null;
        InvalidateVisual();
    }
}

/// <summary>
/// クラスボックスの描画
/// </summary>
internal class ClassBoxVisual
{
    private const double Padding = 10;
    private const double LineHeight = 20;
    private const double MinWidth = 150;
    private const double HeaderHeight = 35;

    public double Width { get; private set; } = MinWidth;
    public double Height { get; private set; } = HeaderHeight;

    private void CalculateSize(ClassModel model)
    {
        Width = MinWidth;
        Height = HeaderHeight;

        if (model.Attributes.Count > 0)
        {
            Height += Padding + model.Attributes.Count * LineHeight;
        }

        if (model.Methods.Count > 0)
        {
            Height += Padding + model.Methods.Count * LineHeight;
        }

        Height += Padding;
    }

    public void Draw(DrawingContext dc, ClassModel model)
    {
        CalculateSize(model);

        var position = model.Position;
        var rect = new Rect(position, new Size(Width, Height));

        var backgroundBrush = model.Type switch
        {
            ClassType.Interface => new SolidColorBrush(Color.FromRgb(230, 240, 255)),
            ClassType.AbstractClass => new SolidColorBrush(Color.FromRgb(255, 245, 230)),
            _ => Brushes.White
        };

        dc.DrawRectangle(backgroundBrush, new Pen(Brushes.Black, 2), rect);

        double currentY = position.Y;

        // ヘッダー
        var headerRect = new Rect(position.X, currentY, Width, HeaderHeight);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(200, 200, 200)), null, headerRect);

        if (!string.IsNullOrEmpty(model.TypeDisplayText))
        {
            DrawText(dc, model.TypeDisplayText, position.X + Padding, currentY + 3, 10, FontStyles.Italic);
            currentY += 12;
        }

        DrawText(dc, model.Name, position.X + Padding, currentY + 8, 14,
            model.Type == ClassType.AbstractClass ? FontStyles.Italic : FontStyles.Normal,
            FontWeights.Bold);

        currentY += HeaderHeight;

        dc.DrawLine(new Pen(Brushes.Black, 1),
            new Point(position.X, currentY),
            new Point(position.X + Width, currentY));

        // 属性
        if (model.Attributes.Count > 0)
        {
            currentY += Padding / 2;
            foreach (var attr in model.Attributes)
            {
                DrawText(dc, attr.DisplayText, position.X + Padding, currentY, 11);
                currentY += LineHeight;
            }
            currentY += Padding / 2;

            dc.DrawLine(new Pen(Brushes.Black, 1),
                new Point(position.X, currentY),
                new Point(position.X + Width, currentY));
        }

        // メソッド
        if (model.Methods.Count > 0)
        {
            currentY += Padding / 2;
            foreach (var method in model.Methods)
            {
                DrawText(dc, method.DisplayText, position.X + Padding, currentY, 11);
                currentY += LineHeight;
            }
        }
    }

    private static void DrawText(DrawingContext dc, string text, double x, double y,
        double fontSize, FontStyle fontStyle = default, FontWeight fontWeight = default)
    {
        fontWeight = fontWeight == default ? FontWeights.Normal : fontWeight;
        fontStyle = fontStyle == default ? FontStyles.Normal : fontStyle;

        var formattedText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), fontStyle, fontWeight, FontStretches.Normal),
            fontSize,
            Brushes.Black,
            1.0
        );

        dc.DrawText(formattedText, new Point(x, y));
    }
}