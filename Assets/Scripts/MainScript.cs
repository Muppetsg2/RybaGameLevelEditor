using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using NativeDialogs.Runtime;

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
            if (_pixelPos.x != value || cleared)
            {
                cleared = false;
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
            if (_pixelPos.y != value || cleared)
            {
                cleared = false;
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

    // Multiple Draws
    private List<DrawData> draws = new();

    // Multiple Erases
    private List<EraseData> erases = new();

    // Displacer
    private DisplacerTakeMove displacerTake = null;
    private DisplacerData displacerMoveData = new();

    // Window Block
    private bool isInteractionBlocked = false;

    private void Start()
    {
        drawImageRect = drawImage.GetComponent<RectTransform>();
        DefaultCursor();

        if (newProjectWindow != null)
        {
            newProjectWindow.onCreate.AddListener(OnNewProjectCreate);
            newProjectWindow.onCancel.AddListener(OnNewProjectCancel);
        }
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
                if (CurrentTool == Tool.DisplacerTake)
                {
                    if (drawTexture.GetPixel(PixelPosX, PixelPosY) != TileTypeColorMap.GetColor(TileType.Default))
                    {
                        displacerTake = new DisplacerTakeMove(ref drawTexture, _pixelPos, out displacerMoveData);
                        CurrentTool = Tool.DisplacerPut;
                        colorChanged = true;
                    }
                }
                else if (CurrentTool == Tool.DisplacerPut)
                {
                    displacerTake = null;
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

                AddMoveToHistory(moveToAdd);

                if (colorChanged)
                {
                    UpdateDrawPointer();
                }
            }

            if (Input.GetMouseButton(0))
            {
                if (CurrentTool == Tool.Brush)
                {
                    if (!draws.Exists(d => d.Pos == _pixelPos))
                    {
                        DrawData data = new()
                        {
                            Pos = _pixelPos,
                            OldColor = drawTexture.GetPixel(_pixelPos.x, _pixelPos.y),
                            NewColor = TileEncoder.EncodeColor(tileType, tilePower, tileRotation)
                        };

                        draws.Add(data);
                        _ = new DrawMove(ref drawTexture, data);
                    }
                }
                else if (CurrentTool == Tool.Eraser)
                {
                    if (drawTexture.GetPixel(PixelPosX, PixelPosY) != TileTypeColorMap.GetColor(TileType.Default) && !erases.Exists(d => d.Pos == _pixelPos))
                    {
                        EraseData data = new()
                        {
                            Pos = _pixelPos,
                            RemovedColor = drawTexture.GetPixel(_pixelPos.x, _pixelPos.y),
                            BackgroundColor = TileTypeColorMap.GetColor(TileType.Default)
                        };

                        erases.Add(data);
                        _ = new EraseMove(ref drawTexture, data);
                    }
                }

                UpdateDrawPointer();
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
            if (pointerIndicatorTexture)
            {
                ClearDrawPointer();
            }

            if (IsScaling)
            {
                IsScaling = false;
            }

            if (IsDefaultTile)
            {
                IsDefaultTile = false;
            }
        }

        if (isLevelCreated && !isInteractionBlocked && Input.GetMouseButtonUp(0))
        {
            IMove moveToAdd = null;
            if (CurrentTool == Tool.Brush)
            {
                if (draws.Count != 0)
                {
                    if (draws.Count == 1)
                    {
                        moveToAdd = new DrawMove(ref drawTexture, draws[0]);
                    }
                    else if (draws.Count > 1)
                    {
                        moveToAdd = new MultipleDrawMove(ref drawTexture, draws);
                    }

                    draws.Clear();
                }
            }
            else if (CurrentTool == Tool.Eraser)
            {
                if (erases.Count != 0)
                {
                    if (erases.Count == 1)
                    {
                        moveToAdd = new EraseMove(ref drawTexture, erases[0]);
                    }
                    else if (erases.Count > 1)
                    {
                        moveToAdd = new MultipleEraseMove(ref drawTexture, erases);
                    }

                    erases.Clear();
                }
            }

            AddMoveToHistory(moveToAdd);
            UpdateDrawPointer();
        }

        if (Input.GetMouseButtonUp(2))
        {
            scrollMoveEnabled = false;

            if (isCursorInView && !isInteractionBlocked)
            {
                ToolCursor();
            }
            else
            {
                DefaultCursor();
            }
        }

        if (IsScaling && Input.GetKeyUp(KeyCode.LeftControl))
        {
            IsScaling = false;
        }
    }

    // Default Options
    private void ResetSettings()
    {
        cleared = false;

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

    private float GetDefaultScale()
    {
        float scaleX = ViewRectWidth / DrawImageRectWidth;
        float scaleY = ViewRectHeight / DrawImageRectHeight;
        float computedScale = Mathf.Min(scaleX, scaleY) * 0.8f;
        return Mathf.Clamp(computedScale, minScale, maxScale);
    }

    private float GetMinScale()
    {
        float scaleX = 2f / DrawImageRectWidth;
        float scaleY = 2f / DrawImageRectHeight;
        float computedScale = Mathf.Min(scaleX, scaleY) * 0.8f;
        return computedScale;
    }

    private float GetMaxScale()
    {
        float scaleX = ViewRectWidth * 0.5f; // ViewRectWidth / 2f
        float scaleY = ViewRectHeight * 0.5f; // ViewRectHeight / 2f
        float computedScale = Mathf.Min(scaleX, scaleY) * 0.8f;
        return computedScale;
    }

    private void Scale()
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

    private void Move()
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

    private void ScrollButtonMove()
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

    private void AddMoveToHistory(IMove moveToAdd)
    {
        if (moveToAdd == null) return;

        if (actualMoveId + 1 != movesHistory.Count)
        {
            int startId = actualMoveId + 1;
            movesHistory.RemoveRange(startId, movesHistory.Count - startId);
        }

        movesHistory.Add(moveToAdd);
        ++actualMoveId;
    }

    public void Undo()
    {
        if (actualMoveId == -1) return;

        if (CheckDisplacerTakeUndoRedo(true)) return;

        movesHistory[actualMoveId].Undo();
        --actualMoveId;
    }

    public void Redo()
    {
        if (movesHistory.Count == 0 || actualMoveId + 1 == movesHistory.Count) return;

        if (CheckDisplacerTakeUndoRedo(false)) return;

        ++actualMoveId;
        movesHistory[actualMoveId].Do();
    }

    private bool CheckDisplacerTakeUndoRedo(bool undo)
    {
        if (messageBox == null || displacerTake == null)
        {
            return false;
        }

        messageBox.ShowDialog("Displacer",
                "You are currently in Displacer placement mode.\n" +
                "If you undo now, your changes will be lost and cannot be redone.\n\n" +
                "Are you sure you want to continue?",
                "No", "Yes", x =>
                {
                    switch (x)
                    {
                        case DialogResult.Confirm:
                            {
                                displacerTake.Undo();
                                displacerTake = null;
                                CurrentTool = Tool.DisplacerTake;

                                if (undo)
                                {
                                    movesHistory[actualMoveId].Undo();
                                    --actualMoveId;
                                }
                                else
                                {
                                    ++actualMoveId;
                                    movesHistory[actualMoveId].Do();
                                }

                                break;
                            }
                        case DialogResult.Cancel:
                            {
                                break;
                            }
                    }
                }
            );

        return true;
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

    private void FinalChangeTool(Action action)
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

    private void ChangeTool(Action action, bool undoDisplacerTake)
    {
        if (undoDisplacerTake)
        {
            CheckDisplacerTakeChangeTool(action);
            return;
        }

        FinalChangeTool(action);
    }

    // Brush
    public void BrushButton()
    {
        if (!isLevelCreated)
            return;

        ChangeToBrush(true);
    }

    private void ChangeToBrush(bool undoDisplacerTake)
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

    private void ChangeToEraser(bool undoDisplacerTake)
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

    private void ChangeToDisplacer(bool undoDisplacerTake)
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

    private void ChangeToPipette(bool undoDisplacerTake)
    {
        ChangeTool(() =>
        {
            CurrentTool = Tool.Pipette;
            pipetteButton.interactable = false;
        }, undoDisplacerTake);
    }

    private void CheckDisplacerTakeChangeTool(Action action)
    {
        if (messageBox == null || displacerTake == null)
        {
            FinalChangeTool(action);
            return;
        }

        messageBox.ShowDialog("Displacer",
                "You are in Displacer placement mode.\n" +
                "Changing the tool will discard your changes.\n\n" +
                "Are you sure you want to change the tool?",
                "No", "Yes", x =>
                {
                    switch (x)
                    {
                        case DialogResult.Confirm:
                            {
                                displacerTake.Undo();
                                displacerTake = null;

                                FinalChangeTool(action);
                                break;
                            }
                        case DialogResult.Cancel:
                            {
                                break;
                            }
                    }
                }
            );
    }

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

    private void DefaultCursor()
    {
        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
    }

    private void ToolCursor()
    {
        if (isCursorInView)
        {
            switch (CurrentTool)
            {
                // TODO: Poprawic pozycje obrazków oraz pomyœleæ co zrobiæ z kolorami
                case Tool.Brush:
                    Cursor.SetCursor(brushCursor, new Vector2(0, 0), CursorMode.Auto);
                    break;
                case Tool.Eraser:
                    Cursor.SetCursor(eraserCursor, new Vector2(0, 0), CursorMode.Auto);
                    break;
                case Tool.DisplacerTake:
                    Cursor.SetCursor(IsDefaultTile ? displacerTakeXMarkCursor : displacerTakeCursor, new Vector2(0, 0), CursorMode.Auto);
                    break;
                case Tool.DisplacerPut:
                    Cursor.SetCursor(displacerPutCursor, new Vector2(0, 0), CursorMode.Auto);
                    break;
                case Tool.Pipette:
                    Cursor.SetCursor(IsDefaultTile ? pipetteXMarkCursor : pipetteCursor, new Vector2(0, 0), CursorMode.Auto);
                    break;
            }
        }
    }

    private void ScaleCursor()
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

    private void ScrollMoveCursor()
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
    private void GenerateGrid(int mapWidth, int mapHeight)
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
    private bool cleared = false;

    private float GetLuminance(Color color)
    {
        return 0.2126f * color.linear.r + 0.7152f * color.linear.g + 0.0722f * color.linear.b;
    }

    private void ClearDrawPointer()
    {
        cleared = true;
        pointerIndicatorTexture.SetPixel(PixelPosX, PixelPosY, pointerIndicatorTexColor);
        pointerIndicatorTexture.Apply();
    }

    private void UpdateDrawPointer()
    {
        if (cleared) return;
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
        string filePath = StandaloneFileBrowser.SaveFilePanel("Save Level Map Image", Application.dataPath, "LevelMap", new ExtensionFilter[] { new("QOI ", new string[] { "qoi" }), new("PNG ", new string[] { "png" }) });

        if (string.IsNullOrEmpty(filePath)) return;

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
    [Header("Project")]
    [SerializeField] private NativeDialogComponent messageBox;
    [SerializeField] private NewProjectWindow newProjectWindow;
    public UnityEvent onLoadProjectFailed;

    public void NewProject()
    {
        if (newProjectWindow == null)
        {
            Debug.Log("New Project Window not found!");
            return;
        }

        isInteractionBlocked = true;
        newProjectWindow.OpenWindow();
    }

    private void OnNewProjectCreate(uint width, uint height)
    {
        if (messageBox == null) return;

        messageBox.ShowDialog("New Project",
                "You are about to create a new project.\n" +
                "Any unsaved changes will be lost.\n\n" +
                "Do you want to continue?",
                "No", "Yes", x =>
                {
                    switch (x)
                    {
                        case DialogResult.Confirm:
                            {
                                CreateLevelTexture(width, height);
                                newProjectWindow.CloseWindow();
                                isInteractionBlocked = false;
                                break;
                            }
                        case DialogResult.Cancel:
                            {
                                break;
                            }
                    }
                }
            );
    }

    private void OnNewProjectCancel()
    {
        newProjectWindow.CloseWindow();
        isInteractionBlocked = false;
    }

    public void LoadProject(bool start)
    {
        string[] filePaths = StandaloneFileBrowser.OpenFilePanel("Load Level Map Project", Application.dataPath, new ExtensionFilter[] { new ExtensionFilter("Level Project ", new string[] { "lep" }) }, false);
        if (filePaths.Length != 0 && !string.IsNullOrEmpty(filePaths[0]))
        {
            ProjectData data = FileWriter.ReadFromBinaryFile<ProjectData>(filePaths[0]);

            Debug.Log("FileName: " + data.fileName);
            Debug.Log("Date: " + data.date);
            Debug.Log("Editor Version: " + data.editorVersion);
            Debug.Log("Format Version: " + data.formatVersion);

            if (data.formatVersion > ProjectFileFormatConst.GetProjectFileFormatVersion())
            {
                Debug.Log("Newer Version of Project File not supported!");
                if (start) onLoadProjectFailed?.Invoke();
                return;
            }

            CreateLevelTexture(data.width, data.height);

            foreach (TileData tile in data.tiles)
            {
                Color color = new Color32(tile.r, tile.g, tile.b, tile.a);
                drawTexture.SetPixel(tile.x, tile.y, color);
            }
            drawTexture.Apply();
        }
        else
        {
            Debug.Log("No files loaded");
            if (start) onLoadProjectFailed?.Invoke();
            return;
        }
    }

    public void SaveProject()
    {
        if (isLevelCreated)
        {
            string filePath = StandaloneFileBrowser.SaveFilePanel("Save Level Map Project", Application.dataPath, "Project", new ExtensionFilter[] { new ExtensionFilter("Level Project ", new string[] { "lep" }) });
            if (!string.IsNullOrEmpty(filePath))
            {
                List<TileData> ModifiedTiles = new();
                for (int x = 0; x < drawTexture.width; ++x)
                {
                    for (int y = 0; y < drawTexture.height; ++y)
                    {
                        Color c = drawTexture.GetPixel(x, y);
                        if (c != TileTypeColorMap.GetColor(TileType.Default))
                        {
                            Color32 color = c;
                            TileData tileData = new()
                            {
                                x = x,
                                y = y,
                                r = color.r,
                                g = color.g,
                                b = color.b,
                                a = color.a
                            };

                            ModifiedTiles.Add(tileData);
                        }
                    }
                }

                ProjectData data = new()
                {
                    fileName = Path.GetFileNameWithoutExtension(filePath),
                    date = DateTime.Now,
                    editorVersion = Application.version,
                    formatVersion = ProjectFileFormatConst.GetProjectFileFormatVersion(),
                    width = (uint)drawTexture.width,
                    height = (uint)drawTexture.height,
                    tiles = ModifiedTiles.ToArray()
                };
                FileWriter.WriteToBinaryFile(filePath, data, false);
            }
        }
    }

    // Application
    public void CloseEditor()
    {
        if (messageBox == null)
        {
            Application.Quit();
            return;
        }

        messageBox.ShowDialog("Exit Editor",
                "You are about to close the editor.\n" +
                "Any unsaved changes will be lost.\n\n" +
                "Do you want to exit?",
                "Stay", "Exit", x =>
                {
                    switch (x)
                    {
                        case DialogResult.Confirm:
                            {
                                Application.Quit();
                                break;
                            }
                        case DialogResult.Cancel:
                            {
                                break;
                            }
                    }
                }
            );
    }
}