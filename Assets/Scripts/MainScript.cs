using NativeDialogs.Runtime;
using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum Tool { Brush, Eraser, DisplacerTake, DisplacerPut, Pipette };

public class MainScript : MonoBehaviour
{
    // General Flags
    private bool isLevelCreated = false;

    // Colors
    [Header("Colors")]
    [SerializeField] private Color pointerIndicatorTexColor = Color.clear;
    [SerializeField] private Color rotationIndicatorTexColor = Color.clear;
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

    [Header("Rotation Indicator Texture")]
    [SerializeField] private RawImage rotationIndicatorImage;
    private Texture2D rotationIndicatorTexture = null;
    [SerializeField][Range(0.0f, 1.0f)] private float rotationIndicatorBrightFactor = 0.35f;
    [SerializeField][Range(0.0f, 1.0f)] private float rotationIndicatorDarkerFactor = 0.35f;

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
                    UpdateRotationIndicatorPixel(PixelPosX, PixelPosY);
                    rotationIndicatorTexture.Apply();
                    UpdateDrawPointer();
                }
            }

            bool leftMouseBtnHold = Input.GetMouseButton(0);
            bool rightMouseBtnHold = Input.GetMouseButton(1);
            if (leftMouseBtnHold || rightMouseBtnHold)
            {
                if (CurrentTool == Tool.Eraser || (rightMouseBtnHold && CurrentTool == Tool.Brush))
                {
                    if (rightMouseBtnHold)
                    {
                        brushButton.interactable = true;
                        eraserButton.interactable = false;
                        Cursor.SetCursor(eraserCursor.cursorImage, eraserCursor.cursorPosition, CursorMode.Auto);
                    }

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
                else if (CurrentTool == Tool.Brush)
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

                UpdateRotationIndicatorTexture();
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

        bool leftMouseBtnUp = Input.GetMouseButtonUp(0);
        bool rightMouseBtnUp = Input.GetMouseButtonUp(1);
        if (isLevelCreated && !isInteractionBlocked && (leftMouseBtnUp || rightMouseBtnUp))
        {
            IMove moveToAdd = null;
            if (CurrentTool == Tool.Eraser || (rightMouseBtnUp && CurrentTool == Tool.Brush))
            {
                if (rightMouseBtnUp)
                {
                    brushButton.interactable = false;
                    eraserButton.interactable = true;
                    Cursor.SetCursor(brushCursor.cursorImage, brushCursor.cursorPosition, CursorMode.Auto);
                }

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
            else if (CurrentTool == Tool.Brush)
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
    [Serializable]
    public struct CursorInfo
    {
        public Texture2D cursorImage;
        public Vector2 cursorPosition;
    };

    [Header("Cursor")]
    [SerializeField] private CursorInfo defaultCursor;
    [SerializeField] private CursorInfo brushCursor;
    [SerializeField] private CursorInfo eraserCursor;
    [SerializeField] private CursorInfo displacerTakeCursor;
    [SerializeField] private CursorInfo displacerTakeXMarkCursor;
    [SerializeField] private CursorInfo displacerPutCursor;
    [SerializeField] private CursorInfo pipetteCursor;
    [SerializeField] private CursorInfo pipetteXMarkCursor;
    [SerializeField] private CursorInfo scrollMoveCursor;
    [SerializeField] private CursorInfo scaleUpCursor;
    [SerializeField] private CursorInfo scaleDownCursor;
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
        Cursor.SetCursor(defaultCursor.cursorImage, defaultCursor.cursorPosition, CursorMode.Auto);
    }

    private void ToolCursor()
    {
        if (isCursorInView)
        {
            switch (CurrentTool)
            {
                case Tool.Brush:
                    Cursor.SetCursor(brushCursor.cursorImage, brushCursor.cursorPosition, CursorMode.Auto);
                    break;
                case Tool.Eraser:
                    Cursor.SetCursor(eraserCursor.cursorImage, eraserCursor.cursorPosition, CursorMode.Auto);
                    break;
                case Tool.DisplacerTake:
                    Cursor.SetCursor(
                        IsDefaultTile ? displacerTakeXMarkCursor.cursorImage : displacerTakeCursor.cursorImage,
                        IsDefaultTile ? displacerTakeXMarkCursor.cursorPosition : displacerTakeCursor.cursorPosition,
                        CursorMode.Auto
                    );
                    break;
                case Tool.DisplacerPut:
                    Cursor.SetCursor(displacerPutCursor.cursorImage, displacerPutCursor.cursorPosition, CursorMode.Auto);
                    break;
                case Tool.Pipette:
                    Cursor.SetCursor(
                        IsDefaultTile ? pipetteXMarkCursor.cursorImage : pipetteCursor.cursorImage,
                        IsDefaultTile ? pipetteXMarkCursor.cursorPosition : pipetteCursor.cursorPosition,
                        CursorMode.Auto);
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
                Cursor.SetCursor(scaleUpCursor.cursorImage, scaleUpCursor.cursorPosition, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(scaleDownCursor.cursorImage, scaleDownCursor.cursorPosition, CursorMode.Auto);
            }
        }
    }

    private void ScrollMoveCursor()
    {
        if (isCursorInView)
        {
            Cursor.SetCursor(scrollMoveCursor.cursorImage, scrollMoveCursor.cursorPosition, CursorMode.Auto);
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

        rotationIndicatorTexture = new((int)width * rotationIndicatorPixelWidth, (int)height * rotationIndicatorPixelHeight)
        {
            filterMode = FilterMode.Point
        };

        for (int x = 0; x < rotationIndicatorTexture.width; ++x)
        {
            for (int y = 0; y < rotationIndicatorTexture.height; ++y)
            {
                rotationIndicatorTexture.SetPixel(x, y, rotationIndicatorTexColor);
            }
        }

        rotationIndicatorTexture.Apply();
        rotationIndicatorImage.texture = rotationIndicatorTexture;
        rotationIndicatorImage.color = Color.white;

        isLevelCreated = true;

        GenerateGrid(drawTexture.width, drawTexture.height);

        ResetSettings();
    }

    // Rotation Indicator Texture
    private const int rotationIndicatorPixelWidth = 9;
    private const int rotationIndicatorPixelHeight = 9;

    private static readonly Vector2Int[] ArrowDownOffsets =
    {
        new(4, 1),
        new(3, 2),
        new(2, 3),
        new(5, 2),
        new(6, 3)
    };

    private Vector2Int GetRotationMarkerOffset(int rotation)
    {
        // rotation:
        // 0 = down
        // 1 = left
        // 2 = up
        // 3 = right
        //
        // Blok 9×9 (lokalne wspó³rzêdne):
        //
        // y=8  . . . . . . . . .
        // y=7  . . . . U . . . .
        // y=6  . . . . . . . . .
        // y=5  . . . . . . . . .
        // y=4  . L . . C . . R .
        // y=3  . . . . . . . . .
        // y=2  . . . . . . . . .
        // y=1  . . . . D . . . .
        // y=0  . . . . . . . . .
        //
        // C = (4,4)

        return rotation switch
        {
            0 => new Vector2Int(4, 1), // down
            1 => new Vector2Int(1, 4), // left
            2 => new Vector2Int(4, 7), // up
            3 => new Vector2Int(7, 4), // right
            _ => new Vector2Int(4, 4),
        };
    }

    private bool IsRotationMarkerDrawn(int x, int y)
    {
        int baseX = x * rotationIndicatorPixelWidth;
        int baseY = y * rotationIndicatorPixelHeight;

        bool res = false;
        for (int i = 0; i < 5; ++i)
        {
            Vector2Int offset = GetRotationMarkerOffset(i);
            Color a = rotationIndicatorTexture.GetPixel(baseX + offset.x, baseY + offset.y);
            if (a != rotationIndicatorTexColor)
            {
                res = true;
                break;
            }
        }

        return res;
    }

    private Vector2Int RotateArrowOffset(Vector2Int offset, int rotation)
    {
        Vector2Int center = new(4, 4);
        Vector2Int p = offset - center;

        for (int i = 0; i < rotation; ++i)
        {
            p = new Vector2Int(p.y, -p.x);
        }

        p += center;

        return new Vector2Int(p.x, p.y);
    }

    private void DrawRotationArrow(int x, int y, Color color, int rotation)
    {
        int baseX = x * rotationIndicatorPixelWidth;
        int baseY = y * rotationIndicatorPixelHeight;

        foreach (Vector2Int offset in ArrowDownOffsets)
        {
            Vector2Int rotated = RotateArrowOffset(offset, rotation);

            rotationIndicatorTexture.SetPixel(baseX + rotated.x, baseY + rotated.y, color);
        }
    }

    private Color AdjustBrightness(Color color, float brightnessDelta)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        v = Mathf.Clamp01(v + brightnessDelta);
        return Color.HSVToRGB(h, s, v);
    }

    private Color GetRotationIndicatorColor(Color color)
    {
        float lum = GetLuminance(color);
        Color markerColor = lum > 0.05f ? AdjustBrightness(color, -rotationIndicatorDarkerFactor) : AdjustBrightness(color, rotationIndicatorBrightFactor);
        return markerColor;
    }

    private void ClearRotationIndicatorPixel(int x, int y)
    {
        int baseX = x * rotationIndicatorPixelWidth;
        int baseY = y * rotationIndicatorPixelHeight;

        for (int dx = 0; dx < rotationIndicatorPixelWidth; ++dx)
        {
            for (int dy = 0; dy < rotationIndicatorPixelHeight; ++dy)
            {
                rotationIndicatorTexture.SetPixel(baseX + dx, baseY + dy, rotationIndicatorTexColor);
            }
        }
    }

    private void UpdateRotationIndicatorPixel(int x, int y)
    {
        if (IsRotationMarkerDrawn(x, y))
        {
            ClearRotationIndicatorPixel(x, y);
        }

        Color imagePixelColor = drawTexture.GetPixel(x, y);
        if (imagePixelColor == TileTypeColorMap.GetColor(TileType.Default)) return;

        Color markerColor = GetRotationIndicatorColor(imagePixelColor);

        TileDecoder.DecodeAlpha(imagePixelColor.a, out _, out int rotation);

        DrawRotationArrow(x, y, markerColor, rotation);
    }

    private void UpdateRotationIndicatorTexture()
    {
        for (int x = 0; x < drawTexture.width; ++x)
        {
            for (int y = 0; y < drawTexture.height; ++y)
            {
                UpdateRotationIndicatorPixel(x, y);
            }
        }
        rotationIndicatorTexture.Apply();
    }

    // Grid
    private void DrawOutline(int x, int y)
    {
        int baseX = x * gridPixelsPerPixelWidth;
        int baseY = y * gridPixelsPerPixelHeight;

        for (int i = 0; i < gridPixelsPerPixelWidth; ++i)
        {
            for (int j = 0; j < gridPixelsPerPixelHeight; ++j)
            {
                if (i == 0 || i == gridPixelsPerPixelWidth - 1 || j == 0 || j == gridPixelsPerPixelHeight - 1)
                {
                    gridTexture.SetPixel(baseX + i, baseY + j, gridColor);
                }
            }
        }
    }

    private void GenerateGrid(int mapWidth, int mapHeight)
    {
        int texWidth = mapWidth * gridPixelsPerPixelWidth;
        int texHeight = mapHeight * gridPixelsPerPixelHeight;

        gridTexture = new Texture2D(texWidth, texHeight)
        {
            filterMode = FilterMode.Point
        };

        for (int y = 0; y < texWidth; ++y)
        {
            for (int x = 0; x < texHeight; ++x)
            {
                gridTexture.SetPixel(x, y, Color.clear);
            }
        }

        for (int y = 0; y < mapWidth; ++y)
        {
            for (int x = 0; x < mapHeight; ++x)
            {
                DrawOutline(x, y);
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
        string filePath = StandaloneFileBrowser.SaveFilePanel("Save Level Map Image", Application.dataPath, "LevelMap", new ExtensionFilter[] { new("QOI", new string[] { "qoi" }), new("PNG", new string[] { "png" }) });

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
    private enum ProjectFileAction
    {
        Load,
        Save
    }

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

    private void ShowProjectFileErrorMessage(ProjectFileAction action, string error)
    {
        string reason = string.IsNullOrEmpty(error) ? "Unknown error." : error;

        string actionText = action.ToString();
        string actionLower = actionText.ToLower();

        string title = $"{actionText} Project Error";
        string message = $"The project could not be {actionLower}ed.\n\nReason:\n{reason}";

        messageBox.ShowDialog(title, message, "OK", x => {});
    }

    public void ShowInfoMessage(string title, string message)
    {
        string safeTitle = string.IsNullOrWhiteSpace(title) ? "Info" : title;
        string safeMessage = string.IsNullOrWhiteSpace(message) ? "(No message provided)" : message;

        messageBox.ShowDialog(safeTitle, safeMessage, "OK", x => { });
    }

    private void ShowUpdateProjectMessage(string originalDirectory, string fileName)
    {
        messageBox.ShowDialog("Outdated Project",
                "The loaded project file uses an older format.\n\n" +
                "Do you want to update the project file now?\n",
                "No", "Yes", x =>
                {
                    switch (x)
                    {
                        case DialogResult.Confirm:
                            {
                                SaveProject(originalDirectory, fileName);
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

    public void LoadProject(bool start)
    {
        string[] filePaths = StandaloneFileBrowser.OpenFilePanel("Load Level Map Project", Application.dataPath, new ExtensionFilter[] { new ExtensionFilter("Level Project", new string[] { "lep" }) }, false);
        if (filePaths.Length != 0 && !string.IsNullOrEmpty(filePaths[0]))
        {
            if (!ProjectFileFormatSerializer.TryReadData(filePaths[0], out ProjectData data, out int formatVersion, out ProjectFileError error))
            {
                string debugMessage;
                string errorMessage;

                switch (error)
                {
                    case ProjectFileError.FileNotFound:
                        debugMessage = "Project file not found.";
                        errorMessage = "The selected project file could not be found.";
                        break;

                    case ProjectFileError.InvalidMagic:
                        debugMessage = "Invalid file signature (magic bytes do not match).";
                        errorMessage = "The selected file is not a valid Level Project file.";
                        break;

                    case ProjectFileError.UnsupportedFormatVersion:
                        debugMessage = "Unsupported project file format version.";
                        errorMessage = "This project was created with a newer or unsupported version.";
                        break;

                    case ProjectFileError.InvalidMapSize:
                        debugMessage = "Invalid or unsupported map size in project file.";
                        errorMessage = "This project contains an unsupported map size.";
                        break;

                    case ProjectFileError.InvalidTileCount:
                        debugMessage = "Invalid tile count in project file.";
                        errorMessage = "This project file contains invalid tile data.";
                        break;

                    case ProjectFileError.InvalidStringLength:
                        debugMessage = "Invalid string length detected (file may be corrupted).";
                        errorMessage = "This project file appears to be corrupted and cannot be loaded.";
                        break;

                    case ProjectFileError.CannotOpenFile:
                        debugMessage = "Failed to open project file (access denied or file in use).";
                        errorMessage = "The selected project file could not be opened.";
                        break;

                    case ProjectFileError.CorruptedData:
                        debugMessage = "Corrupted project file data detected.";
                        errorMessage = "This project file is corrupted and cannot be loaded.";
                        break;

                    default:
                        debugMessage = "Unknown error occurred while loading project file.";
                        errorMessage = "An unknown error occurred while loading the project.";
                        break;
                }

                Debug.LogError(debugMessage);

                if (start)
                    onLoadProjectFailed?.Invoke();

                ShowProjectFileErrorMessage(ProjectFileAction.Load, errorMessage);
                return;
            }

            Debug.Log("FileName: " + data.fileName);
            Debug.Log("Date: " + data.date);
            Debug.Log("Editor Version: " + data.editorVersion);
            Debug.Log("Format Version: " + formatVersion);

            CreateLevelTexture(data.width, data.height);

            foreach (TileData tile in data.tiles)
            {
                Color color = new Color32(tile.r, tile.g, tile.b, tile.a);
                drawTexture.SetPixel(tile.x, tile.y, color);

                TileDecoder.DecodeAlpha(tile.a, out _, out int rotation);
                DrawRotationArrow(tile.x, tile.y, GetRotationIndicatorColor(color), rotation);
            }
            rotationIndicatorTexture.Apply();
            drawTexture.Apply();

            bool deprecatedFormat = formatVersion < ProjectFileFormatSerializer.FORMAT_VERSION;
            if (deprecatedFormat)
            {
                ShowUpdateProjectMessage(Path.GetDirectoryName(filePaths[0]), data.fileName);
            }

            // ShowInfoMessage("Project Loaded", "The project has been loaded successfully!");
        }
        else
        {
            Debug.Log("No project file was selected.");

            if (start)
                onLoadProjectFailed?.Invoke();

            ShowProjectFileErrorMessage(ProjectFileAction.Load, "No project file was selected.");
        }
    }

    private void SaveProject(string directory = "", string fileName = "Project")
    {
        if (isLevelCreated)
        {
            string filePath = StandaloneFileBrowser.SaveFilePanel("Save Level Map Project", string.IsNullOrEmpty(directory) ? Application.dataPath : directory, fileName, new ExtensionFilter[] { new ExtensionFilter("Level Project", new string[] { "lep" }) });
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
                    date = DateTime.Now,
                    editorVersion = Application.version,
                    fileName = Path.GetFileNameWithoutExtension(filePath),
                    width = (uint)drawTexture.width,
                    height = (uint)drawTexture.height,
                    tiles = ModifiedTiles.ToArray()
                };

                if (!ProjectFileFormatSerializer.TryWriteData(filePath, data, out ProjectFileError error))
                {
                    string debugMessage;
                    string errorMessage;

                    switch (error)
                    {
                        case ProjectFileError.InvalidMapSize:
                            debugMessage = "Invalid or unsupported map size.";
                            errorMessage = "The project contains an unsupported map size and cannot be saved.";
                            break;

                        case ProjectFileError.TilesNull:
                            debugMessage = "Tiles array is not initialized.";
                            errorMessage = "The project tiles are not initialized and cannot be saved.";
                            break;

                        case ProjectFileError.InvalidTileArraySize:
                            debugMessage = "Tile array size is invalid.";
                            errorMessage = "The project contains invalid tile data and cannot be saved";
                            break;

                        case ProjectFileError.InvalidStringLength:
                            debugMessage = "String length is invalid (possibly corrupted).";
                            errorMessage = "The project contains invalid data and cannot be saved.";
                            break;

                        case ProjectFileError.CannotOpenFile:
                            debugMessage = "File could not be opened (access denied or file in use).";
                            errorMessage = "The project file could not be opened for saving.";
                            break;

                        case ProjectFileError.WriteFailed:
                            debugMessage = "Write operation failed.";
                            errorMessage = "The project could not be saved due to a write error.";
                            break;

                        default:
                            debugMessage = "Unknown error occurred while saving project file.";
                            errorMessage = "An unknown error occurred and the project could not be saved.";
                            break;
                    }

                    Debug.LogError(debugMessage);
                    ShowProjectFileErrorMessage(ProjectFileAction.Save, errorMessage);
                    return;
                }

                ShowInfoMessage("Project Saved", "The project has been saved successfully!");
            }
        }
    }

    public void SaveProjectButton()
    {
        SaveProject();
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