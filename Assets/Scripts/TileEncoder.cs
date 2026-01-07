using UnityEngine;

public class TileEncoder
{
    public static Color EncodeColor(TileType type, int power, int rotation)
    {
        Color ret = TileTypeColorMap.GetColor(type);
        ret.a = EncodeAlpha(power, rotation);

        return ret;
    }

    public static float EncodeAlpha(int power, int rotation)
    {
        int a = ((63 - power) << 2) | rotation;
        a &= 0xFF;
        return a / 255f;
    }
}