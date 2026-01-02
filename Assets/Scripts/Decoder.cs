using UnityEngine;
using SFB;
using NaughtyAttributes;
using System.IO;

public class Decoder : MonoBehaviour
{
    private Texture2D map;
    private GameObject handler;

    void DecodeLoop()
    {
        handler = new("Vertex Handler");
        handler.transform.position = Vector3.zero;
        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                Color pixel = map.GetPixel(x, y);
                if (pixel.a != 0f && DecodeID(pixel) == 0)
                {
                    DecodeVertexes(x, y);
                }
            }
        }
    }

    void DecodeVertexes(int x, int y)
    {
        int currX = x, currY = y;
        for (int i = 0; i < 4; i++)
        {
            GameObject vertex = new("Vertex (" + i + ", " + currX + ", " + currY + ")");
            vertex.transform.position = new Vector3(currX, currY);
            vertex.transform.parent = handler.transform;
            (currX, currY) = DecodePos(map.GetPixel(currX, currY));
        }
    }

    public static int DecodeID(Color color)
    {
        return ((int)(color.r * 255f) & 0xC0) >> 6;
    }

    public static (int, int) DecodePos(Color color)
    {
        //int id = 0;
        int x = 0;
        int y = 0;

        int r = (int)(color.r * 255f);
        int g = (int)(color.g * 255f);
        int b = (int)(color.b * 255f);

        //id = (r & 0xC0) >> 6;
        x = ((r & 0x3F) << 5) | ((g & 0xF8) >> 3);
        y = ((g & 0x07) << 8) | b;

        return (x, y);
    }

    [Button()]
    public void LoadVertexMap()
    {
        //StandaloneFileBrowserWindows browser = new StandaloneFileBrowserWindows();
        //string[] filePaths = browser.OpenFilePanel("Load Vertex Map", Application.dataPath, new ExtensionFilter[] { new ExtensionFilter("Vertex Map", new string[] { "png" }) }, false);
        string[] filePaths = StandaloneFileBrowser.OpenFilePanel("Load Vertex Map", Application.dataPath, new ExtensionFilter[] { new ExtensionFilter("Vertex Map", new string[] { "png" }) }, false);
        if (filePaths.Length != 0)
        {
            byte[] bytes = File.ReadAllBytes(filePaths[0]);
            map = new(1, 1);
            map.LoadImage(bytes);
            DecodeLoop();
        }
        else
        {
            Debug.Log("No files loaded");
        }
    }
}
