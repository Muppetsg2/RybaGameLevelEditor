using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Tool { Brush, Eraser, DisplacerTake, DisplacerPut, Pipette };

public class MainScript : MonoBehaviour
{
    // General Flags
    private bool isLevelCreated = false;

    // Colors
    [Header("Colors")]
    [SerializeField] private Color pointerIndicatorTexColor = new(0f, 0f, 0f, 0f);
    [SerializeField] private Color gridColor = Color.white;
    [SerializeField] private Color brightPointerColor = Color.white;
    [SerializeField] private Color darkPointerColor = Color.black;

    // Draw Texture
    [Header("Draw Image")]
    [SerializeField] private RawImage drawImage;
    private Texture2D drawTexture = null;

    // Raw Draw Image Dimensions
    private RectTransform drawImageRect;
    private float DrawImageRectWidth
    {
        get
        {
            return drawImageRect.rect.width;
        }
    }
    private float DrawImageRectHeight
    {
        get
        {
            return drawImageRect.rect.height;
        }
    }

    // View
    [Header("View")]
    [SerializeField] private RectTransform viewRect;
    private float ViewRectWidth
    {
        get
        {
            return viewRect.rect.width;
        }
    }
    private float ViewRectHeight
    {
        get
        {
            return viewRect.rect.height;
        }
    }

    // Mouse Position in Raw Image Dimensions
    private Vector2 mouseImagePos = new();
    private Vector2 mouseViewPos = new();

    // Pixel
    private float pixelWidth = 0f;
    private float pixelHeight = 0f;

