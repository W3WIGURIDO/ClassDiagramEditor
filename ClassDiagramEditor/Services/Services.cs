using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClassDiagramEditor.Models;

namespace ClassDiagramEditor.Services;

/// <summary>
/// ファイルの保存・読み込みサービス
/// </summary>
public class FileService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public FileService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(),
                new PointJsonConverter()
            }
        };
    }

    public void SaveDiagram(DiagramModel diagram, string filePath)
    {
        try
        {
            var dto = ConvertToDto(diagram);
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save diagram: {ex.Message}", ex);
        }
    }

    public DiagramModel LoadDiagram(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<DiagramDto>(json, _jsonOptions);

            if (dto == null)
                throw new InvalidOperationException("Failed to deserialize diagram");

            return ConvertFromDto(dto);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load diagram: {ex.Message}", ex);
        }
    }

    private DiagramDto ConvertToDto(DiagramModel diagram)
    {
        var dto = new DiagramDto
        {
            Name = diagram.Name,
            CreatedDate = diagram.CreatedDate,
            ModifiedDate = diagram.ModifiedDate,
            Classes = [],
            Relations = []
        };

        foreach (var classModel in diagram.Classes)
        {
            var classDto = new ClassDto
            {
                Id = classModel.Id,
                Name = classModel.Name,
                Type = classModel.Type,
                Position = classModel.Position,
                Attributes = [],
                Methods = []
            };

            foreach (var attr in classModel.Attributes)
            {
                classDto.Attributes.Add(new AttributeDto
                {
                    Name = attr.Name,
                    DataType = attr.DataType,
                    AccessModifier = attr.AccessModifier
                });
            }

            foreach (var method in classModel.Methods)
            {
                var methodDto = new MethodDto
                {
                    Name = method.Name,
                    ReturnType = method.ReturnType,
                    AccessModifier = method.AccessModifier,
                    Parameters = []
                };

                foreach (var param in method.Parameters)
                {
                    methodDto.Parameters.Add(new ParameterDto
                    {
                        Name = param.Name,
                        DataType = param.DataType
                    });
                }

                classDto.Methods.Add(methodDto);
            }

            dto.Classes.Add(classDto);
        }

        foreach (var relation in diagram.Relations)
        {
            dto.Relations.Add(new RelationDto
            {
                Id = relation.Id,
                SourceClassId = relation.SourceClassId,
                TargetClassId = relation.TargetClassId,
                Type = relation.Type,
                Label = relation.Label
            });
        }

        return dto;
    }

    private DiagramModel ConvertFromDto(DiagramDto dto)
    {
        var diagram = new DiagramModel
        {
            Name = dto.Name,
            CreatedDate = dto.CreatedDate,
            ModifiedDate = dto.ModifiedDate
        };

        foreach (var classDto in dto.Classes)
        {
            var classModel = new ClassModel
            {
                Id = classDto.Id,
                Name = classDto.Name,
                Type = classDto.Type,
                Position = classDto.Position
            };

            foreach (var attrDto in classDto.Attributes)
            {
                classModel.Attributes.Add(new AttributeModel
                {
                    Name = attrDto.Name,
                    DataType = attrDto.DataType,
                    AccessModifier = attrDto.AccessModifier
                });
            }

            foreach (var methodDto in classDto.Methods)
            {
                var methodModel = new MethodModel
                {
                    Name = methodDto.Name,
                    ReturnType = methodDto.ReturnType,
                    AccessModifier = methodDto.AccessModifier
                };

                foreach (var paramDto in methodDto.Parameters)
                {
                    methodModel.Parameters.Add(new ParameterModel
                    {
                        Name = paramDto.Name,
                        DataType = paramDto.DataType
                    });
                }

                classModel.Methods.Add(methodModel);
            }

            diagram.Classes.Add(classModel);
        }

        foreach (var relationDto in dto.Relations)
        {
            diagram.Relations.Add(new RelationModel
            {
                Id = relationDto.Id,
                SourceClassId = relationDto.SourceClassId,
                TargetClassId = relationDto.TargetClassId,
                Type = relationDto.Type,
                Label = relationDto.Label
            });
        }

        return diagram;
    }
}

#region DTOs

internal class DiagramDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public List<ClassDto> Classes { get; set; } = [];
    public List<RelationDto> Relations { get; set; } = [];
}

internal class ClassDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClassType Type { get; set; }
    public Point Position { get; set; }
    public List<AttributeDto> Attributes { get; set; } = [];
    public List<MethodDto> Methods { get; set; } = [];
}

internal class AttributeDto
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public AccessModifier AccessModifier { get; set; }
}

internal class MethodDto
{
    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public AccessModifier AccessModifier { get; set; }
    public List<ParameterDto> Parameters { get; set; } = [];
}

internal class ParameterDto
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}

