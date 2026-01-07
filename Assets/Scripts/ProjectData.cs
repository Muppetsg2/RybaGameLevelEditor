using System;

[Serializable]
public struct TileData
{
    // Position
    public int x;
    public int y;

    // Color
    public int r;
    public int g;
    public int b;
    public int a;
}

[Serializable]
public struct ProjectData
{
    // Metadata
    public string fileName; // File Name
    public DateTime date;
    public string editorVersion;
    public const string formatVersion = "1.0";

    // Texture Data
    public uint width;
    public uint height;

    // Tiles Data
    public TileData[] tiles;
}