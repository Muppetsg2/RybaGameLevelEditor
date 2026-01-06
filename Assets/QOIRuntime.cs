using UnityEngine;
using Qoi.Csharp;
using Channels = Qoi.Csharp.Channels;
using ColorSpace = Qoi.Csharp.ColorSpace;

public static class QOIRuntime
{
    static bool HasAlpha(Color32[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a < 255)
                return true;
        }
        return false;
    }

    public static byte[] EncodeToQOI(this Texture2D tex)
    {
        Color32[] pixels = tex.GetPixels32();
        bool hasAlpha = HasAlpha(pixels);
        Channels channels = hasAlpha ? Channels.Rgba : Channels.Rgb;

        byte[] data = hasAlpha ? tex.GetByteArray32() : tex.GetByteArray24();

        return Encoder.Encode(data, tex.width, tex.height, channels, ColorSpace.SRgb);
    }
}