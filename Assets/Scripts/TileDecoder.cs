using System;
using UnityEngine;

public class TileDecoder : MonoBehaviour
{
    public static TileType DecodeType(Color color)
    {
        foreach (TileType type in Enum.GetValues(typeof(TileType)))
        {
            if (CompareColorsWithoutAlpha(color, TileTypeColorMap.GetColor(type)))
                return type;
        }

        return TileType.Default;
    }

    private static bool CompareColorsWithoutAlpha(Color a, Color b)
    {
        return a.r == b.r && a.g == b.g && a.b == b.b;
    }

    public static void DecodeAlpha(float alpha, out int power, out int rotation)
    {
        int a = Mathf.RoundToInt(alpha * 255f);
        a &= 0xFF;

        rotation = a & 0b00000011;
        power = 63 - ((a >> 2) & 0b00111111);
    }
}