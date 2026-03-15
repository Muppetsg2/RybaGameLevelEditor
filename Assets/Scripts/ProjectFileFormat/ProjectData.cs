using System;
using System.IO;
using System.Text;

[Serializable]
public struct ColorData : IEquatable<ColorData>
{
    // Color
    public byte r;
    public byte g;
    public byte b;
    public byte a;

    public readonly bool Equals(ColorData other)
    {
        return r == other.r && g == other.g && b == other.b && a == other.a;
    }

    public override readonly bool Equals(object obj)
    {
        return obj is ColorData other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public static bool operator ==(ColorData left, ColorData right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ColorData left, ColorData right)
    {
        return !left.Equals(right);
    }
}

[Serializable]
public struct TileData
{
    // Position
    public ushort x;
    public ushort y;

    // Color
    public ColorData color;
}

[Serializable]
public struct ProjectData
{
    // Metadata
    public DateTime date;           // long ticks
    public string editorVersion;    // Size int, bytes (encoding utf-8)
    public string fileName;         // Size int, bytes (encoding utf-8)

    // Texture Data
    public ushort width;              // Value between <0; 16384>
    public ushort height;             // Value between <0; 16384>

    // Tiles Data
    public TileData[] tiles;
}

public enum ProjectFileError
{
    None,

    FileNotFound,
    CannotOpenFile,

    InvalidMagic,
    UnsupportedFormatVersion,

    InvalidStringLength,
    InvalidMapSize,
    InvalidTileCount,

    TilesNull,
    InvalidTileArraySize,
    WriteFailed,

    CorruptedData,
    Unknown
}

public class ProjectFileFormatSerializer
{
    private static readonly byte[] MAGIC = { (byte)'L', (byte)'E', (byte)'P', 0 };
    public static readonly uint FORMAT_VERSION = 3;
    private static readonly uint MAX_WIDTH = 16384;
    private static readonly uint MAX_HEIGHT = 16384;
    private static readonly byte OP_DATA = 0;
    private static readonly byte OP_INDEX = 1;

    private static ColorData[] palette;

    public static bool TryReadData(string filePath, out ProjectData data, out uint formatVersion, out ProjectFileError error)
    {
        data = default;
        error = ProjectFileError.None;
        formatVersion = 0u;

        if (!File.Exists(filePath))
        {
            error = ProjectFileError.FileNotFound;
            return false;
        }

        try
        {
            using BinaryReader reader = new(File.OpenRead(filePath));

            palette = new ColorData[64];

            // ---- Header ----
            byte[] magic = reader.ReadBytes(4);

            if (!magic.AsSpan().SequenceEqual(MAGIC))
            {
                error = ProjectFileError.InvalidMagic;
                return false;
            }

            formatVersion = reader.ReadUInt32();
            switch (formatVersion)
            {
                case 3:
                case 2:
                    {
                        break;
                    }
                case 1:
                default:
                    {
                        error = ProjectFileError.UnsupportedFormatVersion;
                        return false;
                    }
            }

            // ---- Metadata ----
            data.fileName = ReadStringSafe(reader, ref error);
            if (error != ProjectFileError.None)
            {
                return false;
            }

            data.date = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);

            data.editorVersion = ReadStringSafe(reader, ref error);
            if (error != ProjectFileError.None)
            {
                return false;
            }

            // ---- Map Size ----
            data.width = formatVersion < 3u ? (ushort)reader.ReadUInt32() : reader.ReadUInt16();
            data.height = formatVersion < 3u ? (ushort)reader.ReadUInt32() : reader.ReadUInt16();

            if (data.width > MAX_WIDTH || data.height > MAX_HEIGHT)
            {
                error = ProjectFileError.InvalidMapSize;
                return false;
            }

            // ---- Tiles ----
            int tileCount = reader.ReadInt32();

            long maxTileCount = (long)MAX_WIDTH * MAX_HEIGHT;
            if (tileCount < 0 || tileCount > maxTileCount)
            {
                error = ProjectFileError.InvalidTileCount;
                return false;
            }

            data.tiles = new TileData[tileCount];
            for (int i = 0; i < tileCount; ++i)
            {
                data.tiles[i] = ReadTileData(reader, formatVersion);
            }

            return true;
        }
        catch (IOException)
        {
            error = ProjectFileError.CannotOpenFile;
            return false;
        }
        catch
        {
            error = ProjectFileError.CorruptedData;
            return false;
        }
    }

