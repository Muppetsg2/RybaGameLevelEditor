using System;
using System.Runtime.InteropServices.WindowsRuntime;

[Serializable]
public struct TileData
{
    // Position
    public int x;
    public int y;

    // Color
    public byte r;
    public byte g;
    public byte b;
    public byte a;
}

public class ProjectFileFormatConst
{
    public static bool CompareWithMagicChars(char[] toCompare)
    {
        char[] magic = GetProjectFileFormatMagicChars();

        bool result = toCompare.Length == magic.Length;

        if (result)
        {
            for (int i = 0; i < magic.Length; ++i)
            {
                if (toCompare[i] != magic[i])
                {
                    result = false;
                    break;
                }
            }
        }

        return result;
    }

    public static char[] GetProjectFileFormatMagicChars()
    {
        return new char[4] { 'L', 'E', 'P', '\0' };
    }

    public static int GetProjectFileFormatVersion()
    {
        return 1;
    }
}

[Serializable]
public struct ProjectData
{
    // Metadata
    public char[] magic;    // LEP\0
    public string fileName; // File Name
    public DateTime date;
    public string editorVersion;
    public int formatVersion;

    // Texture Data
    public uint width;
    public uint height;

    // Tiles Data
    public TileData[] tiles;
}