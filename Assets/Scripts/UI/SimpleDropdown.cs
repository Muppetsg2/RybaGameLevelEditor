using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(TMP_Dropdown))]
public class SimpleDropdown : MonoBehaviour
{
    [Header("General")]
    [SerializeField]
    private TMP_Dropdown dropdown;

    [Header("Font")]
    [SerializeField]
    private TMP_FontAsset font;

    [SerializeField]
    private uint labelFontSize;

    [SerializeField]
    private uint itemFontSize;

    [Header("Options")]
    [SerializeField]
    private uint value;

    [SerializeField]
    private List<string> options;

    [Header("Events")]
    public UnityEvent<int> onValueChanged;

    public int OptionsCount
    {
        get { return options.Count; }
    }

    void OnValidate()
    {
        if (dropdown != null)
        {
            if (dropdown.captionText != null)
            {
                dropdown.captionText.font = font;
                dropdown.captionText.fontSize = labelFontSize;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.font = font;
                dropdown.itemText.fontSize = itemFontSize;
            }
        }
    }

    void Start()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(newValue =>
            {
                onValueChanged?.Invoke(newValue);
            });

            if (dropdown.captionText != null)
            {
                dropdown.captionText.font = font;
                dropdown.captionText.fontSize = labelFontSize;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.font = font;
                dropdown.itemText.fontSize = itemFontSize;
            }

            ParseOptions();

            SetOption(value);
        }
        else
        {
            Debug.LogWarning($"Dropdown not set in {gameObject.name}.SimpleDropdown");
        }
    }

    public void SetOption(uint optIdx)
    {
        if (options.Count == 0)
        {
            value = 0;
        }
        else
        {
            value = (uint)Mathf.Clamp((int)optIdx, 0, options.Count - 1);
        }
        dropdown.value = (int)value;
    }

    public void AddOption(string optionName)
    {
        options.Add(optionName);
        ParseOptions();
    }

    public void EraseOption(uint optIdx)
    {
        if (optIdx < 0 || optIdx > options.Count - 1) return;

        options.RemoveAt((int)optIdx);
        ParseOptions();
    }

    public void AddOptions(List<string> newOptions)
    {
        options.AddRange(newOptions);
        ParseOptions();
    }

    public void ClearOptions()
    {
        options.Clear();
        ParseOptions();
    }

    private void ParseOptions()
    {
        dropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> dataList = new();
        foreach (string opt in options)
        {
            dataList.Add(new TMP_Dropdown.OptionData(opt, null, Color.white));
        }
        dropdown.AddOptions(dataList);
    }
}