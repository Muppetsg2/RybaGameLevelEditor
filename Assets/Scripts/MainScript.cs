using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public enum Tool { Brush, Eraser, DisplacerEraser, DisplacerBrush };

public class MainScript : MonoBehaviour
{
    public bool isLevelCreated = false;
    [SerializeField] private Color defaultColor = new(0f, 0f, 0f, 0.988f);
    [SerializeField] private Color gridColor = Color.white;

    // Draw Texture
    [Header("Draw Texture")]
    private Texture2D drawTexture = null;
    [SerializeField] private RawImage drawImage; // image before

    // Raw Image Dimensions
    RectTransform drawImageRect;
    float DrawImageRectWidth
    {
        get
        {
            return drawImageRect.rect.width;
        }
    }
    float DrawImageRectHeight
    {
        get
        {
            return drawImageRect.rect.height;
        }
    }

    // View
    [Header("View")]
    [SerializeField] private RectTransform viewRect;
    float ViewRectWidth
    {
        get
        {
            return viewRect.rect.width;
        }
    }
    float ViewRectHeight
    {
        get
        {
            return viewRect.rect.height;
        }
    }

    // Mouse Position in Raw Image Dimensions
    Vector2 mouseImagePos = new();
    Vector2 mouseViewPos = new();

    // Pixel
    float pixelWidth = 0f;
    float pixelHeight = 0f;

    // Choosed Pixel
    Vector2 _pixelPos = new();
    int PixelPosX
    {
        get
        {
            return (int)_pixelPos.x;
        }
        set
        {
            if (_pixelPos.x != value)
            {
                _pixelPos.x = value;
                UpdateDrawPointer();
            }
        }
    }
    int PixelPosY
    {
        get
        {
            return (int)_pixelPos.y;
        }
        set
        {
            if (_pixelPos.y != value)
            {
                _pixelPos.y = value;
                UpdateDrawPointer();
            }
        }
    }

    // Pivot
    Vector2 pivotPos = new(.5f, .5f);

    // Pointer Indicator Texture
    private Texture2D pointerIndicatorTexture = null;

    [Header("Pointer Indicator Texture")]
    [SerializeField]
    private RawImage pointerIndicatorImage;

    // Grid Texture
    private Texture2D gridTexture = null;

    [Header("Grid Texture")]
    [SerializeField] 
    private int gridPixelsPerPixelWidth = 25;

    [SerializeField]
    private int gridPixelsPerPixelHeight = 25;

    [SerializeField]
    private RawImage gridImage;

    // Moves
    private List<IMove> moves = new();

    // Vertex Group
    private VertexGroup currentVertexGroup = new();

    // Displacer
    private int selectedID = -1;

    // Window Block
    private bool isInteractionBlocked = false;

    private void Start()
    {
        drawImageRect = drawImage.GetComponent<RectTransform>();
        //DefaultCursor();
    }

