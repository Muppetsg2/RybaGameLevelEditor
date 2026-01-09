using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NewProjectWindow : MonoBehaviour
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
    private Button createBtn;

    [SerializeField]
    private Button cancelBtn;

    [SerializeField]
    private GameObject window;

    [SerializeField]
    private uint defaultPreset = 0;

    [SerializeField]
    private List<Preset> presets;

    public UnityEvent<uint, uint> onCreate;
    public UnityEvent onCancel;

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
                    if (createBtn != null) createBtn.interactable = true;
                }
                else
                {
                    if (createBtn != null) createBtn.interactable = false;
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
                    if (createBtn != null) createBtn.interactable = true;
                }
                else
                {
                    if (createBtn != null) createBtn.interactable = false;
                }

                if (dropdownValueUpdateCounter != 0)
                {
                    --dropdownValueUpdateCounter;
                    return;
                }

                presetDropdown.SetOption((uint)(presetDropdown.OptionsCount - 1));
            });
        }

        if (createBtn != null)
        {
            createBtn.onClick.AddListener(Create);
        }

        if (cancelBtn != null)
        {
            cancelBtn.onClick.AddListener(Cancel);
        }
    }

    void Start()
    {
        CloseWindow();
    }

    private void Create()
    {
        uint width = widthInput != null ? (uint)widthInput.GetValue() : 1;
        uint height = heightInput != null ? (uint)heightInput.GetValue() : 1;
        onCreate?.Invoke(width, height);
    }

    private void Cancel()
    {
        onCancel?.Invoke();
    }

    public void OpenWindow()
    {
        if (window != null) window.SetActive(true);
    }

    public void CloseWindow()
    {
        if (window != null) window.SetActive(false);
    }
}