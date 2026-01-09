using QOI;
using UnityEngine;

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
        QOIChannels channels = hasAlpha ? QOIChannels.Rgba : QOIChannels.Rgb;

        byte[] data = hasAlpha ? tex.GetByteArray32() : tex.GetByteArray24();

        return QOIEncoder.Encode(data, tex.width, tex.height, channels, QOIColorSpace.SRgb);
    }
}