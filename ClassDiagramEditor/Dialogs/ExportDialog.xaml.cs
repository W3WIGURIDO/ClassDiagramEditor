using System.Windows;

namespace ClassDiagramEditor.Dialogs;

// エクスポート形式を表す列挙型
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
    // [2026-03-25 追加] 背景・グリッドを透過するか（PNG・クリップボードのみ有効）
    public bool TransparentBackground { get; init; }
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

        // [2026-03-25 追加] SVG選択時は透過チェックボックスを無効化（SVGは透過不要）
        RadioSvg.Checked += (s, e) => TransparentBackgroundCheckBox.IsEnabled = false;
        RadioPng.Checked += (s, e) => TransparentBackgroundCheckBox.IsEnabled = true;
        RadioClipboard.Checked += (s, e) => TransparentBackgroundCheckBox.IsEnabled = true;
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
            // [2026-03-25 追加] チェックボックスの状態を取得
            TransparentBackground = TransparentBackgroundCheckBox.IsChecked == true
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