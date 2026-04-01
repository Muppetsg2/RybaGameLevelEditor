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
                    if (createBtn != null) createBtn.interactable = true;

                    height = heightInput.GetValue();
                }
                else
                {
                    if (createBtn != null) createBtn.interactable = false;
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
                    if (createBtn != null) createBtn.interactable = true;

                    width = widthInput.GetValue();
                }
                else
                {
                    if (createBtn != null) createBtn.interactable = false;
                }

                if (checkPreset && !IsPresetValue((int)presetDropdown.OptionIndex, width, newValue))
                {
                    presetDropdown.SetOption((uint)(presetDropdown.OptionsCount - 1));
                }
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

    public void OpenWindow(int width, int height)
    {
        if (window != null) window.SetActive(true);

        widthInput.SetValue(width);
        heightInput.SetValue(height);

        uint optIdx = 0;
        foreach (Preset p in presets)
        {
            if (p.width == width && p.height == height)
            {
                break;
            }
            ++optIdx;
        }

        presetDropdown.SetOption(optIdx);
    }

    public void CloseWindow()
    {
        if (window != null) window.SetActive(false);
    }

    private bool IsPresetValue(int presetIdx, int width, int height)
    {
        return presetIdx >= 0 && presetIdx < presets.Count && presets[presetIdx].width == width && presets[presetIdx].height == height;
    }
}