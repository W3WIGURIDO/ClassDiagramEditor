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
    public void ExportToPng(UIElement element, string filePath, double width, double height)
    {
        try
        {
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(0, 0, width, height));
            element.UpdateLayout();

            var renderBitmap = new RenderTargetBitmap(
                (int)width,
                (int)height,
                96,
                96,
                PixelFormats.Pbgra32
            );

            renderBitmap.Render(element);

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

    public void ExportToSvg(UIElement element, string filePath, double width, double height)
    {
        try
        {
            var svg = GenerateSvgFromElement(element, width, height);
            File.WriteAllText(filePath, svg);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export to SVG: {ex.Message}", ex);
        }
    }

    private string GenerateSvgFromElement(UIElement element, double width, double height)
    {
        var svg = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <svg width="{width}" height="{height}" xmlns="http://www.w3.org/2000/svg">
              <rect width="100%" height="100%" fill="white"/>
              <text x="10" y="30" font-family="Segoe UI" font-size="14">
                SVG Export - Basic Implementation
              </text>
              <text x="10" y="50" font-family="Segoe UI" font-size="12" fill="gray">
                For complete SVG export, consider using a dedicated library
              </text>
            </svg>
            """;
        return svg;
    }

    public void CopyToClipboard(UIElement element, double width, double height)
    {
        try
        {
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(0, 0, width, height));
            element.UpdateLayout();

            var renderBitmap = new RenderTargetBitmap(
                (int)width,
                (int)height,
                96,
                96,
                PixelFormats.Pbgra32
            );

            renderBitmap.Render(element);
            Clipboard.SetImage(renderBitmap);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to copy to clipboard: {ex.Message}", ex);
        }
    }
}