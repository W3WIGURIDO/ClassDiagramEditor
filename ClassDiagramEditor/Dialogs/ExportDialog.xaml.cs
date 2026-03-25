using System.Windows;

namespace ClassDiagramEditor.Dialogs;

public enum ExportFormat
{
    Png,
    Svg,
    Clipboard
}

/// <summary>
/// エクスポートダイアログの選択結果
/// </summary>
public class ExportDialogResult
{
    public ExportFormat Format { get; init; }
    // バウンディングボックスに加える余白（ピクセル）
    public double Padding { get; init; }
    // PNG・クリップボード時は常にグリッド透過（SVGは常にfalse）
    public bool TransparentBackground => Format != ExportFormat.Svg;
}

/// <summary>
/// エクスポートダイアログ
/// </summary>
public partial class ExportDialog : Window
{
    public ExportDialogResult? Result { get; private set; }

    public ExportDialog()
    {
        InitializeComponent();

        // クリップボード選択時の注記表示切り替え
        RadioClipboard.Checked += (s, e) => ClipboardNote.Visibility = Visibility.Visible;
        RadioPng.Checked += (s, e) => ClipboardNote.Visibility = Visibility.Collapsed;
        RadioSvg.Checked += (s, e) => ClipboardNote.Visibility = Visibility.Collapsed;
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var format = RadioSvg.IsChecked == true ? ExportFormat.Svg
                   : RadioClipboard.IsChecked == true ? ExportFormat.Clipboard
                   : ExportFormat.Png;

        Result = new ExportDialogResult
        {
            Format = format,
            Padding = PaddingSlider.Value,
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}