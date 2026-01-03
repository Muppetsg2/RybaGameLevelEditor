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

    private int dropdownValueUpdateCounter = 2;

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

                dropdownValueUpdateCounter = 2;

                widthInput.SetValue(presets[newValue].width);
                heightInput.SetValue(presets[newValue].height);
            });
        }

        if (widthInput != null)
        {
            widthInput.SetInitialValue(presets[(int)defaultPreset].width);
            widthInput.onValueChanged.AddListener((newValue, emptyValue) =>
            {
                if (!emptyValue && heightInput.HasValue())
                {
                    if (newProjectBtn != null) newProjectBtn.interactable = true;
                }
                else
                {
                    if (newProjectBtn != null) newProjectBtn.interactable = false;
                }

                if (dropdownValueUpdateCounter != 0)
                {
                    --dropdownValueUpdateCounter;
                    return;
                }

                presetDropdown.SetOption((uint)(presetDropdown.OptionsCount - 1));
            });
        }

        if (heightInput != null)
        {
            heightInput.SetInitialValue(presets[(int)defaultPreset].height);
            heightInput.onValueChanged.AddListener((newValue, emptyValue) =>
            {
                if (!emptyValue && widthInput.HasValue())
                {
                    if (newProjectBtn != null) newProjectBtn.interactable = true;
                }
                else
                {
                    if (newProjectBtn != null) newProjectBtn.interactable = false;
                }

                if (dropdownValueUpdateCounter != 0)
                {
                    --dropdownValueUpdateCounter;
                    return;
                }
                
                presetDropdown.SetOption((uint)(presetDropdown.OptionsCount - 1));
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

    public void NewProjectEditor()
    {
        uint width = widthInput != null ? (uint)widthInput.GetValue() : 1;
        uint height = heightInput != null ? (uint)heightInput.GetValue() : 1;
        onNewProject?.Invoke(width, height);
        OpenEditor();
    }

    public void LoadProjectEditor()
    {
        onLoadProject?.Invoke();
        OpenEditor();
    }

    public void OpenEditor()
    {
        if (startMenuCanvas != null) startMenuCanvas.gameObject.SetActive(false);
        if (editorCanvas != null) editorCanvas.gameObject.SetActive(true);
    }
}