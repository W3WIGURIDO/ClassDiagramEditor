# Class Diagram Editor

**WPF + C# + .NET 10.0** で構築されたUMLクラス図作成ソフトウェア

![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![License](https://img.shields.io/badge/License-MIT-green)

---

## 🎯 特徴

- ✅ **.NET 10.0** 最新技術スタック
- ✅ **Self-contained** ランタイム不要、単一実行ファイル
- ✅ **高パフォーマンス** 最適化されたJIT/GC
- ✅ **MVVMアーキテクチャ** 保守性の高い設計
- ✅ **Undo/Redo** 完全なコマンドパターン実装
- ✅ **リアルタイム描画** スムーズなドラッグ＆ドロップ

---

## 📋 機能一覧

### ✅ 実装済み機能

#### クラス図作成
- クラス、インターフェース、抽象クラスの追加
- 属性（フィールド）の追加・編集・削除
- メソッドの追加・編集・削除（パラメータ編集を含む）
- アクセス修飾子（public/private/protected/internal）

#### 関係
- 継承（Inheritance）- 実線 + 白抜き三角
- 実装（Implementation）- 点線 + 白抜き三角
- 関連（Association）- 実線
- 依存（Dependency）- 点線矢印
- 集約（Aggregation）- 実線 + 白抜きダイヤ
- 合成（Composition）- 実線 + 黒塗りダイヤ
- 関係線のラベル編集（ダブルクリックでインライン編集、右クリックメニューから追加・編集・削除）
- 関係線のホバーハイライト（赤色強調表示）
- 関係線の右クリックメニューによる削除

#### 編集機能
- ドラッグ&ドロップによるクラス配置
- 複数選択（Ctrl+クリック、矩形ドラッグ）・一括移動
- 全選択（Ctrl+A）
- Undo/Redo（無制限）
- クラス・関係の削除
- プロパティのリアルタイム編集（クラス名、種別）
- 属性・メソッドのダブルクリック編集
- 属性・メソッドの右クリックメニュー（編集・削除）
- 名前を付けて保存（上書き保存と別名保存を区別）

#### 表示
- UML標準記法での描画（Consolasフォントで属性・メソッドを表示）
- ズームイン/アウト（10%～300%）
- グリッド表示（エクスポート時は非表示）
- クラス種別による色分け（クラス: 白、インターフェース: 水色、抽象クラス: 橙）
- 選択クラスのハイライト（青枠＋コーナーマーカー）
- 中ボタンドラッグによるキャンバスパン
- ウィンドウタイトルへの現在ファイル名表示

#### 入出力
- プロジェクト保存/読み込み（.cdf / JSON形式）
- PNG画像エクスポート（グリッド非表示・白背景）
- クリップボードへのPNGコピー
- SVG画像エクスポート（関係線・ラベル・クラスボックス対応）

### 🔲 今後実装予定

- マウスホイールズーム（Ctrl+スクロール）
- キーボードショートカット（Ctrl+N / Ctrl+O / Ctrl+S）
- クラスの複製機能（`ClassModel.Clone()` 実装済み、UI未接続）
- 関係線のクリック選択・プロパティパネル表示
- 自動レイアウト
- C#コードからの自動生成（Roslyn）
- テーマのカスタマイズ

---

## 🚀 セットアップ

### 必要要件

- **IDE**: Visual Studio 2022 以降
- **.NET SDK**: .NET 10.0 SDK
- **OS**: Windows 10 (1809以降) / Windows 11

### インストール手順

#### 1. リポジトリをクローン
```bash
git clone https://github.com/yourusername/ClassDiagramEditor.git
cd ClassDiagramEditor
```

#### 2. Visual Studioで開く
```bash
start ClassDiagramEditor.slnx
```

または、Visual Studio を起動して `ClassDiagramEditor.slnx` を開く

#### 3. ビルド＆実行

**方法1: Visual Studioから**
- `F5`キーを押す、または`デバッグ` → `デバッグの開始`

**方法2: コマンドラインから**
```bash
dotnet run
```

---

## 📦 配布用ビルド

### 手動ビルド
```bash
# Framework-dependent版（ランタイム必要）
dotnet publish -c Release -o publish/framework-dependent

# Self-contained版（ランタイム不要、推奨）
dotnet publish -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  -o publish/self-contained
```

---

## 🎨 使い方

### 基本操作

#### クラスの追加
1. 左パネルの「📦 クラス」「🔷 インターフェース」「📐 抽象クラス」をクリック
2. キャンバスに新しいクラスが追加されます
3. ドラッグして自由に配置

#### 属性・メソッドの追加
1. クラスをクリックして選択
2. 右パネルで「➕ 属性を追加」または「➕ メソッドを追加」
3. ダイアログで情報を入力（メソッドはパラメータも設定可）

#### 属性・メソッドの編集・削除
- **ダブルクリック**: 右パネルのリスト上でダブルクリックして編集ダイアログを開く
- **右クリックメニュー**: リスト上で右クリックして「✏️ 編集」または「🗑️ 削除」を選択

#### 関係の追加
1. 左パネルの「⬆️ 継承」「🔸 実装」「↔️ 関連」「⤴️ 依存」「◇ 集約」「◆ 合成」をクリック
2. 関係元のクラスをクリック
3. 関係先のクラスをクリック
4. 関係線が自動描画されます

#### 関係のラベル編集
- **ダブルクリック**: 関係線上でダブルクリックするとインライン編集ボックスが表示される
  - Enter で確定、Escape でキャンセル、フォーカスアウトで確定
- **右クリックメニュー**: 関係線上で右クリックして「🏷️ ラベルを追加/編集」または「🏷️ ラベルを削除」

#### 複数選択と一括移動
- **Ctrl+クリック**: 個別に選択を追加/解除
- **矩形ドラッグ**: 空白部分からドラッグして範囲内のクラスを一括選択
- **Ctrl+A**: 全クラスを選択
- 選択したクラスをまとめてドラッグ移動可能

#### キャンバスの移動
- **中ボタンドラッグ**: キャンバス全体をパン（スクロール）

#### ファイル操作

| 操作 | 方法 |
|------|------|
| 新規作成 | ツールバー → 「新規作成」 |
| 開く | ツールバー → 「開く」→ .cdfファイルを選択 |
| 上書き保存 | ツールバー → 「保存」（ファイル未設定時はダイアログ表示） |
| 名前を付けて保存 | ツールバー → 「名前を付けて保存」 |
| エクスポート | ツールバー → 「エクスポート」→ 形式・余白を選択 |

**エクスポート形式:**
- PNG 画像（グリッド非表示・白背景）
- SVG ベクター画像（関係線・ラベル・クラスボックス対応）
- クリップボードにコピー（PNG形式）

#### キーボードショートカット

| 操作 | ショートカット |
|------|--------------|
| Undo | `Ctrl+Z` |
| Redo | `Ctrl+Y` |
| 全選択 | `Ctrl+A` |
| 選択解除 | `Escape` |
| 削除 | `Delete` |

---

## 🏗️ アーキテクチャ

### プロジェクト構造
```
ClassDiagramEditor/
├── Models/                  # データモデル
│   ├── Enums.cs            # 列挙型（AccessModifier, ClassType, RelationType）
│   └── Models.cs           # モデルクラス（ClassModel, AttributeModel, MethodModel, RelationModel, DiagramModel）
├── Commands/                # Undo/Redoコマンド
│   └── Commands.cs         # AddClass/RemoveClass/MoveClass/AddRelation/RemoveRelation/EditRelationLabel/MoveMultipleClasses
├── ViewModels/              # ViewModel層
│   └── ViewModels.cs       # MainViewModel, RelayCommand, ViewModelBase
├── Controls/                # カスタムコントロール
│   └── DiagramCanvas.cs    # キャンバス描画・マウス操作・ClassBoxVisual
├── Dialogs/                 # ダイアログ
│   ├── AddAttributeDialog.xaml / .cs   # 属性追加・編集
│   ├── AddMethodDialog.xaml / .cs      # メソッド追加・編集（パラメータ含む）
│   └── ExportDialog.xaml / .cs         # エクスポート設定
├── Services/                # サービス層
│   └── Services.cs         # FileService（JSON保存読込）, ExportService（PNG/SVG/クリップボード）
├── MainWindow.xaml          # メインUI
├── MainWindow.xaml.cs
├── App.xaml
└── App.xaml.cs             # NullToVisibilityConverter, NotNullToVisibilityConverter
```

### デザインパターン

- **MVVM** (Model-View-ViewModel)
- **Command Pattern** (Undo/Redo)
- **Observer Pattern** (INotifyPropertyChanged)
- **Repository Pattern** (FileService)

### 技術スタック

| レイヤー | 技術 |
|---------|------|
| UI | WPF + XAML |
| 言語 | C# 13 |
| フレームワーク | .NET 10.0 |
| データバインディング | INotifyPropertyChanged |
| シリアライズ | System.Text.Json + JsonStringEnumConverter |
| 描画 | DrawingContext / DrawingVisual |

---

## 🐛 トラブルシューティング

### Self-contained版が起動しない

**症状:** ダブルクリックしても何も起こらない

**解決方法:**
1. ファイルを右クリック → プロパティ
2. 「ブロックの解除」にチェック → OK
3. 再度実行

### ファイルが開けない

**症状:** 保存した.cdfファイルが開けない

**解決方法:**
- ファイルが壊れていないか確認
- 別のJSONエディタで開けるか確認
- バックアップから復元

### 描画が遅い

**症状:** クラスが多いと動作が重い

**解決方法:**
- クラス数を200個以下に抑える
- ズームレベルを下げる
- 不要な関係を削除


## 👤 作者

**W3WIGURIDO**
- GitHub: [W3WIGURIDO](https://github.com/W3WIGURIDO)

---

## 📝 更新履歴

### v1.0.0 (2026-XX-XX)
- 🎉 初回リリース
- ✅ 基本的なクラス図作成機能（クラス・インターフェース・抽象クラス）
- ✅ 6種類の関係（継承・実装・関連・依存・集約・合成）
- ✅ 属性・メソッドの追加・編集・削除（パラメータ編集含む）
- ✅ 関係ラベルのインライン編集
- ✅ 複数選択・一括移動・矩形選択
- ✅ Undo/Redo（無制限）
- ✅ ファイル保存/読み込み（.cdf / JSON）
- ✅ PNG・SVG・クリップボードエクスポート
- ✅ 中ボタンパン
- ✅ .NET 10.0 Self-contained

---