    public static bool TryWriteData(string filePath, ProjectData dataToWrite, out ProjectFileError error)
    {
        error = ProjectFileError.None;

        // ---- Validation ----
        if (dataToWrite.width > MAX_WIDTH || dataToWrite.height > MAX_HEIGHT)
        {
            error = ProjectFileError.InvalidMapSize;
            return false;
        }

        if (dataToWrite.tiles == null)
        {
            error = ProjectFileError.TilesNull;
            return false;
        }

        long maxTileCount = (long)MAX_WIDTH * MAX_HEIGHT;
        if (dataToWrite.tiles.Length < 0 || dataToWrite.tiles.Length > maxTileCount)
        {
            error = ProjectFileError.InvalidTileArraySize;
            return false;
        }

        if (dataToWrite.fileName == null || dataToWrite.editorVersion == null)
        {
            error = ProjectFileError.InvalidStringLength;
            return false;
        }

        try
        {
            using BinaryWriter writer = new(File.Open(filePath, FileMode.Create));

            palette = new ColorData[64];

            // ---- Header ----
            // Magic (4 bytes)
            writer.Write(MAGIC);

            // Format version
            writer.Write(FORMAT_VERSION);

            // ---- Metadata ----
            // File name
            WriteStringSafe(writer, dataToWrite.fileName, ref error);
            if (error != ProjectFileError.None)
            {
                return false;
            }

            // DateTime as long
            writer.Write(dataToWrite.date.ToUniversalTime().Ticks);

            // Editor version
            WriteStringSafe(writer, dataToWrite.editorVersion, ref error);
            if (error != ProjectFileError.None)
            {
                return false;
            }

            // ---- Map Size ----
            writer.Write(dataToWrite.width);
            writer.Write(dataToWrite.height);

            // ---- Tiles ----
            writer.Write(dataToWrite.tiles.Length);

            foreach (TileData tile in dataToWrite.tiles)
            {
                WriteTileData(writer, tile);
            }

            return true;
        }
        catch (IOException)
        {
            error = ProjectFileError.CannotOpenFile;
            return false;
        }
        catch
        {
            error = ProjectFileError.WriteFailed;
            return false;
        }
    }

    private static TileData ReadTileData(BinaryReader reader, uint formatVersion)
    {
        TileData data;
        if (formatVersion < 3u)
        {
            data = new()
            {
                x = (ushort)reader.ReadInt32(),
                y = (ushort)reader.ReadInt32(),
                color = new()
                {
                    r = reader.ReadByte(),
                    g = reader.ReadByte(),
                    b = reader.ReadByte(),
                    a = reader.ReadByte()
                }
            };
        }
        else
        {
            byte op_code = reader.ReadByte();

            if (op_code == OP_DATA)
            {
                data = new()
                {
                    x = reader.ReadUInt16(),
                    y = reader.ReadUInt16(),
                    color = new()
                    {
                        r = reader.ReadByte(),
                        g = reader.ReadByte(),
                        b = reader.ReadByte(),
                        a = reader.ReadByte()
                    }
                };

                int index_pos = HashColor(data.color);
                palette[index_pos] = data.color;
            }
            else if (op_code == OP_INDEX)
            {
                data = new()
                {
                    x = reader.ReadUInt16(),
                    y = reader.ReadUInt16(),
                    color = palette[reader.ReadByte()]
                };
            }
            else
            {
                data = new();
            }
        }

        return data;
    }

    private static string ReadStringSafe(BinaryReader reader, ref ProjectFileError error)
    {
        int length = reader.ReadInt32();

        if (length < 0 || length > 1024 * 1024)
        {
            error = ProjectFileError.InvalidStringLength;
            return null;
        }

        byte[] bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    private static void WriteTileData(BinaryWriter writer, TileData tile)
    {
        int index_pos = HashColor(tile.color);
        if (palette[index_pos] == tile.color)
        {
            writer.Write(OP_INDEX);

            writer.Write(tile.x);
            writer.Write(tile.y);

            writer.Write((byte)index_pos);
            return;
        }
        writer.Write(OP_DATA);

        writer.Write(tile.x);
        writer.Write(tile.y);

        writer.Write(tile.color.r);
        writer.Write(tile.color.g);
        writer.Write(tile.color.b);
        writer.Write(tile.color.a);

        palette[index_pos] = tile.color;
    }

    private static void WriteStringSafe(BinaryWriter writer, string value, ref ProjectFileError error)
    {
        if (value == null)
        {
            error = ProjectFileError.InvalidStringLength;
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);

        if (bytes.Length < 0 || bytes.Length > 1024 * 1024)
        {
            error = ProjectFileError.InvalidStringLength;
            return;
        }

        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static int HashColor(ColorData color)
    {
        return (color.r * 3 + color.g * 5 + color.b * 7 + color.a * 11) & 63;
    }
}