    // Choosed Pixel
    private Vector2Int _pixelPos = new();
    private int PixelPosX
    {
        get
        {
            return _pixelPos.x;
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
    private int PixelPosY
    {
        get
        {
            return _pixelPos.y;
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

    // Pointer Indicator Texture
    [Header("Pointer Indicator Texture")]
    [SerializeField] private RawImage pointerIndicatorImage;
    private Texture2D pointerIndicatorTexture = null;

    // Grid Texture
    [Header("Grid Texture")]
    [SerializeField] private int gridPixelsPerPixelWidth = 25;
    [SerializeField] private int gridPixelsPerPixelHeight = 25;
    [SerializeField] private RawImage gridImage;
    private Texture2D gridTexture = null;

    // Displacer
    private DisplacerData displacerMoveData = new();

    // Window Block
    private bool isInteractionBlocked = false;

    private void Start()
    {
        drawImageRect = drawImage.GetComponent<RectTransform>();
        DefaultCursor();
    }

    private void Update()
    {
        undoButton.interactable = !(actualMoveId == -1);
        redoButton.interactable = !(movesHistory.Count == 0 || actualMoveId + 1 == movesHistory.Count);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, Input.mousePosition, Camera.main, out mouseViewPos);

        mouseViewPos.x += ViewRectWidth * 0.5f;
        mouseViewPos.y += ViewRectHeight * 0.5f;

        if (isLevelCreated && !isInteractionBlocked && mouseViewPos.x >= 0f && mouseViewPos.x <= ViewRectWidth && mouseViewPos.y >= 0f && mouseViewPos.y <= ViewRectHeight)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(drawImageRect, Input.mousePosition, Camera.main, out mouseImagePos);

            // Calculate Pixel Position
            mouseImagePos.x += DrawImageRectWidth * 0.5f;
            mouseImagePos.y += DrawImageRectHeight * 0.5f;

            // Calculate position in 0–1 UV space
            scalePivotPos.x = mouseImagePos.x / DrawImageRectWidth;
            scalePivotPos.y = mouseImagePos.y / DrawImageRectHeight;

            PixelPosX = (int)Mathf.Clamp(mouseImagePos.x / pixelWidth, 0f, drawTexture.width - 1);
            PixelPosY = (int)Mathf.Clamp(mouseImagePos.y / pixelHeight, 0f, drawTexture.height - 1);

            GetPixelInfo(PixelPosX, PixelPosY);

            IsDefaultTile = drawTexture.GetPixel(PixelPosX, PixelPosY) == TileTypeColorMap.GetColor(TileType.Default);

            if (Input.GetMouseButtonDown(0))
            {
                bool colorChanged = false;
                IMove moveToAdd = null;
                if (CurrentTool == Tool.Brush)
                {
                    moveToAdd = new DrawMove(ref drawTexture, TileEncoder.EncodeColor(tileType, tilePower, tileRotation), _pixelPos);
                    colorChanged = true;
                }
                else if (CurrentTool == Tool.Eraser)
                {
                    if (drawTexture.GetPixel(PixelPosX, PixelPosY) != TileTypeColorMap.GetColor(TileType.Default))
                    {
                        moveToAdd = new EraseMove(ref drawTexture, _pixelPos);
                        colorChanged = true;
                    }
                }
                else if (CurrentTool == Tool.DisplacerTake)
                {
                    if (drawTexture.GetPixel(PixelPosX, PixelPosY) != TileTypeColorMap.GetColor(TileType.Default))
                    {
                        moveToAdd = new DisplacerTakeMove(ref drawTexture, _pixelPos, out displacerMoveData);
                        CurrentTool = Tool.DisplacerPut;
                        colorChanged = true;
                    }
                }
                else if (CurrentTool == Tool.DisplacerPut)
                {
                    --actualMoveId;
                    moveToAdd = new DisplacerPutMove(ref drawTexture, displacerMoveData, _pixelPos);
                    CurrentTool = Tool.DisplacerTake;
                    colorChanged = true;
                }
                else if (CurrentTool == Tool.Pipette)
                {
                    _ = new PipetteMove(ref drawTexture, _pixelPos, out TileType pipetteType, out int pipettePower, out int pipetteRotation);
                    if (pipetteType != TileType.Default)
                    {
                        brushTypeDropdown.SetOption((uint)(int)(pipetteType - 1));
                        powerInput.SetValue(pipettePower);
                        rotationDropdown.SetOption((uint)pipetteRotation);
                    }
                }

                if (moveToAdd != null)
                {
                    if (actualMoveId + 1 != movesHistory.Count)
                    {
                        int startId = actualMoveId + 1;
                        movesHistory.RemoveRange(startId, movesHistory.Count - startId);
                    }

                    movesHistory.Add(moveToAdd);
                    ++actualMoveId;
                }

                if (colorChanged)
                {
                    UpdateDrawPointer();
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

            if (Input.GetMouseButtonDown(2))
            {
                scrollMoveEnabled = true;
                startMousePos = Input.mousePosition;
                startImagePos = drawImageRect.transform.localPosition;
            }

            if (scrollMoveEnabled)
            {
                ScrollMoveCursor();
                ScrollButtonMove();
            }
        }
        else
        {
            if (IsScaling)
            {
                IsScaling = false;
            }

            if (IsDefaultTile)
            {
                IsDefaultTile = false;
            }
        }

        if (Input.GetMouseButtonUp(2))
        {
            scrollMoveEnabled = false;

            if (isCursorInView)
            {
                ToolCursor();
            }
            else
            {
                DefaultCursor();
            }
        }

        /*
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
        */

        if (IsScaling && Input.GetKeyUp(KeyCode.LeftControl))
        {
            IsScaling = false;
        }
    }

    // Default Options
    void ResetSettings()
    {
        lastPointerPos.x = 0;
        lastPointerPos.y = 0;

        drawImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, drawTexture.width);
        drawImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, drawTexture.height);

        minScale = GetMinScale();
        maxScale = GetMaxScale();
        currentScale = GetDefaultScale();

        drawImageRect.localScale = new Vector3(currentScale, currentScale, 1.0f);

        pixelWidth = DrawImageRectWidth / drawTexture.width;
        pixelHeight = DrawImageRectHeight / drawTexture.height;

        drawImageRect.transform.localPosition = Vector2.zero;
        scrollMoveEnabled = false;

        scalePivotPos = Vector2.one;

        ChangeToBrush(false);
    }

    // Scale
    [Header("Scale")]
    public float scrollScaleSensitivityPercent = 0.1f;
    private float currentScale = 1.0f;
    private float minScale = 0.01f;
    private float maxScale = 200.0f;

    // Mouse Image UV Pos == scalePivotPos
    private Vector2 scalePivotPos = Vector2.one;

    private bool isScaling = false;
    private bool IsScaling
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

    private bool lastUp = true;
    bool LastUp
    {
        get
        {
            return lastUp;
        }
        set
        {
            if (lastUp != value)
            {
                lastUp = value;
                ScaleCursor();
            }
        }
    }

    float GetDefaultScale()
    {
        float scaleX = ViewRectWidth / DrawImageRectWidth;
        float scaleY = ViewRectHeight / DrawImageRectHeight;
        float computedScale = Mathf.Min(scaleX, scaleY) * 0.8f;
        return Mathf.Clamp(computedScale, minScale, maxScale);
    }

    float GetMinScale()
    {
        float scaleX = 2f / DrawImageRectWidth;
        float scaleY = 2f / DrawImageRectHeight;
        float computedScale = Mathf.Min(scaleX, scaleY) * 0.8f;
        return computedScale;
    }

    float GetMaxScale()
    {
        float scaleX = ViewRectWidth * 0.5f; // ViewRectWidth / 2f
        float scaleY = ViewRectHeight * 0.5f; // ViewRectHeight / 2f
        float computedScale = Mathf.Min(scaleX, scaleY) * 0.8f;
        return computedScale;
    }

    void Scale()
    {
        float oldScale = currentScale;
        currentScale += scrollScaleSensitivityPercent * currentScale * Input.mouseScrollDelta.y;
        currentScale = Mathf.Clamp(currentScale, minScale, maxScale);

        float scaleDiff = currentScale - oldScale;
        if (!LastUp && scaleDiff > 0.0f)
        {
            LastUp = true;
        }
        else if (LastUp && scaleDiff < 0.0f)
        {
            LastUp = false;
        }

        // Scale
        drawImageRect.localScale = new Vector3(currentScale, currentScale, 1.0f);

        Vector3 toMove = (Vector3)((scalePivotPos - drawImageRect.pivot) * (scaleDiff));
        toMove.x *= DrawImageRectWidth;
        toMove.y *= DrawImageRectHeight;
        drawImageRect.transform.localPosition -= toMove;

        pixelWidth = DrawImageRectWidth / drawTexture.width;
        pixelHeight = DrawImageRectHeight / drawTexture.height;
    }

    // Move
    [Header("Movement")]
    public float scrollMoveSensitivity = 0.5f;
    private Vector3 startImagePos = Vector3.zero;
    private Vector3 startMousePos = Vector3.zero;
    bool scrollMoveEnabled = false;

    void Move()
    {
        Vector3 currentMove = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentMove.x += Input.mouseScrollDelta.y * scrollMoveSensitivity;
        }
        else
        {
            currentMove.y -= Input.mouseScrollDelta.y * scrollMoveSensitivity;
        }

        drawImageRect.transform.localPosition = drawImageRect.transform.localPosition + currentMove;
    }

    void ScrollButtonMove()
    {
        Vector3 mouseDelta = Input.mousePosition - startMousePos;

        drawImageRect.transform.localPosition = startImagePos + new Vector3(mouseDelta.x, mouseDelta.y, 0);
    }

    // Move History
    [Header("Move History")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;
    private List<IMove> movesHistory = new();
    private int actualMoveId = -1;

    public void Undo()
    {
        if (actualMoveId == -1) return;

        movesHistory[actualMoveId].Undo();
        --actualMoveId;
    }

    public void Redo()
    {
        if (movesHistory.Count == 0 || actualMoveId + 1 == movesHistory.Count) return;

        ++actualMoveId;
        movesHistory[actualMoveId].Do();
    }

    // Tools
    [Header("Tools")]
    [SerializeField] private Button brushButton;
    [SerializeField] private Button eraserButton;
    [SerializeField] private Button displacerButton;
    [SerializeField] private Button pipetteButton;
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
            case Tool.DisplacerTake:
            case Tool.DisplacerPut:
                displacerButton.interactable = true;
                break;
            case Tool.Pipette:
                pipetteButton.interactable = true;
                break;
        }

        action.Invoke();
    }

