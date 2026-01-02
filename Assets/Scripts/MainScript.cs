using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;

// TODO:
// Ustawiæ minimalne pomniejszenie
// Kursor zmienia siê podczas przesuwania (w prawo-lewo i góra-dó³) (zrezygnowa³em)
// Sprawdziæ displacer i id

public enum Tool { Brush, Eraser, DisplacerEraser, DisplacerBrush };

public class MainScript : MonoBehaviour
{
    // Map Texture
    private Texture2D mapTex = null;
    [Header("Map Texture")]
    [SerializeField] private RawImage image;
    private bool isTextureLoaded = false;

    // Raw Image Dimensions
    RectTransform imageRect;
    float ImageRectWidth
    {
        get
        {
            return imageRect.rect.width;
        }
    }
    float ImageRectHeight
    {
        get
        {
            return imageRect.rect.height;
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

    // Pointer Texture
    private Texture2D pointerTex = null;
    [Header("Pointer Texture")]
    [SerializeField] private RawImage pointerImage;

    // Vertex Map Texture
    private Texture2D drawTex = null;
    [Header("Draw Texture")]
    [SerializeField] private RawImage drawImage;

    // Grid Texture
    private Texture2D gridTex = null;
    [Header("Grid Texture")]
    [SerializeField] private int gridPixelsPerPixelWidth = 25;
    [SerializeField] private int gridPixelsPerPixelHeight = 25;
    [SerializeField] private RawImage gridImage;

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
        imageRect = image.GetComponent<RectTransform>();
        isTextureLoaded = false;
        DefaultCursor();
    }

    private void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, Input.mousePosition, Camera.main, out mouseViewPos);

        mouseViewPos.x = ViewRectWidth - (ViewRectWidth / 2f - mouseViewPos.x);
        mouseViewPos.y = -((ViewRectHeight / 2f - mouseViewPos.y) - ViewRectHeight);

