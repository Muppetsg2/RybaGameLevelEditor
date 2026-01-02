using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexGroup
{
    private List<Vector2> vertexes = new();
    public List<Vector2> Vertexes
    {
        get
        {
            return vertexes;
        }
    }
    public bool isFull
    {
        get
        {
            return vertexes.Count == 4;
        }
    }

    public void Add(Vector2 vertex)
    {
        vertexes.Add(vertex);
    }

    public void Clear()
    {
        vertexes.Clear();
    }

    public void Remove(Vector2 vertex)
    {
        vertexes.Remove(vertex);
    }

    public void InsertAt(int index, Vector2 vertex)
    {
        vertexes.Insert(index, vertex);
    }

    public VertexGroup Copy()
    {
        VertexGroup copy = new();
        for (int i = 0; i < vertexes.Count; i++)
        {
            copy.Add(vertexes[i]);
        }
        return copy;
    }

    public List<Color> CalculateVertexesColors()
    {
        List<Color> colors = new();

        for (uint i = 0; i < vertexes.Count; i++)
        {
            uint x, y;
            if (i < vertexes.Count - 1)
            {
                x = (uint)vertexes[(int)i + 1].x;
                y = (uint)vertexes[(int)i + 1].y;
            }
            else
            {
                x = (uint)vertexes[0].x;
                y = (uint)vertexes[0].y;
            }

            uint r = (uint)(((i & 0x03) << 6) | ((x & 0x7E0) >> 5));
            uint g = (uint)(((x & 0x01F) << 3 ) | ((y & 0x700) >> 8));
            uint b = (uint)(y & 0x0FF);

            colors.Add(new Color(r / 255f, g / 255f, b / 255f, 1f));
        }

        return colors;
    }
}