internal class RelationDto
{
    public Guid Id { get; set; }
    public Guid SourceClassId { get; set; }
    public Guid TargetClassId { get; set; }
    public RelationType Type { get; set; }
    public string Label { get; set; } = string.Empty;
}

#endregion

#region JSON Converters

public class PointJsonConverter : JsonConverter<Point>
{
    public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        double x = 0, y = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new Point(x, y);

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLower())
            {
                case "x":
                    x = reader.GetDouble();
                    break;
                case "y":
                    y = reader.GetDouble();
                    break;
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}

#endregion

/// <summary>
/// 画像エクスポートサービス
/// </summary>
public class ExportService
{
    // [2026-03-25 修正] 透過背景オプションを追加
    public void ExportToPng(UIElement element, string filePath, Rect bounds, double padding,
                            bool transparentBackground = false)
    {
        try
        {
            var (renderWidth, renderHeight, translateX, translateY) = CalcRenderParams(bounds, padding);
            var renderBitmap = RenderToBitmap(element, renderWidth, renderHeight,
                                              translateX, translateY, transparentBackground);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using var fileStream = new FileStream(filePath, FileMode.Create);
            encoder.Save(fileStream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export to PNG: {ex.Message}", ex);
        }
    }

    // [2026-03-26 修正] sizeMapを引数で受け取り独自のテキスト幅推定計算を廃止
    public void ExportToSvg(string filePath, Rect bounds, double padding,
                            IEnumerable<ClassModel> classes,
                            IEnumerable<RelationModel> relations,
                            Dictionary<Guid, (double Width, double Height)> classSizes)
    {
        try
        {
            var svg = GenerateSvg(bounds, padding, classes, relations, classSizes);
            File.WriteAllText(filePath, svg);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export to SVG: {ex.Message}", ex);
        }
    }

    // [2026-03-25 修正] 透過背景オプションを追加
    public void CopyToClipboard(UIElement element, Rect bounds, double padding,
                                bool transparentBackground = false)
    {
        try
        {
            var (renderWidth, renderHeight, translateX, translateY) = CalcRenderParams(bounds, padding);
            var renderBitmap = RenderToBitmap(element, renderWidth, renderHeight,
                                              translateX, translateY, transparentBackground);
            Clipboard.SetImage(renderBitmap);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to copy to clipboard: {ex.Message}", ex);
        }
    }


    // [2026-03-25 修正] 透過フラグに応じて白背景の描画を切り替え
    private static RenderTargetBitmap RenderToBitmap(
        UIElement element, int width, int height,
        double translateX, double translateY,
        bool transparentBackground = false)
    {
        var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

        var drawingVisual = new DrawingVisual();
        using (var ctx = drawingVisual.RenderOpen())
        {
            // [2026-03-25 修正] 透過指定がない場合のみ白背景を描画
            if (!transparentBackground)
            {
                ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            }

            var brush = new VisualBrush(element)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                Viewbox = new Rect(-translateX, -translateY, width, height),
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, width, height),
                ViewportUnits = BrushMappingMode.Absolute
            };
            ctx.DrawRectangle(brush, null, new Rect(0, 0, width, height));
        }

