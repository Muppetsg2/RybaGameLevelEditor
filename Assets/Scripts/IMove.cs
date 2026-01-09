using UnityEngine;
using System.Collections.Generic;

public interface IMove
{
    void Do();
    void Undo();
}

public struct DrawData
{
    public Vector2Int Pos;
    public Color32 OldColor;
    public Color32 NewColor;
}

public class DrawMove : IMove
{
    private readonly Texture2D _tex = null;

    private readonly Vector2Int _pixelPos = new();

    private readonly Color _newColor = new();

    private readonly Color _undoColor = new();

    public DrawMove(ref Texture2D tex, Color newColor, Vector2Int pixelPos)
    {
        _tex = tex;
        _pixelPos = pixelPos;
        _newColor = newColor;

        _undoColor = _tex.GetPixel(_pixelPos.x, _pixelPos.y);

        Do();
    }

    public DrawMove(ref Texture2D tex, DrawData data)
    {
        _tex = tex;
        _pixelPos = data.Pos;
        _newColor = data.NewColor;
        _undoColor = data.OldColor;

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

public class MultipleDrawMove : IMove
{
    private readonly Texture2D _tex = null;

    private readonly List<Vector2Int> _pixelPoses = new();

    private readonly List<Color> _newColors = new();

    private readonly List<Color> _undoColors = new();

    public MultipleDrawMove(ref Texture2D tex, List<DrawData> data)
    {
        _tex = tex;

        foreach (DrawData item in data)
        {
            _pixelPoses.Add(item.Pos);
            _newColors.Add(item.NewColor);
            _undoColors.Add(item.OldColor);
        }

        Do();
    }

    public void Do()
    {
        for (int i = 0; i < _pixelPoses.Count; ++i)
        {
            _tex.SetPixel(_pixelPoses[i].x, _pixelPoses[i].y, _newColors[i]);
        }
        _tex.Apply();
    }

    public void Undo()
    {
        for (int i = 0; i < _pixelPoses.Count; ++i)
        {
            _tex.SetPixel(_pixelPoses[i].x, _pixelPoses[i].y, _undoColors[i]);
        }
        _tex.Apply();
    }
}

public struct EraseData
{
    public Vector2Int Pos;
    public Color32 RemovedColor;
    public Color32 BackgroundColor;
}

public class EraseMove : IMove
{
    private readonly Texture2D _tex;

    private readonly Vector2Int _removedPos = new();
    private readonly Color _removedColor = new();
    private readonly Color _bgColor = new();

    public EraseMove(ref Texture2D tex, Vector2Int pixelPos, Color bgColor)
    {
        _tex = tex;
        _removedPos = pixelPos;
        _removedColor = _tex.GetPixel(_removedPos.x, _removedPos.y);
        _bgColor = bgColor;

        Do();
    }

    public EraseMove(ref Texture2D tex, EraseData data)
    {
        _tex = tex;
        _removedPos = data.Pos;
        _removedColor = data.RemovedColor;
        _bgColor = data.BackgroundColor;

        Do();
    }

    public void Do()
    {
        _tex.SetPixel(_removedPos.x, _removedPos.y, _bgColor);
        _tex.Apply();
    }

    public void Undo()
    {
        _tex.SetPixel(_removedPos.x, _removedPos.y, _removedColor);
        _tex.Apply();
    }
}

public class MultipleEraseMove : IMove
{
    private readonly Texture2D _tex;

    private readonly List<Vector2Int> _removedPoses = new();
    private readonly List<Color> _removedColors = new();
    private readonly List<Color> _bgColors = new();

    public MultipleEraseMove(ref Texture2D tex, List<EraseData> data)
    {
        _tex = tex;

        foreach (EraseData item in data)
        {
            _removedPoses.Add(item.Pos);
            _removedColors.Add(item.RemovedColor);
            _bgColors.Add(item.BackgroundColor);
        }

        Do();
    }

    public void Do()
    {
        for (int i = 0; i < _removedPoses.Count; ++i)
        {
            _tex.SetPixel(_removedPoses[i].x, _removedPoses[i].y, _bgColors[i]);
        }
        _tex.Apply();
    }

    public void Undo()
    {
        for (int i = 0; i < _removedPoses.Count; ++i)
        {
            _tex.SetPixel(_removedPoses[i].x, _removedPoses[i].y, _removedColors[i]);
        }
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