    void ChangeTool(Action action, bool undoDisplacerTake)
    {
        /*
        if (undoDisplacerSelect)
        {
            CheckDisplacerSelect(action, removeUnfinished);
            return;
        }
        */

        FinalChangeTool(action);
    }

    // Brush
    public void BrushButton()
    {
        if (!isLevelCreated)
            return;

        ChangeToBrush(true);
    }

    void ChangeToBrush(bool undoDisplacerTake)
    {
        ChangeTool(() =>
        {
            CurrentTool = Tool.Brush;
            brushButton.interactable = false;
        }, undoDisplacerTake);
    }

    // Eraser
    public void EraserButton()
    {
        if (!isLevelCreated)
            return;

        ChangeToEraser(true);
    }

    void ChangeToEraser(bool undoDisplacerTake)
    {
        ChangeTool(() =>
        {
            CurrentTool = Tool.Eraser;
            eraserButton.interactable = false;
        }, undoDisplacerTake);
    }

    // Displacer
    public void DisplacerButton()
    {
        if (!isLevelCreated)
            return;

        ChangeToDisplacer(true);
    }

    void ChangeToDisplacer(bool undoDisplacerTake)
    {
        ChangeTool(() =>
        {
            CurrentTool = Tool.DisplacerTake;
            displacerButton.interactable = false;
        }, undoDisplacerTake);
    }

