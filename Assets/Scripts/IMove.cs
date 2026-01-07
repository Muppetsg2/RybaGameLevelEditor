using UnityEngine;

public interface IMove
{
    void Do();
    void Undo();
}

public class DrawMove : IMove
{
    private readonly Texture2D _tex = null;

    private Vector2Int _pixelPos = new();

    private Color _newColor = new();

    private Color _undoColor = new();

    public DrawMove(ref Texture2D tex, Color newColor, Vector2Int pixelPos)
    {
        _tex = tex;
        _pixelPos = pixelPos;
        _newColor = newColor;

        _undoColor = _tex.GetPixel(_pixelPos.x, _pixelPos.y);

        Do();
    }

    public void Do()
    {
        _tex.SetPixel(_pixelPos.x, _pixelPos.y, _newColor);
        _tex.Apply();
    }

    public void Undo()
    {
        _tex.SetPixel(_pixelPos.x, _pixelPos.y, _undoColor);
        _tex.Apply();
    }
}

public class EraseMove : IMove
{
    private readonly Texture2D _tex;

    private readonly Vector2Int _removedPos = new();
    private readonly Color _removedColor = new();

    public EraseMove(ref Texture2D tex, Vector2Int pixelPos)
    {
        _tex = tex;
        _removedPos = pixelPos;
        _removedColor = _tex.GetPixel(_removedPos.x, _removedPos.y);

        Do();
    }

    public void Do()
    {
        _tex.SetPixel(_removedPos.x, _removedPos.y, new Color(0f, 0f, 0f, 0.988f));
        _tex.Apply();
    }

    public void Undo()
    {
        _tex.SetPixel(_removedPos.x, _removedPos.y, _removedColor);
        _tex.Apply();
    }
}

public struct DisplacerData
{
    public Vector2Int FromPos;
    public TileType MoveType;
    public int MovePower;
    public int MoveRotation;
};

public class DisplacerTakeMove : IMove
{
    private readonly Texture2D _tex;

    private readonly Vector2Int _pos = new();
    private readonly Color _lastColor = new();

    public DisplacerTakeMove(ref Texture2D tex, Vector2Int pixelPos, out DisplacerData data)
    {
        _tex = tex;
        _pos = pixelPos;
        _lastColor = _tex.GetPixel(_pos.x, _pos.y);

        data = new DisplacerData
        {
            FromPos = _pos,
            MoveType = TileDecoder.DecodeType(_lastColor)
        };
        TileDecoder.DecodeAlpha(_lastColor.a, out data.MovePower, out data.MoveRotation);

        Do();
    }

    public void Do()
    {
        _tex.SetPixel(_pos.x, _pos.y, TileTypeColorMap.GetColor(TileType.Default));
        _tex.Apply();
    }

    public void Undo()
    {
        _tex.SetPixel(_pos.x, _pos.y, _lastColor);
        _tex.Apply();
    }
}

public class DisplacerPutMove : IMove
{
    private readonly Texture2D _tex;

    private readonly Vector2Int _pos = new();
    private readonly Color _lastColor = new();
    private readonly DisplacerData _data;

    public DisplacerPutMove(ref Texture2D tex, DisplacerData moveData, Vector2Int pos)
    {
        _tex = tex;
        _data = moveData;
        _pos = pos;
        _lastColor = _tex.GetPixel(_pos.x, _pos.y);

        Do();
    }

    public void Do()
    {
        _tex.SetPixel(_data.FromPos.x, _data.FromPos.y, TileTypeColorMap.GetColor(TileType.Default));
        _tex.SetPixel(_pos.x, _pos.y, TileEncoder.EncodeColor(_data.MoveType, _data.MovePower, _data.MoveRotation));
        _tex.Apply();
    }

    public void Undo()
    {
        _tex.SetPixel(_pos.x, _pos.y, _lastColor);
        _tex.SetPixel(_data.FromPos.x, _data.FromPos.y, TileEncoder.EncodeColor(_data.MoveType, _data.MovePower, _data.MoveRotation));
        _tex.Apply();
    }
}

public class PipetteMove : IMove
{
    public PipetteMove(ref Texture2D tex, Vector2Int pos, out TileType type, out int power, out int rotation)
    {
        Color pixelColor = tex.GetPixel(pos.x, pos.y);
        type = TileDecoder.DecodeType(pixelColor);
        TileDecoder.DecodeAlpha(pixelColor.a, out power, out rotation);
    }

    public void Do() {}

    public void Undo() {}
}