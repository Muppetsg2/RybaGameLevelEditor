using System.Collections.Generic;
using UnityEngine;

public class TileTypeColorMap
{
    struct TypeToColor
    {
        public TileType Type;
        public Color Color;

        public TypeToColor(TileType type, Color32 color)
        {
            this.Type = type;
            this.Color = color;
        }
    }

    private static readonly List<TypeToColor> typeToColors = new()
    {
        new(TileType.Default,       new Color32(  0,   0,   0, 252)),
        new(TileType.PathStraight,  new Color32(255, 255,   0, 252)),
        new(TileType.PathTurn,      new Color32(  0, 255, 255, 252)),
        new(TileType.Factory,       new Color32(  0, 255,   0, 252)),
        new(TileType.Boiler,        new Color32(  0,   0, 255, 252)),
        new(TileType.Splitter,      new Color32(255,   0,   0, 252)),
        new(TileType.Merger,        new Color32(255,   0, 255, 252)),
        new(TileType.Obstacle,      new Color32(255, 255, 255, 252))
    };

    public static Color32 GetColor(TileType type)
    {
        foreach(TypeToColor color in typeToColors)
        {
            if (color.Type == type) return color.Color;
        }

        return typeToColors[0].Color;
    }
}