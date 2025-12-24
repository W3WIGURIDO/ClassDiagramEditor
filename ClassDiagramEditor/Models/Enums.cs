namespace ClassDiagramEditor.Models;

/// <summary>
/// アクセス修飾子
/// </summary>
public enum AccessModifier
{
    Public,
    Private,
    Protected,
    Internal
}

/// <summary>
/// クラスの種類
/// </summary>
public enum ClassType
{
    Class,
    Interface,
    AbstractClass
}

/// <summary>
/// クラス間の関係
/// </summary>
public enum RelationType
{
    Inheritance,      // 継承
    Implementation,   // 実装
    Association,      // 関連
    Dependency       // 依存
}

/// <summary>
/// アクセス修飾子のヘルパー拡張メソッド
/// </summary>
public static class AccessModifierExtensions
{
    /// <summary>
    /// UML記法のシンボルを取得
    /// </summary>
    public static string ToSymbol(this AccessModifier modifier) => modifier switch
    {
        AccessModifier.Public => "+",
        AccessModifier.Private => "-",
        AccessModifier.Protected => "#",
        AccessModifier.Internal => "~",
        _ => ""
    };
}