    private void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, Input.mousePosition, Camera.main, out mouseViewPos);

        mouseViewPos.x = ViewRectWidth - (ViewRectWidth * 0.5f - mouseViewPos.x);
        mouseViewPos.y = -((ViewRectHeight * 0.5f - mouseViewPos.y) - ViewRectHeight);

        if (isLevelCreated && !isInteractionBlocked && mouseViewPos.x >= 0f && mouseViewPos.x <= ViewRectWidth && mouseViewPos.y >= 0f && mouseViewPos.y <= ViewRectHeight)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(drawImageRect, Input.mousePosition, Camera.main, out mouseImagePos);

            // Calculate Pixel Position
            mouseImagePos.x = DrawImageRectWidth - (DrawImageRectWidth * 0.5f - mouseImagePos.x);
            mouseImagePos.y = -((DrawImageRectHeight * 0.5f - mouseImagePos.y) - DrawImageRectHeight);

            // Calculate Pivot
            pivotPos.x = mouseImagePos.x / DrawImageRectWidth;
            pivotPos.y = mouseImagePos.y / DrawImageRectHeight;

            PixelPosX = (int)Mathf.Clamp(mouseImagePos.x / pixelWidth, 0f, drawTexture.width - 1);
            PixelPosY = (int)Mathf.Clamp(mouseImagePos.y / pixelHeight, 0f, drawTexture.height - 1);

            /*
            if (Input.GetMouseButtonDown(0))
            {
                if (CurrentTool == Tool.Brush)
                {
                    if (drawTexture.GetPixel(PixelPosX, PixelPosY).a == 0f)
                    {
                        DrawMove dm = new(ref drawTexture, ref currentVertexGroup, currentPointerColor, _pixelPos);
                        if (currentVertexGroup.isFull)
                        {
                            moves.Add(new VertexGroupCreationMove(dm, ref drawTexture, ref currentVertexGroup));
                        }
                        else
                        {
                            moves.Add(dm);
                        }
                    }
                }
                else if (CurrentTool == Tool.Eraser)
                {
                    if (drawTexture.GetPixel(PixelPosX, PixelPosY).a != 0f)
                    {
                        moves.Add(new EraseMove(ref drawTexture, _pixelPos));
                    }
                }
                else if (CurrentTool == Tool.DisplacerEraser)
                {
                    if (drawTexture.GetPixel(PixelPosX, PixelPosY).a != 0f)
                    {
                        moves.Add(new DisplacerSelectMove(ref drawTexture, ref mapTex, _pixelPos, out selectedID, ref currentVertexGroup));
                        CurrentTool = Tool.DisplacerBrush;
                    }
                }
                else if (CurrentTool == Tool.DisplacerBrush)
                {
                    if (drawTexture.GetPixel(PixelPosX, PixelPosY).a == 0f)
                    {
                        moves.Add(new DisplacerDrawMove(ref drawTexture, ref selectedID, selectedID, _pixelPos, ref currentVertexGroup));
                        CurrentTool = Tool.DisplacerEraser;
                    }
                }
            }

            if (Input.mouseScrollDelta.y != 0f)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    IsScaling = true;
                    Scale();
                }
                else
                {
                    Move();
                }
            }

            if (Input.GetMouseButtonDown(2) && !scrollMoveEnabledChanged)
            {
                scrollMoveEnabled = !scrollMoveEnabled;
                scrollMoveEnabledChanged = true;
                if (scrollMoveEnabled)
                {
                    ScrollMoveCursor();
                    inizializedMousePosition = Input.mousePosition;
                }
                else
                {
                    if (isCursorInView)
                    {
                        ToolCursor();
                    }
                    else
                    {
                        DefaultCursor();
                    }
                }
            }
            */
        }
        /*
        else
        {
            if (IsScaling)
            {
                IsScaling = false;
            }
        }

        if (Input.GetMouseButtonDown(1) && moves.Count != 0)
        {
            IMove lastMove = moves[^1];
            moves.Remove(lastMove);
            lastMove.Undo();

            var moveType = lastMove.GetType();
            if (moveType.IsAssignableFrom(typeof(DrawMove)) || moveType.IsAssignableFrom(typeof(VertexGroupCreationMove)))
            {
                ChangeToBrush(false, false);
            }
            else if (moveType.IsAssignableFrom(typeof(EraseMove)) || moveType.IsAssignableFrom(typeof(DisplacerSelectMove)))
            {
                if (CurrentTool == Tool.DisplacerBrush)
                {
                    CurrentTool = Tool.DisplacerEraser;
                }
            }
            else if (moveType.IsAssignableFrom(typeof(DisplacerDrawMove)))
            {
                ChangeToDisplacer(false, false);
                CurrentTool = Tool.DisplacerBrush;
            }
        }

        if (scrollMoveEnabled && !scrollMoveEnabledChanged)
        {
            ScrollButtonMove();
            if (Input.GetMouseButtonDown(2))
            {
                scrollMoveEnabled = false;
                scrollMoveEnabledChanged = true;
                if (isCursorInView)
                {
                    ToolCursor();
                }
                else
                {
                    DefaultCursor();
                }
            }
        }
        scrollMoveEnabledChanged = false;

        if (IsScaling && Input.GetKeyUp(KeyCode.LeftControl))
        {
            IsScaling = false;
        }
        */
    }

    // Default Options
    void ResetSettings()
    {
        lastPointerPos.x = 0f;
        lastPointerPos.y = 0f;

        currentVertexGroup.Clear();

        /*
        currentScale.x = 1.0f;
        currentScale.y = 1.0f * mapTex.height / mapTex.width;
        drawImageRect.localScale = currentScale;
        minScale = currentScale;
        */

        pixelWidth = DrawImageRectWidth / drawTexture.width;
        pixelHeight = DrawImageRectHeight / drawTexture.height;

        /*
        currentMove = Vector2.zero;
        drawImageRect.transform.localPosition = currentMove;
        scrollMoveEnabled = false;
        */

        pivotPos = Vector2.one * .5f;

        //ChangeToBrush(false, false);
    }

    /*
    // Scale
    Vector2 minScale = Vector2.one;
    Vector2 currentScale = Vector2.one;
    [Header("Scale")]
    public float scaleSensitivity = 1.0f;
    bool isScaling = false;
    bool IsScaling
    {
        get
        {
            return isScaling;
        }
        set
        {
            if (isScaling != value)
            {
                isScaling = value;
                if (isScaling)
                {
                    ScaleCursor();
                }
                else
                {
                    if (isCursorInView)
                    {
                        ToolCursor();
                    }
                    else
                    {
                        DefaultCursor();
                    }
                }
            }
        }
    }
    void Scale()
    {
        currentScale += Vector2.one * scaleSensitivity * Input.mouseScrollDelta.y;
        if (currentScale.x < minScale.x || currentScale.y < minScale.y)
        {
            currentScale = minScale;
            drawImageRect.localScale = currentScale;
        }
        else
        {
            drawImageRect.localScale = currentScale;

            Vector2 toMove = (pivotPos - drawImageRect.pivot) * scaleSensitivity * Input.mouseScrollDelta.y;
            toMove.x *= DrawImageRectWidth;
            toMove.y *= DrawImageRectHeight;
            currentMove -= toMove;
            drawImageRect.transform.localPosition = currentMove;
        }
        
        pixelWidth = DrawImageRectWidth / mapTex.width;
        pixelHeight = DrawImageRectHeight / mapTex.height;
    }

    // Move
    Vector2 currentMove = Vector2.zero;
    [Header("Movement")]
    public float moveSensitivity = 1.0f;
    void Move()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentMove.x += moveSensitivity * Input.mouseScrollDelta.y;
        }
        else
        {
            currentMove.y -= moveSensitivity * Input.mouseScrollDelta.y;
        }

        drawImageRect.transform.localPosition = currentMove;
    }

    // Scroll Button Move
    bool scrollMoveEnabled = false;
    bool scrollMoveEnabledChanged = false;
    Vector2 inizializedMousePosition = Vector2.zero;
    void ScrollButtonMove()
    {
        currentMove -= ((Vector2)Input.mousePosition - inizializedMousePosition) * 0.01f * moveSensitivity;
        drawImageRect.transform.localPosition = currentMove;
    }
    */

    // Pointer Values
    Vector2 lastPointerPos = new();
    Color currentPointerColor = new();
    void UpdateDrawPointer()
    {
        pointerIndicatorTexture.SetPixel((int)lastPointerPos.x, (int)lastPointerPos.y, new Color(0,0,0,0));
        lastPointerPos.x = PixelPosX;
        lastPointerPos.y = PixelPosY;
        Color imagePixelColor = drawTexture.GetPixel((int)lastPointerPos.x, (int)lastPointerPos.y);
        // TODO: Change way that its checking color brightness
        currentPointerColor = (imagePixelColor.r + imagePixelColor.g + imagePixelColor.b) / 3f >= 0.5f ? Color.black : Color.white;
        pointerIndicatorTexture.SetPixel((int)lastPointerPos.x, (int)lastPointerPos.y, currentPointerColor);
        pointerIndicatorTexture.Apply();
    }

    // Grid
    void GenerateGrid(int mapWidth, int mapHeight)
    {
        gridTexture = new(mapWidth * (gridPixelsPerPixelWidth + 1) + 1, mapHeight * (gridPixelsPerPixelHeight + 1) + 1)
        {
            filterMode = FilterMode.Point
        };

        for (int x = 0; x <= mapWidth * (gridPixelsPerPixelWidth + 1) + 1; ++x)
        {
            for (int y = 0; y <= mapHeight * (gridPixelsPerPixelHeight + 1) + 1; ++y)
            {
                if (x % (gridPixelsPerPixelWidth + 1) == 0 || y % (gridPixelsPerPixelHeight + 1) == 0)
                {
                    gridTexture.SetPixel(x, y, gridColor);
                }
                else if (x == 0 || y == 0 || x == mapWidth * (gridPixelsPerPixelWidth + 1) + 1 || y == mapHeight * (gridPixelsPerPixelHeight + 1) + 1)
                {
                    gridTexture.SetPixel(x, y, gridColor);
                }
                else
                {
                    gridTexture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                }
            }
        }
        gridTexture.Apply();
        gridImage.texture = gridTexture;
        gridImage.color = Color.white;
    }

    // Image
    public void CreateLevelTexture(uint width, uint height)
    {
        pointerIndicatorTexture = new((int)width, (int)height)
        {
            filterMode = FilterMode.Point
        };

        drawTexture = new((int)width, (int)height)
        {
            filterMode = FilterMode.Point
        };

        Color def = defaultColor;
        for (int x = 0; x < drawTexture.width; ++x)
        {
            for (int y = 0; y < drawTexture.height; ++y)
            {
                drawTexture.SetPixel(x, y, def);
                pointerIndicatorTexture.SetPixel(x, y, def);
            }
        }

        drawTexture.Apply();
        drawImage.texture = drawTexture;
        drawImage.color = Color.white;

        pointerIndicatorTexture.Apply();
        pointerIndicatorImage.texture = pointerIndicatorTexture;
        pointerIndicatorImage.color = Color.white;

        isLevelCreated = true;

        GenerateGrid(drawTexture.width, drawTexture.height);

        ResetSettings();
    }

    /*
    public void SaveImage()
    {
        string filePath = StandaloneFileBrowser.SaveFilePanel("Save Level Map Image", Application.dataPath, "LevelMap.png", new ExtensionFilter[] { new ExtensionFilter("Level Map Image", new string[] { "png" }) });
        if (filePath != null && filePath != "")
        {
            byte[] bytes = drawTexture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
        }
    }
    */

    // Draft
    /*
    public void LoadDraft()
    {
        string[] filePaths = StandaloneFileBrowser.OpenFilePanel("Load Vertex Map Draft", Application.dataPath, new ExtensionFilter[] { new ExtensionFilter("Vertex Map Draft", new string[] { "vmd" }) }, false);
        if (filePaths.Length != 0)
        {
            DraftData data = FileWriter.ReadFromBinaryFile<DraftData>(filePaths[0]);

            print("Author: " + data.author);
            print("Date: " + data.date);
            print("Name: " + data.name);
            print("App Version: " + data.version);

            mapTex = new(1, 1);
            mapTex.filterMode = FilterMode.Point;
            mapTex.LoadImage(data.mapTexture);
            image.texture = mapTex;
            image.color += new Color(0, 0, 0, 1f);

            isLevelCreated = true;

            drawTex = new(1, 1);
            drawTex.filterMode = FilterMode.Point;
            drawTex.LoadImage(data.drawTexture);
            drawImage.texture = drawTex;
            drawImage.color += new Color(0, 0, 0, 1f);

            pointerIndicatorTexture = new(mapTex.width, mapTex.height);
            pointerIndicatorTexture.filterMode = FilterMode.Point;
            Color def = new(0, 0, 0, 0);
            for (int x = 0; x < drawTex.width; x++)
            {
                for (int y = 0; y < drawTex.height; y++)
                {
                    pointerIndicatorTexture.SetPixel(x, y, def);
                }
            }
            pointerIndicatorTexture.Apply();
            pointerIndicatorImage.texture = pointerIndicatorTexture;
            pointerIndicatorImage.color += new Color(0, 0, 0, 1f);

            GenerateGrid(mapTex.width, mapTex.height);

            ResetSettings();
        }
        else
        {
            Debug.Log("No files loaded");
        }
    }

    public void SaveDraft()
    {
        if (isTextureLoaded)
        {
            if (currentVertexGroup.Vertexes.Count != 0)
            {
                isInteractionBlocked = true;

                PopupSystem.CreateWindow("Vertex Group",
                "You have one unfinished vertex group.\n" +
                "If you want to save your draft, your changes will be lost.\n" +
                "Are you sure you want to do this?",
                "Yes", () =>
                {
                    isInteractionBlocked = false;

                    for (int i = 0; i < currentVertexGroup.Vertexes.Count; i++)
                    {
                        drawTex.SetPixel((int)currentVertexGroup.Vertexes[i].x, (int)currentVertexGroup.Vertexes[i].y, new Color(0, 0, 0, 0));
                    }
                    drawTex.Apply();
                    currentVertexGroup.Clear();

                    string filePath = StandaloneFileBrowser.SaveFilePanel("Save Vertex Map Draft", Application.dataPath, "VertexMapDraft.vmd", new ExtensionFilter[] { new ExtensionFilter("Vertex Map Draft", new string[] { "vmd" }) });
                    if (filePath != null && filePath != "")
                    {
                        DraftData data = new()
                        {
                            author = "Unknown",
                            name = "Vertex Map",
                            date = DateTime.Now,
                            version = Application.version,
                            mapTexture = mapTex.EncodeToPNG(),
                            drawTexture = drawTex.EncodeToPNG()
                        };
                        FileWriter.WriteToBinaryFile(filePath, data, false);
                    }
                },
                "No", () =>
                {
                    isInteractionBlocked = false;
                });

                return;
            }

            string filePath = StandaloneFileBrowser.SaveFilePanel("Save Vertex Map Draft", Application.dataPath, "VertexMapDraft.vmd", new ExtensionFilter[] { new ExtensionFilter("Vertex Map Draft", new string[] { "vmd" }) });
            if (filePath != null && filePath != "")
            {
                DraftData data = new()
                {
                    author = "Unknown",
                    name = "Vertex Map",
                    date = DateTime.Now,
                    version = Application.version,
                    mapTexture = mapTex.EncodeToPNG(),
                    drawTexture = drawTex.EncodeToPNG()
                };
                FileWriter.WriteToBinaryFile(filePath, data, false);
            }
        }
    }
    */

    // Tools
    /*
    [Header("Tools")]
    [SerializeField] private Button brushButton;
    [SerializeField] private Button eraserButton;
    [SerializeField] private Button displacerButton;
    private Tool _currentTool = Tool.Brush;
    private Tool CurrentTool
    {
        get
        {
            return _currentTool;
        }
        set
        {
            if (_currentTool != value)
            {
                _currentTool = value;
                ToolCursor();
            }
        }
    }

    void CheckDisplacerSelect(Action action, bool removeUnfinished)
    {
        if (moves.Count != 0 && moves[^1].GetType().IsAssignableFrom(typeof(DisplacerSelectMove)))
        {
            isInteractionBlocked = true;
            PopupSystem.CreateWindow("Displacer",
                "You are currently in displacer draw mode.\n" + 
                "If you change tool now your changes will be lost.\n" + 
                "Are you sure you want to do this?",
                "Yes", () =>
                {
                    isInteractionBlocked = false;

                    IMove move = moves[^1];
                    moves.RemoveAt(moves.Count - 1);
                    move.Undo();

                    if (removeUnfinished)
                    {
                        CheckUnfinishedGroup(action);
                        return;
                    }

                    FinalChangeTool(action);
                },
                "No", () => 
                {
                    isInteractionBlocked = false;
                });

            return;
        }

        if (removeUnfinished)
        {
            CheckUnfinishedGroup(action);
            return;
        }

        FinalChangeTool(action);
    }

    void CheckUnfinishedGroup(Action action)
    {
        if (currentVertexGroup.Vertexes.Count != 0)
        {
            isInteractionBlocked = true;

            PopupSystem.CreateWindow("Vertex Group",
                "You have one unfinished vertex group.\n" +
                "If you change tool now your changes will be lost.\n" + 
                "Are you sure you want to do this?",
                "Yes", () =>
                {
                    isInteractionBlocked = false;

                    for (int i = 0; i < currentVertexGroup.Vertexes.Count; i++)
                    {
                        drawTex.SetPixel((int)currentVertexGroup.Vertexes[i].x, (int)currentVertexGroup.Vertexes[i].y, new Color(0, 0, 0, 0));
                    }
                    drawTex.Apply();
                    currentVertexGroup.Clear();

                    FinalChangeTool(action);
                },
                "No", () => 
                {
                    isInteractionBlocked = false;
                });
            return;
        }

        FinalChangeTool(action);
    }

    void FinalChangeTool(Action action)
    {
        switch (CurrentTool)
        {
            case Tool.Brush:
                brushButton.interactable = true;
                break;
            case Tool.Eraser:
                eraserButton.interactable = true;
                break;
            case Tool.DisplacerEraser:
            case Tool.DisplacerBrush:
                displacerButton.interactable = true;
                break;
        }

        action.Invoke();
    }

    void ChangeTool(Action action, bool undoDisplacerSelect, bool removeUnfinished)
    {
        if (undoDisplacerSelect)
        {
            CheckDisplacerSelect(action, removeUnfinished);
            return;
        }

        if (removeUnfinished)
        {
            CheckUnfinishedGroup(action);
            return;
        }

        FinalChangeTool(action);
    }

    public void BrushButton()
    {
        if (!isTextureLoaded)
            return;

        ChangeToBrush(true, true);
    }

    void ChangeToBrush(bool removeUnfinished, bool undoDisplacerSelect)
    {
        ChangeTool(() =>
        {
            CurrentTool = Tool.Brush;
            brushButton.interactable = false;
        }, removeUnfinished, undoDisplacerSelect);
    }

    public void EraserButton()
    {
        if (!isTextureLoaded)
            return;

        ChangeToEraser(true, true);
    }

    void ChangeToEraser(bool removeUnfinished, bool undoDisplacerSelect)
    {
        ChangeTool(() =>
        {
            CurrentTool = Tool.Eraser;
            eraserButton.interactable = false;
        }, removeUnfinished, undoDisplacerSelect);
    }

    public void DisplacerButton()
    {
        if (!isTextureLoaded)
            return;

        ChangeToDisplacer(true, true);
    }

    void ChangeToDisplacer(bool removeUnfinished, bool undoDisplacerSelect)
    {
        ChangeTool(() =>
        {
            CurrentTool = Tool.DisplacerEraser;
            displacerButton.interactable = false;
        }, removeUnfinished, undoDisplacerSelect);
    }

    // Cursor
    [Header("Cursor")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D brushCursor; 
    [SerializeField] private Texture2D eraserCursor; 
    [SerializeField] private Texture2D displacerEraserCursor;
    [SerializeField] private Texture2D displacerBrushCursor; 
    [SerializeField] private Texture2D scaleCursor; 
    [SerializeField] private Texture2D moveVerticalyCursor; 
    [SerializeField] private Texture2D moveHorizontalyCursor;
    [SerializeField] private Texture2D scrollMoveCursor;
    private bool isCursorInView = false;
    public void EnableToolCursor()
    {
        isCursorInView = true;
        if (scrollMoveEnabled)
        {
            ScrollMoveCursor();
        }
        else
        {
            ToolCursor();
        }
    }
    public void DisableToolCursor()
    {
        isCursorInView = false;
        if (scrollMoveEnabled)
        {
            ScrollMoveCursor();
        }
        else
        {
            DefaultCursor();
        }
    }
    void DefaultCursor()
    {
        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
    }
    void ToolCursor()
    {
        if (isCursorInView)
        {
            switch (CurrentTool)
            {
                case Tool.Brush:
                    Cursor.SetCursor(brushCursor, new Vector2(0, 64), CursorMode.Auto);
                    break;
                case Tool.Eraser:
                    Cursor.SetCursor(eraserCursor, new Vector2(23, 64), CursorMode.Auto);
                    break;
                case Tool.DisplacerEraser:
                    Cursor.SetCursor(displacerEraserCursor, new Vector2(26, 38), CursorMode.Auto);
                    break;
                case Tool.DisplacerBrush:
                    Cursor.SetCursor(displacerBrushCursor, new Vector2(38, 26), CursorMode.Auto);
                    break;
            }
        }
    }
    void ScaleCursor()
    {
        if (isCursorInView)
        {
            Cursor.SetCursor(scaleCursor, new Vector2(32, 32), CursorMode.Auto);
        }
    }

    void ScrollMoveCursor()
    {
        if (isCursorInView)
        {
            Cursor.SetCursor(scrollMoveCursor, new Vector2(32,32), CursorMode.Auto);
        }
    }
    */
}
