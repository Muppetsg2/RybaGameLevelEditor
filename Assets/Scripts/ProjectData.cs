using System;

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
    public static int GetProjectFileFormatVersion()
    {
        return 1;
    }
}

[Serializable]
public struct ProjectData
{
    // Metadata
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