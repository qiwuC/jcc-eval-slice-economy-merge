using System;

/// 棋子配置：固定费用与 1 星属性，Inspector 可改（反硬编码验证依赖此点）。
[Serializable]
public class PieceConfig
{
    public string pieceId;
    public int cost;
    public int baseHp;
    public int baseAtk;
}

/// 棋子实例（纯数据对象，不做 GameObject）。
[Serializable]
public class ChessPiece
{
    public string pieceId;
    public int star;
    public int baseHp;
    public int baseAtk;
}
