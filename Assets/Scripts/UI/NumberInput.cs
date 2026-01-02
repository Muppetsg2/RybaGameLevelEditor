using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(TMP_InputField))]
public class NumberInput : MonoBehaviour
{
    [Header("General")]
    [SerializeField]
    private TMP_InputField inputField;

    [SerializeField]
    private bool canHaveEmptyValue = false;

    [SerializeField]
    private TMP_FontAsset font;

    [SerializeField]
    private uint fontSize = 26;

    [SerializeField]
    [TextArea]
    private string placeholderText = "Input";

    [Header("Buttons")]
    [SerializeField]
    private MouseOverButton incrementButton;

    [SerializeField]
    private MouseOverButton decrementButton;

    [Header("Value")]
    [SerializeField]
    [Tooltip("Initial value of the number field on start")]
    private int initialValue = 0;

    [SerializeField]
    [Tooltip("Minimum value the number should be")]
    private int minimumValue = 0;

    [SerializeField]
    [Tooltip("Maximum value the number should be")]
    private int maximumValue = 100;

    [SerializeField]
    [Tooltip("Value which we increment and decrement our number with, everytime we click on the increment or decrement button")]
    private int changeFactor = 1;

    [SerializeField]
    private int CharacterLimit = 5;

    [Header("Long Click")]
    [SerializeField]
    [Tooltip("When user long clicks on the increment or decrement button, the number increases or decreases by change factor. This value determines how frequent the long click action will be invoked")]
    [Range(0.05f, 1.00f)]
    private float longClickInvokeRate = 0.1f;

    [Header("Events")]
    public UnityEvent<int, bool> onValueChanged;

    [Foldout("Info")]
    [ReadOnly]
    [SerializeField]
    private int value;

    [Foldout("Info")]
    [SerializeField]
    [ReadOnly]
    private bool emptyValue = false;

    void OnValidate()
    {
        if (inputField != null)
        {
            if (inputField.textComponent != null)
            {
                inputField.textComponent.font = font;
                inputField.textComponent.fontSize = fontSize;
            }

            if (inputField.placeholder != null)
            {
                TMP_Text placeholder = (TMP_Text)inputField.placeholder;
                placeholder.font = font;
                placeholder.fontSize = fontSize;
                placeholder.text = placeholderText;
            }
        }
    }

    void Start()
    {
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
        }

        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(newValue =>
            {
                emptyValue = newValue == null || newValue == string.Empty;

                if (!emptyValue)
                {
                    value = int.Parse(newValue);
                    ClampNumberAndSetInputField();
                }
                else if (emptyValue && !canHaveEmptyValue)
                {
                    emptyValue = false;
                    value = minimumValue;
                    ClampNumberAndSetInputField();
                }
                else
                {
                    value = minimumValue;
                }

                onValueChanged?.Invoke(value, emptyValue);
            });

            inputField.characterLimit = CharacterLimit;

            inputField.fontAsset = font;
            if (inputField.textComponent != null)
            {
                inputField.textComponent.font = font;
                inputField.textComponent.fontSize = fontSize;
            }

            if (inputField.placeholder != null)
            {
                TMP_Text placeholder = (TMP_Text)inputField.placeholder;
                placeholder.font = font;
                placeholder.fontSize = fontSize;
                placeholder.text = placeholderText;
            }
        }
        else
        {
            Debug.LogWarning($"InputField not set in {gameObject.name}.NumberInput");
        }

        emptyValue = false;
        value = initialValue;
        ClampNumberAndSetInputField();

        if (incrementButton != null)
        {
            incrementButton.longClickInvokeRate = longClickInvokeRate;
            incrementButton.button.onClick.AddListener(() => 
            {
                Increment();
                onValueChanged?.Invoke(value, emptyValue);
            });
            incrementButton.longClickAction = Increment;
        }
        else
        {
            Debug.LogWarning($"IncrementButton not set in {gameObject.name}.NumberInput");
        }

        if (decrementButton != null)
        {
            decrementButton.longClickInvokeRate = longClickInvokeRate;
            decrementButton.button.onClick.AddListener(() =>
            {
                Decrement();
                onValueChanged?.Invoke(value, emptyValue);
            });
            decrementButton.longClickAction = Decrement;
        }
        else
        {
            Debug.LogWarning($"DecrementButton not set in {gameObject.name}.NumberInput");
        }
    }

    public bool HasValue()
    {
        return !emptyValue;
    }

    public int GetValue()
    {
        return emptyValue ? minimumValue : value;
    }

    public void SetInitialValue(int newInitialValue)
    {
        initialValue = newInitialValue;
    }

    public void SetValue(int newValue)
    {
        emptyValue = false;
        value = newValue;
        ClampNumberAndSetInputField();
    }

    private void Increment()
    {
        if (emptyValue)
        {
            emptyValue = false;
            value = minimumValue;
        }
        value += changeFactor;
        ClampNumberAndSetInputField();
    }

    private void Decrement()
    {
        if (emptyValue)
        {
            emptyValue = false;
            value = maximumValue;
        }
        value -= changeFactor;
        ClampNumberAndSetInputField();
    }

    private void ClampNumberAndSetInputField()
    {
        value = Mathf.Clamp(value, minimumValue, maximumValue);

        if (inputField != null && !emptyValue)
        {
            inputField.text = value.ToString();
        }
    }
}