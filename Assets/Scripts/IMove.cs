using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public interface IMove
{
    void Undo();
}

public class DrawMove : IMove
{
    private readonly Texture2D _tex = null;
    private readonly VertexGroup _group = null;

    private Vector2 _pixelPos = new();

    private Color _newColor = new();

    public DrawMove(ref Texture2D tex, ref VertexGroup group, Color newColor, Vector2 pixelPos)
    {
        _tex = tex;
        _group = group;
        _pixelPos = pixelPos;
        _newColor = newColor;

        _tex.SetPixel((int)_pixelPos.x, (int)_pixelPos.y, _newColor);
        _tex.Apply();
        _group.Add(_pixelPos);
    }

    public void Undo()
    {
        _tex.SetPixel((int)_pixelPos.x, (int)_pixelPos.y, new Color(0,0,0,0));
        _tex.Apply();
        _group.Remove(_pixelPos);
    }
}

public class VertexGroupCreationMove : IMove
{
    private readonly Texture2D _tex = null;
    private readonly VertexGroup _currGroup = null;
    private readonly DrawMove _lastVertexDraw = null;
    private readonly VertexGroup _group = null;

    private List<Vector2> Positions
    {
        get
        {
            if (_group != null)
            {
                return _group.Vertexes;
            }
            return null;
        }
    }

    private readonly List<Color> _lastColors = new();

    public VertexGroupCreationMove(DrawMove drawMove, ref Texture2D tex, ref VertexGroup group)
    {
        _lastVertexDraw = drawMove;
        _currGroup = group;
        _group = _currGroup.Copy();
        _currGroup.Clear();
        _tex = tex;

        List<Color> newColors = _group.CalculateVertexesColors();
        for (int i = 0; i < Positions.Count; i++)
        {
            _lastColors.Add(_tex.GetPixel((int)Positions[i].x, (int)Positions[i].y));
            _tex.SetPixel((int)Positions[i].x, (int)Positions[i].y, newColors[i]);
        }
        _tex.Apply();
    }

    public void Undo()
    {
        for (int i = 0; i < Positions.Count; i++)
        {
            _tex.SetPixel((int)Positions[i].x, (int)Positions[i].y, _lastColors[i]);
        }
        _tex.Apply();

        _currGroup.Clear();
        for (int i = 0; i < Positions.Count; i++)
        {
            _currGroup.Add(Positions[i]);
        }

        _lastVertexDraw.Undo();
    }
}

public class EraseMove : IMove
{
    private readonly Texture2D _tex;

    private readonly List<Vector2> _removedPositions = new();
    private readonly List<Color> _removedColors = new();

    public EraseMove(ref Texture2D tex, Vector2 pixelPos)
    {
        _tex = tex;
        Vector2 nextPos = pixelPos;
        Color pixel;
        Color bg = new(0, 0, 0, 0);
        for (int i = 0; i < 4; i++)
        {
            _removedPositions.Add(nextPos);
            pixel = _tex.GetPixel((int)nextPos.x, (int)nextPos.y);
            _removedColors.Add(pixel);
            _tex.SetPixel((int)nextPos.x, (int)nextPos.y, bg);
            (nextPos.x, nextPos.y) = Decoder.DecodePos(pixel);
        }
        _tex.Apply();
    }

    public void Undo()
    {
        for (int i = 0; i < 4; i++)
        {
            _tex.SetPixel((int)_removedPositions[i].x, (int)_removedPositions[i].y, _removedColors[i]);
        }
        _tex.Apply();
    }
}

public class DisplacerSelectMove : IMove
{
    private readonly Texture2D _tex;

    private VertexGroup _currGroup;

    private readonly List<Vector2> _lastPos = new();
    private readonly List<Color> _lastColors = new();

    public DisplacerSelectMove(ref Texture2D tex, ref Texture2D mapTexture, Vector2 pixelPos, out int id, ref VertexGroup vertexGroup)
    {
        _tex = tex;

        Vector2 nextPos = pixelPos;
        Color pixel = _tex.GetPixel((int)nextPos.x, (int)nextPos.y);
        _lastPos.Add(nextPos);
        _lastColors.Add(pixel);

        id = Decoder.DecodeID(pixel);
        
        _tex.SetPixel((int)nextPos.x, (int)nextPos.y, new Color(0, 0, 0, 0));
        
        Color newColor;
        int[] ids = { -1, -1, -1 };
        for (int i = 0; i < 3; i++)
        {
            (nextPos.x, nextPos.y) = Decoder.DecodePos(pixel);
            pixel = _tex.GetPixel((int)nextPos.x, (int)nextPos.y);
            ids[i] = Decoder.DecodeID(pixel);
            _lastPos.Add(nextPos);
            _lastColors.Add(pixel);

            newColor = mapTexture.GetPixel((int)nextPos.x, (int)nextPos.y);
            newColor = (newColor.r + newColor.g + newColor.b) / 3f >= .5f ? Color.black : Color.white;

            _tex.SetPixel((int)nextPos.x, (int)nextPos.y, newColor);
        }

        _tex.Apply();

        _currGroup = vertexGroup;
        _currGroup.Clear();
        List<Vector2> sorted = new(_lastPos);
        sorted.RemoveAt(0);
        sorted = sorted.OrderBy((v) => { return ids[sorted.IndexOf(v)]; }).ToList();
        for (int i = 0; i < 3; i++)
        {
            _currGroup.Add(sorted[i]);
        }
    }

    public void Undo()
    {
        for (int i = 0; i < 4; i++)
        {
            _tex.SetPixel((int)_lastPos[i].x, (int)_lastPos[i].y, _lastColors[i]);
        }
        _tex.Apply();

        _currGroup.Clear();
    }
}

public class DisplacerDrawMove : IMove
{
    private readonly Texture2D _tex;

    private readonly int _id;
    private int _currId;

    private readonly VertexGroup _group;
    private List<Vector2> Positions
    {
        get
        {
            if (_group != null)
            {
                return _group.Vertexes;
            }
            return null;
        }
    }

    private readonly VertexGroup _currGroup;

    private readonly List<Color> _lastColors = new();

    public DisplacerDrawMove(ref Texture2D tex, ref int currId, int id, Vector2 pos, ref VertexGroup group)
    {
        _currId = currId;
        _id = id;
        _currGroup = group;
        _currGroup.InsertAt(_id, pos);
        _group = _currGroup.Copy();
        _currGroup.Clear();
        _tex = tex;

        List<Color> newColors = _group.CalculateVertexesColors();
        for (int i = 0; i < Positions.Count; i++)
        {
            _lastColors.Add(_tex.GetPixel((int)Positions[i].x, (int)Positions[i].y));
            _tex.SetPixel((int)Positions[i].x, (int)Positions[i].y, newColors[i]);
        }
        _tex.Apply();
    }

    public void Undo()
    {
        for (int i = 0; i < Positions.Count; i++)
        {
            _tex.SetPixel((int)Positions[i].x, (int)Positions[i].y, _lastColors[i]);
        }
        _tex.Apply();

        _currGroup.Clear();
        for (int i = 0; i < Positions.Count; i++)
        {
            if (i != _id)
            {
                _currGroup.Add(Positions[i]);
            }
        }

        _currId = _id;
    }
}
