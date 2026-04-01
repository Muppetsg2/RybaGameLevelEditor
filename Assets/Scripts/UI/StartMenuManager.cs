using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.Events;

public class StartMenuManager : MonoBehaviour
{
    [Serializable]
    struct Preset
    {
        public string name;
        public int width;
        public int height;
    };

    [SerializeField]
    private SimpleDropdown presetDropdown;

    [SerializeField]
    private NumberInput widthInput;

    [SerializeField]
    private NumberInput heightInput;

    [SerializeField]
    private Button newProjectBtn;

    [SerializeField]
    private Button loadProjectBtn;

    [SerializeField]
    private Canvas startMenuCanvas;

    [SerializeField]
    private Canvas editorCanvas;

    [SerializeField]
    private uint defaultPreset = 0;

    [SerializeField]
    private List<Preset> presets;

    public UnityEvent onLoadProject;
    public UnityEvent<uint, uint> onNewProject;

    private bool checkPreset = true;

    void Awake()
    {
        if (presetDropdown != null)
        {
            presetDropdown.ClearOptions();
            List<string> options = new();
            foreach (Preset p in presets)
            {
                options.Add(p.name);
            }
            options.Add("Custom");
            presetDropdown.AddOptions(options);
            presetDropdown.SetOption(defaultPreset);

            presetDropdown.onValueChanged.AddListener(newValue =>
            {
                if (newValue == presetDropdown.OptionsCount - 1) return;

                checkPreset = false;
                widthInput.SetValue(presets[newValue].width);
                checkPreset = true;
                heightInput.SetValue(presets[newValue].height);
            });
        }

        if (widthInput != null)
        {
            widthInput.SetInitialValue(presets[(int)defaultPreset].width);
            widthInput.onValueChanged.AddListener((newValue, emptyValue) =>
            {
                int height = 0;
                if (!emptyValue && heightInput != null && heightInput.HasValue())
                {
                    if (newProjectBtn != null) newProjectBtn.interactable = true;

                    height = heightInput.GetValue();
                }
                else
                {
                    if (newProjectBtn != null) newProjectBtn.interactable = false;
                }

                if (checkPreset && !IsPresetValue((int)presetDropdown.OptionIndex, newValue, height))
                {
                    presetDropdown.SetOption((uint)(presetDropdown.OptionsCount - 1));
                }
            });
        }

        if (heightInput != null)
        {
            heightInput.SetInitialValue(presets[(int)defaultPreset].height);
            heightInput.onValueChanged.AddListener((newValue, emptyValue) =>
            {
                int width = 0;
                if (!emptyValue && widthInput != null && widthInput.HasValue())
                {
                    if (newProjectBtn != null) newProjectBtn.interactable = true;

                    width = widthInput.GetValue();
                }
                else
                {
                    if (newProjectBtn != null) newProjectBtn.interactable = false;
                }

                if (checkPreset && !IsPresetValue((int)presetDropdown.OptionIndex, width, newValue))
                {
                    presetDropdown.SetOption((uint)(presetDropdown.OptionsCount - 1));
                }
            });
        }

        if (newProjectBtn != null)
        {
            newProjectBtn.onClick.AddListener(NewProjectEditor);
        }

        if (loadProjectBtn != null)
        {
            loadProjectBtn.onClick.AddListener(LoadProjectEditor);
        }
    }

    void Start()
    {
        CloseEditor();
    }

    public void NewProjectEditor()
    {
        uint width = widthInput != null ? (uint)widthInput.GetValue() : 1;
        uint height = heightInput != null ? (uint)heightInput.GetValue() : 1;
        OpenEditor();
        onNewProject?.Invoke(width, height);
    }

    public void LoadProjectEditor()
    {
        OpenEditor();
        onLoadProject?.Invoke();
    }

    public void OpenEditor()
    {
        if (startMenuCanvas != null) startMenuCanvas.gameObject.SetActive(false);
        if (editorCanvas != null) editorCanvas.gameObject.SetActive(true);
    }

    public void CloseEditor()
    {
        if (editorCanvas != null) editorCanvas.gameObject.SetActive(false);
        if (startMenuCanvas != null) startMenuCanvas.gameObject.SetActive(true);
    }

    private bool IsPresetValue(int presetIdx, int width, int height)
    {
        return presetIdx >= 0 && presetIdx < presets.Count && presets[presetIdx].width == width && presets[presetIdx].height == height;
    }
}