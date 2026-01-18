using System;
using System.IO;
using System.Text;

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

[Serializable]
public struct ProjectData
{
    // Metadata
    public DateTime date;           // long ticks
    public string editorVersion;    // Size int, bytes (encoding utf-8)
    public string fileName;         // Size int, bytes (encoding utf-8)

    // Texture Data
    public uint width;              // Value between <0; 16384>
    public uint height;             // Value between <0; 16384>

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
    public static readonly int FORMAT_VERSION = 2;
    private static readonly int MAX_WIDTH = 16384;
    private static readonly int MAX_HEIGHT = 16384;

    public static bool TryReadData(string filePath, out ProjectData data, out int formatVersion, out ProjectFileError error)
    {
        data = default;
        error = ProjectFileError.None;
        formatVersion = 0;

        if (!File.Exists(filePath))
        {
            error = ProjectFileError.FileNotFound;
            return false;
        }

        try
        {
            using BinaryReader reader = new(File.OpenRead(filePath));

            // ---- Header ----
            byte[] magic = reader.ReadBytes(4);

            if (!magic.AsSpan().SequenceEqual(MAGIC))
            {
                error = ProjectFileError.InvalidMagic;
                return false;
            }

            formatVersion = reader.ReadInt32();
            switch (formatVersion)
            {
                case 2:
                    {
                        break;
                    }
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
            data.width = reader.ReadUInt32();
            data.height = reader.ReadUInt32();

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
                data.tiles[i] = ReadTileData(reader);
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

    private static TileData ReadTileData(BinaryReader reader)
    {
        TileData data = new()
        {
            x = reader.ReadInt32(),
            y = reader.ReadInt32(),
            r = reader.ReadByte(),
            g = reader.ReadByte(),
            b = reader.ReadByte(),
            a = reader.ReadByte()
        };

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
        writer.Write(tile.x);
        writer.Write(tile.y);

        writer.Write(tile.r);
        writer.Write(tile.g);
        writer.Write(tile.b);
        writer.Write(tile.a);
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
}