        if (isTextureLoaded && !isInteractionBlocked && mouseViewPos.x >= 0f && mouseViewPos.x <= ViewRectWidth && mouseViewPos.y >= 0f && mouseViewPos.y <= ViewRectHeight)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRect, Input.mousePosition, Camera.main, out mouseImagePos);

            // Calculate Pixel Position
            mouseImagePos.x = ImageRectWidth - (ImageRectWidth / 2f - mouseImagePos.x);
            mouseImagePos.y = -((ImageRectHeight / 2f - mouseImagePos.y) - ImageRectHeight);

            // Calculate Pivot
            pivotPos.x = mouseImagePos.x / ImageRectWidth;
            pivotPos.y = mouseImagePos.y / ImageRectHeight;

            PixelPosX = (int)Mathf.Clamp(mouseImagePos.x / pixelWidth, 0f, mapTex.width - 1);
            PixelPosY = (int)Mathf.Clamp(mouseImagePos.y / pixelHeight, 0f, mapTex.height - 1);

            if (Input.GetMouseButtonDown(0))
            {
                if (CurrentTool == Tool.Brush)
                {
                    if (drawTex.GetPixel(PixelPosX, PixelPosY).a == 0f)
                    {
                        DrawMove dm = new(ref drawTex, ref currentVertexGroup, currentPointerColor, _pixelPos);
                        if (currentVertexGroup.isFull)
                        {
                            moves.Add(new VertexGroupCreationMove(dm, ref drawTex, ref currentVertexGroup));
                        }
                        else
                        {
                            moves.Add(dm);
                        }
                    }
                }
                else if (CurrentTool == Tool.Eraser)
                {
                    if (drawTex.GetPixel(PixelPosX, PixelPosY).a != 0f)
                    {
                        moves.Add(new EraseMove(ref drawTex, _pixelPos));
                    }
                }
                else if (CurrentTool == Tool.DisplacerEraser)
                {
                    if (drawTex.GetPixel(PixelPosX, PixelPosY).a != 0f)
                    {
                        moves.Add(new DisplacerSelectMove(ref drawTex, ref mapTex, _pixelPos, out selectedID, ref currentVertexGroup));
                        CurrentTool = Tool.DisplacerBrush;
                    }
                }
                else if (CurrentTool == Tool.DisplacerBrush)
                {
                    if (drawTex.GetPixel(PixelPosX, PixelPosY).a == 0f)
                    {
                        moves.Add(new DisplacerDrawMove(ref drawTex, ref selectedID, selectedID, _pixelPos, ref currentVertexGroup));
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
            /*else
            {
                if (IsMovingHorizontal)
                {
                    IsMovingHorizontal = false;
                }

                if (IsMovingVertical)
                {
                    IsMovingVertical = false;
                }
            }*/

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
        }
        else
        {
            if (IsScaling)
            {
                IsScaling = false;
            }

            /*if (IsMovingHorizontal)
            {
                IsMovingHorizontal = false;
            }

            if (IsMovingVertical)
            {
                IsMovingVertical = false;
            }*/
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
    }

    // Default Options
    void ResetSettings()
    {
        lastPointerPos.x = 0f;
        lastPointerPos.y = 0f;

        currentVertexGroup.Clear();

        currentScale.x = 1.0f;
        currentScale.y = 1.0f * mapTex.height / mapTex.width;
        imageRect.localScale = currentScale;
        minScale = currentScale;

        pixelWidth = ImageRectWidth / mapTex.width;
        pixelHeight = ImageRectHeight / mapTex.height;

        currentMove = Vector2.zero;
        imageRect.transform.localPosition = currentMove;
        scrollMoveEnabled = false;

        pivotPos = Vector2.one * .5f;

        ChangeToBrush(false, false);
    }

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
            imageRect.localScale = currentScale;
        }
        else
        {
            imageRect.localScale = currentScale;

            Vector2 toMove = (pivotPos - imageRect.pivot) * scaleSensitivity * Input.mouseScrollDelta.y;
            toMove.x *= ImageRectWidth;
            toMove.y *= ImageRectHeight;
            currentMove -= toMove;
            imageRect.transform.localPosition = currentMove;
        }
        
        pixelWidth = ImageRectWidth / mapTex.width;
        pixelHeight = ImageRectHeight / mapTex.height;
    }

    // Move
    Vector2 currentMove = Vector2.zero;
    [Header("Movement")]
    public float moveSensitivity = 1.0f;
    /*bool isMovingHorizontal = false;
    bool IsMovingHorizontal
    {
        get
        {
            return isMovingHorizontal;
        }
        set
        {
            if (isMovingHorizontal != value)
            {
                isMovingHorizontal = value;
                if (isMovingHorizontal)
                {
                    MoveHorizontalCursor();
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
    bool isMovingVertical = false;
    bool IsMovingVertical
    {
        get
        {
            return isMovingVertical;
        }
        set
        {
            if (isMovingVertical != value)
            {
                isMovingVertical = value;
                if (isMovingVertical)
                {
                    MoveVerticalCursor();
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
    }*/
    void Move()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            /*IsMovingVertical = false;
            IsMovingHorizontal = true;*/
            currentMove.x += moveSensitivity * Input.mouseScrollDelta.y;
        }
        else
        {
            /*IsMovingHorizontal = false;
            IsMovingVertical = true;*/
            currentMove.y -= moveSensitivity * Input.mouseScrollDelta.y;
        }

        imageRect.transform.localPosition = currentMove;
    }

    // Scroll Button Move
    bool scrollMoveEnabled = false;
    bool scrollMoveEnabledChanged = false;
    Vector2 inizializedMousePosition = Vector2.zero;
    void ScrollButtonMove()
    {
        currentMove -= ((Vector2)Input.mousePosition - inizializedMousePosition) * 0.01f * moveSensitivity;
        imageRect.transform.localPosition = currentMove;
    }

    // Pointer Values
    Vector2 lastPointerPos = new();
    Color currentPointerColor = new();
    void UpdateDrawPointer()
    {
        pointerTex.SetPixel((int)lastPointerPos.x, (int)lastPointerPos.y, new Color(0,0,0,0));
        lastPointerPos.x = PixelPosX;
        lastPointerPos.y = PixelPosY;
        Color imagePixelColor = drawTex.GetPixel((int)lastPointerPos.x, (int)lastPointerPos.y);
        if (imagePixelColor.a == 0)
        {
            imagePixelColor = mapTex.GetPixel((int)lastPointerPos.x, (int)lastPointerPos.y);
        }
        currentPointerColor = (imagePixelColor.r + imagePixelColor.g + imagePixelColor.b) / 3f >= 0.5f ? Color.black : Color.white;
        pointerTex.SetPixel((int)lastPointerPos.x, (int)lastPointerPos.y, currentPointerColor);
        pointerTex.Apply();
    }

    // Grid
    void GenerateGrid(int mapWidth, int mapHeight)
    {
        gridTex = new(mapWidth * (gridPixelsPerPixelWidth + 1) + 1, mapHeight * (gridPixelsPerPixelHeight + 1) + 1);
        gridTex.filterMode = FilterMode.Point;
        for (int x = 0; x <= mapWidth * (gridPixelsPerPixelWidth + 1) + 1; x++)
        {
            for (int y = 0; y <= mapHeight * (gridPixelsPerPixelHeight + 1) + 1; y++)
            {
                if (x % (gridPixelsPerPixelWidth + 1) == 0 || y % (gridPixelsPerPixelHeight + 1) == 0)
                {
                    gridTex.SetPixel(x, y, Color.black);
                }
                else
                {
                    gridTex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }
        gridTex.Apply();
        gridImage.texture = gridTex;
        gridImage.color += new Color(0, 0, 0, 1);
    }

    // Image
    public void LoadImage()
    {
        //StandaloneFileBrowserWindows fileBrowser = new();
        string[] filePaths = StandaloneFileBrowser.OpenFilePanel("Load Map Image", Application.dataPath, new ExtensionFilter[] { new ExtensionFilter("map image", new string[] { "png" }) }, false);
        if (filePaths.Length != 0)
        {
            byte[] bytes = File.ReadAllBytes(filePaths[0]);
            mapTex = new(1, 1);
            mapTex.filterMode = FilterMode.Point;
            mapTex.LoadImage(bytes);
            image.texture = mapTex;
            image.color += new Color(0,0,0,1f);

            isTextureLoaded = true;

            drawTex = new(mapTex.width, mapTex.height);
            drawTex.filterMode = FilterMode.Point;
            pointerTex = new(mapTex.width, mapTex.height);
            pointerTex.filterMode = FilterMode.Point;
            Color def = new(0, 0, 0, 0);
            for (int x = 0; x < drawTex.width; x++)
            {
                for (int y = 0; y < drawTex.height; y++)
                {
                    drawTex.SetPixel(x, y, def);
                    pointerTex.SetPixel(x, y, def);
                }
            }
            drawTex.Apply();
            drawImage.texture = drawTex;
            drawImage.color += new Color(0, 0, 0, 1f);
            pointerTex.Apply();
            pointerImage.texture = pointerTex;
            pointerImage.color += new Color(0, 0, 0, 1f);

            GenerateGrid(mapTex.width, mapTex.height);

            ResetSettings();
        }
        else
        {
            Debug.Log("No files loaded");
        }
    }

    public void SaveImage()
    {
        if (isTextureLoaded)
        {
            //StandaloneFileBrowserWindows browser = new();
            string filePath = StandaloneFileBrowser.SaveFilePanel("Save Vertex Map Image", Application.dataPath, "VertexMap.png", new ExtensionFilter[] { new ExtensionFilter("Vertex Map Image", new string[] { "png" }) });
            if (filePath != null && filePath != "")
            {
                byte[] bytes = drawTex.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);
            }
        }
    }

    // Draft
    public void LoadDraft()
    {
        //StandaloneFileBrowserWindows fileBrowser = new();
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

            isTextureLoaded = true;

            drawTex = new(1, 1);
            drawTex.filterMode = FilterMode.Point;
            drawTex.LoadImage(data.drawTexture);
            drawImage.texture = drawTex;
            drawImage.color += new Color(0, 0, 0, 1f);

            pointerTex = new(mapTex.width, mapTex.height);
            pointerTex.filterMode = FilterMode.Point;
            Color def = new(0, 0, 0, 0);
            for (int x = 0; x < drawTex.width; x++)
            {
                for (int y = 0; y < drawTex.height; y++)
                {
                    pointerTex.SetPixel(x, y, def);
                }
            }
            pointerTex.Apply();
            pointerImage.texture = pointerTex;
            pointerImage.color += new Color(0, 0, 0, 1f);

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

                    //StandaloneFileBrowserWindows browser = new();
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

            //StandaloneFileBrowserWindows browser = new();
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

    // Tools
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
    void MoveVerticalCursor()
    {
        if (isCursorInView)
        {
            Cursor.SetCursor(moveVerticalyCursor, new Vector2(32,32), CursorMode.Auto);
        }
    }
    void MoveHorizontalCursor()
    {
        if (isCursorInView)
        {
            Cursor.SetCursor(moveHorizontalyCursor, new Vector2(32,32), CursorMode.Auto);
        }
    }

    void ScrollMoveCursor()
    {
        if (isCursorInView)
        {
            Cursor.SetCursor(scrollMoveCursor, new Vector2(32,32), CursorMode.Auto);
        }
    }
}