    // Pipette
    public void PipetteButton()
    {
        if (!isLevelCreated)
            return;

        ChangeToPipette(true);
    }

    void ChangeToPipette(bool undoDisplacerTake)
    {
        ChangeTool(() =>
        {
            CurrentTool = Tool.Pipette;
            pipetteButton.interactable = false;
        }, undoDisplacerTake);
    }

    /*
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
    */

    // Cursor
    [Header("Cursor")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D brushCursor;
    [SerializeField] private Texture2D eraserCursor;
    [SerializeField] private Texture2D displacerTakeCursor;
    [SerializeField] private Texture2D displacerTakeXMarkCursor;
    [SerializeField] private Texture2D displacerPutCursor;
    [SerializeField] private Texture2D pipetteCursor;
    [SerializeField] private Texture2D pipetteXMarkCursor;
    [SerializeField] private Texture2D scrollMoveCursor;
    [SerializeField] private Texture2D scaleUpCursor;
    [SerializeField] private Texture2D scaleDownCursor;
    private bool isCursorInView = false;
    private bool _isDefaultTile = false;
    private bool IsDefaultTile
    {
        get
        {
            return _isDefaultTile;
        }
        set
        {
            if (_isDefaultTile != value)
            {
                _isDefaultTile = value;
                ToolCursor();
            }
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
                // TODO: Poprawic pozycje obrazków oraz pomyœleæ co zrobiæ z kolorami
                case Tool.Brush:
                    Cursor.SetCursor(brushCursor, new Vector2(0, 64), CursorMode.Auto);
                    break;
                case Tool.Eraser:
                    Cursor.SetCursor(eraserCursor, new Vector2(23, 64), CursorMode.Auto);
                    break;
                case Tool.DisplacerTake:
                    Cursor.SetCursor(IsDefaultTile ? displacerTakeXMarkCursor : displacerTakeCursor, new Vector2(26, 38), CursorMode.Auto);
                    break;
                case Tool.DisplacerPut:
                    Cursor.SetCursor(displacerPutCursor, new Vector2(38, 26), CursorMode.Auto);
                    break;
                case Tool.Pipette:
                    Cursor.SetCursor(IsDefaultTile ? pipetteXMarkCursor : pipetteCursor, new Vector2(38, 26), CursorMode.Auto);
                    break;
            }
        }
    }