        renderBitmap.Render(drawingVisual);
        return renderBitmap;
    }

    // [2026-03-24 追加] バウンディングボックス + 余白からレンダリングパラメータを計算
    private static (int width, int height, double translateX, double translateY)
        CalcRenderParams(Rect bounds, double padding)
    {
        int width = (int)(bounds.Width + padding * 2);
        int height = (int)(bounds.Height + padding * 2);

        // 最低サイズ保証
        width = Math.Max(width, 100);
        height = Math.Max(height, 100);

        double translateX = -bounds.X + padding;
        double translateY = -bounds.Y + padding;

        return (width, height, translateX, translateY);
    }

    private static string GenerateSvg(
        Rect bounds, double padding,
        IEnumerable<ClassModel> classes,
        IEnumerable<RelationModel> relations,
        Dictionary<Guid, (double Width, double Height)> classSizes)
    {
        // [2026-03-26 修正] IEnumerableの多重列挙によるデータ消失を防ぐため
        // 先頭でリスト化して以降の全ループで再利用する
        var classesList = classes.ToList();
        var relationsList = relations.ToList();

        var sizeMap = classSizes;

        const double classHeaderH = 35;
        const double stereotypeExtraH = 14;
        const double classLineH = 20;
        const double classPad = 10;
        const double fontSize = 11;

        // [2026-03-26 修正] classesList使用・sizeMapのTryGetValue失敗時はスキップ
        double svgMinX = double.MaxValue, svgMinY = double.MaxValue;
        double svgMaxX = double.MinValue, svgMaxY = double.MinValue;
        foreach (var cls in classesList)
        {
            if (!sizeMap.TryGetValue(cls.Id, out var sz)) continue;
            svgMinX = Math.Min(svgMinX, cls.Position.X);
            svgMinY = Math.Min(svgMinY, cls.Position.Y);
            svgMaxX = Math.Max(svgMaxX, cls.Position.X + sz.Width);
            svgMaxY = Math.Max(svgMaxY, cls.Position.Y + sz.Height);
        }

        double ox, oy, svgW, svgH;
        if (svgMaxX > svgMinX)
        {
            ox = svgMinX - padding;
            oy = svgMinY - padding;
            svgW = (svgMaxX - svgMinX) + padding * 2;
            svgH = (svgMaxY - svgMinY) + padding * 2;
        }
        else
        {
            ox = bounds.X - padding;
            oy = bounds.Y - padding;
            svgW = bounds.Width + padding * 2;
            svgH = bounds.Height + padding * 2;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine($"""<svg width="{svgW:F0}" height="{svgH:F0}" xmlns="http://www.w3.org/2000/svg">""");
        sb.AppendLine("""  <rect width="100%" height="100%" fill="white"/>""");
        sb.AppendLine("""
  <defs>
    <marker id="arrowTriangleOpen" markerWidth="12" markerHeight="10" refX="10" refY="5" orient="auto">
      <polygon points="0,0 10,5 0,10" fill="white" stroke="black" stroke-width="1.5"/>
    </marker>
    <marker id="arrowOpen" markerWidth="10" markerHeight="10" refX="10" refY="5" orient="auto">
      <polyline points="0,0 10,5 0,10" fill="none" stroke="black" stroke-width="1.5"/>
    </marker>
    <!-- [2026-03-27 修正] refX=14に変更。marker-end使用時にダイヤの右尖り(x=14)をx2点に合わせることで
         ダイヤ全体がクラスボックスの外側(x1方向)に表示される。refX=0だとダイヤがボックス内に埋まる -->
    <marker id="diamondOpen" markerWidth="14" markerHeight="10" refX="14" refY="5" orient="auto">
      <polygon points="0,5 7,0 14,5 7,10" fill="white" stroke="black" stroke-width="1.5"/>
    </marker>
    <marker id="diamondFilled" markerWidth="14" markerHeight="10" refX="14" refY="5" orient="auto">
      <polygon points="0,5 7,0 14,5 7,10" fill="black" stroke="black" stroke-width="1.5"/>
    </marker>
  </defs>
""");

        // ── 関係線 ──────────────────────────────
        foreach (var rel in relationsList)
        {
            var src = classesList.FirstOrDefault(c => c.Id == rel.SourceClassId);
            var tgt = classesList.FirstOrDefault(c => c.Id == rel.TargetClassId);
            if (src == null || tgt == null) continue;
            if (!sizeMap.TryGetValue(src.Id, out var ss) || !sizeMap.TryGetValue(tgt.Id, out var ts)) continue;

            var sc = new Point(src.Position.X + ss.Width / 2, src.Position.Y + ss.Height / 2);
            var tc = new Point(tgt.Position.X + ts.Width / 2, tgt.Position.Y + ts.Height / 2);

            var sp = SvgConnectionPoint(src.Position, ss.Width, ss.Height, sc, tc);
            var tp = SvgConnectionPoint(tgt.Position, ts.Width, ts.Height, tc, sc);

            double x1 = sp.X - ox, y1 = sp.Y - oy;
            double x2 = tp.X - ox, y2 = tp.Y - oy;

            bool isDashed = rel.Type is RelationType.Implementation or RelationType.Dependency;
            string strokeDash = isDashed ? """ stroke-dasharray="6,4" """ : " ";
            string stroke = "black";

            switch (rel.Type)
            {
                case RelationType.Inheritance:
                case RelationType.Implementation:
                    sb.AppendLine($"""  <line x1="{x1:F1}" y1="{y1:F1}" x2="{x2:F1}" y2="{y2:F1}" stroke="{stroke}" stroke-width="2"{strokeDash}marker-end="url(#arrowTriangleOpen)"/>""");
                    break;
                case RelationType.Dependency:
                    sb.AppendLine($"""  <line x1="{x1:F1}" y1="{y1:F1}" x2="{x2:F1}" y2="{y2:F1}" stroke="{stroke}" stroke-width="1.5"{strokeDash}marker-end="url(#arrowOpen)"/>""");
                    break;
                case RelationType.Association:
                    sb.AppendLine($"""  <line x1="{x1:F1}" y1="{y1:F1}" x2="{x2:F1}" y2="{y2:F1}" stroke="{stroke}" stroke-width="1.5"/>""");
                    break;
                case RelationType.Aggregation:
                    // [2026-03-27 修正] WPFのDrawArrowHeadはダイヤをTarget側(end)に描画しているため
                    // SVGもmarker-end（x2側=Target側）にダイヤを置く。refX=0で左尖りがx2点に接する
                    sb.AppendLine($"""  <line x1="{x1:F1}" y1="{y1:F1}" x2="{x2:F1}" y2="{y2:F1}" stroke="{stroke}" stroke-width="1.5" marker-end="url(#diamondOpen)"/>""");
                    break;
                case RelationType.Composition:
                    // [2026-03-27 修正] 同上。合成は黒塗りダイヤをTarget側に配置
                    sb.AppendLine($"""  <line x1="{x1:F1}" y1="{y1:F1}" x2="{x2:F1}" y2="{y2:F1}" stroke="{stroke}" stroke-width="1.5" marker-end="url(#diamondFilled)"/>""");
                    break;
            }
        }

        // ── クラスボックス ───────────────────────
        foreach (var cls in classesList)
        {
            if (!sizeMap.TryGetValue(cls.Id, out var sz)) continue;

            double bx = cls.Position.X - ox;
            double by = cls.Position.Y - oy;
            double bw = sz.Width;
            double bh = sz.Height;

            string bgColor = cls.Type switch
            {
                ClassType.Interface => "#E6F0FF",
                ClassType.AbstractClass => "#FFF5E6",
                _ => "#FFFFFF"
            };

            sb.AppendLine($"""  <rect x="{bx:F1}" y="{by:F1}" width="{bw:F1}" height="{bh:F1}" fill="{bgColor}" stroke="black" stroke-width="2"/>""");

            bool clsHasStereotype = !string.IsNullOrEmpty(cls.TypeDisplayText);
            double clsHeaderH = classHeaderH + (clsHasStereotype ? stereotypeExtraH : 0);
            sb.AppendLine($"""  <rect x="{bx:F1}" y="{by:F1}" width="{bw:F1}" height="{clsHeaderH:F1}" fill="#C8C8C8"/>""");

            double textY = by + 3;
            if (!string.IsNullOrEmpty(cls.TypeDisplayText))
            {
                sb.AppendLine($"""  <text x="{bx + classPad:F1}" y="{textY + 10:F1}" font-family="Segoe UI" font-size="10" font-style="italic">{SvgEscape(cls.TypeDisplayText)}</text>""");
                textY += stereotypeExtraH;
            }

            string nameStyle = cls.Type == ClassType.AbstractClass ? " font-style=\"italic\"" : "";
            sb.AppendLine($"""  <text x="{bx + classPad:F1}" y="{textY + 18:F1}" font-family="Segoe UI" font-size="14" font-weight="bold"{nameStyle}>{SvgEscape(cls.Name)}</text>""");

            double curY = by + clsHeaderH;
            sb.AppendLine($"""  <line x1="{bx:F1}" y1="{curY:F1}" x2="{bx + bw:F1}" y2="{curY:F1}" stroke="black" stroke-width="1"/>""");

            if (cls.Attributes.Count > 0)
            {
                curY += classPad / 2;
                foreach (var attr in cls.Attributes)
                {
                    sb.AppendLine($"""  <text x="{bx + classPad:F1}" y="{curY + fontSize:F1}" font-family="Consolas,monospace" font-size="{fontSize:F0}">{SvgEscape(attr.DisplayText)}</text>""");
                    curY += classLineH;
                }
                curY += classPad / 2;
                sb.AppendLine($"""  <line x1="{bx:F1}" y1="{curY:F1}" x2="{bx + bw:F1}" y2="{curY:F1}" stroke="black" stroke-width="1"/>""");
            }

            if (cls.Methods.Count > 0)
            {
                curY += classPad / 2;
                foreach (var method in cls.Methods)
                {
                    sb.AppendLine($"""  <text x="{bx + classPad:F1}" y="{curY + fontSize:F1}" font-family="Consolas,monospace" font-size="{fontSize:F0}">{SvgEscape(method.DisplayText)}</text>""");
                    curY += classLineH;
                }
            }
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    // [2026-03-24 追加] SVG生成用の接続点計算（DiagramCanvasのGetConnectionPointと同ロジック）
    private static Point SvgConnectionPoint(
        Point boxPos, double w, double h, Point from, Point to)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double angle = Math.Atan2(dy, dx);
        double halfW = w / 2, halfH = h / 2;
        double threshold = Math.Atan2(halfH, halfW);
        double absAngle = Math.Abs(angle);

        if (absAngle < threshold)
            return new Point(boxPos.X + w, from.Y);
        if (absAngle > Math.PI - threshold)
            return new Point(boxPos.X, from.Y);
        if (angle > 0)
            return new Point(from.X, boxPos.Y + h);
        return new Point(from.X, boxPos.Y);
    }

    // [2026-03-24 追加] SVGテキスト内の特殊文字をエスケープ
    private static string SvgEscape(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}