    void ScaleCursor()
    {
        if (isCursorInView)
        {
            if (LastUp)
            {
                Cursor.SetCursor(scaleUpCursor, new Vector2(32, 32), CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(scaleDownCursor, new Vector2(32, 32), CursorMode.Auto);
            }
        }
    }

    void ScrollMoveCursor()
    {
        if (isCursorInView)
        {
            Cursor.SetCursor(scrollMoveCursor, new Vector2(32, 32), CursorMode.Auto);
        }
    }

    public void ShowToolCursor()
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

    public void HideToolCursor()
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

    // Brush Settings
    [Header("Brush Settings")]
    [SerializeField] private SimpleDropdown brushTypeDropdown;
    [SerializeField] private NumberInput powerInput;
    [SerializeField] private SimpleDropdown rotationDropdown;
    private TileType tileType = TileType.PathStraight;
    private int tilePower = 0;
    private int tileRotation = 0;

    public void TileTypeChanged(int value)
    {
        tileType = (TileType)(value + 1);
    }

    public void TilePowerChanged(int value, bool isEmpty)
    {
        tilePower = isEmpty ? 0 : value;
    }

    public void TileRotationChanged(int value)
    {
        tileRotation = value;
    }

    // Info
    [Header("Info Texts")]
    [SerializeField] private TextMeshProUGUI XText;
    [SerializeField] private TextMeshProUGUI YText;
    [SerializeField] private TextMeshProUGUI TypeText;
    [SerializeField] private TextMeshProUGUI PowerText;
    [SerializeField] private TextMeshProUGUI RotationText;

    private void GetPixelInfo(int x, int y)
    {
        Color color = drawTexture.GetPixel(x, y);
        XText.text = x.ToString();
        YText.text = (drawTexture.height - 1 - y).ToString();
        TypeText.text = TileDecoder.DecodeType(color).ToString();
        TileDecoder.DecodeAlpha(color.a, out int power, out int rotation);
        PowerText.text = power.ToString();
        RotationText.text = (rotation * 90).ToString() + "°";
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

        Color defaultTileColor = TileTypeColorMap.GetColor(TileType.Default);
        for (int x = 0; x < drawTexture.width; ++x)
        {
            for (int y = 0; y < drawTexture.height; ++y)
            {
                drawTexture.SetPixel(x, y, defaultTileColor);
                pointerIndicatorTexture.SetPixel(x, y, pointerIndicatorTexColor);
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

    // Pointer Values
    private Vector2Int lastPointerPos = new();
    private Color currentPointerColor = new();
    float GetLuminance(Color color)
    {
        return 0.2126f * color.linear.r + 0.7152f * color.linear.g + 0.0722f * color.linear.b;
    }

    void UpdateDrawPointer()
    {
        pointerIndicatorTexture.SetPixel(lastPointerPos.x, lastPointerPos.y, pointerIndicatorTexColor);
        lastPointerPos.x = PixelPosX;
        lastPointerPos.y = PixelPosY;
        Color imagePixelColor = drawTexture.GetPixel(lastPointerPos.x, lastPointerPos.y);
        currentPointerColor = GetLuminance(imagePixelColor) > 0.5f ? darkPointerColor : brightPointerColor;
        pointerIndicatorTexture.SetPixel(lastPointerPos.x, lastPointerPos.y, currentPointerColor);
        pointerIndicatorTexture.Apply();
    }

    public void SaveImage()
    {
        string filePath = StandaloneFileBrowser.SaveFilePanel("Save Level Map Image", Application.dataPath, "LevelMap", new ExtensionFilter[] { new("QOI", new string[] { "qoi" }), new("PNG", new string[] { "png" }) });

        if (string.IsNullOrEmpty(filePath))
            return;

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        byte[] bytes;

        switch (extension)
        {
            case ".png":
                bytes = drawTexture.EncodeToPNG();
                break;

            case ".qoi":
                bytes = drawTexture.EncodeToQOI();
                break;

            default:
                Debug.LogError("Unsupported file format: " + extension);
                return;
        }

        File.WriteAllBytes(filePath, bytes);
    }

    // Project
    /*
    public void NewProject()
    {
        // TODO: Open Window with defining new project image size and with cancel button oraz create button
    }

    public void LoadProject()
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

    public void SaveProject()
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
}