using TMPro;
using UnityEngine;
using NaughtyAttributes;
using System;

[RequireComponent(typeof(TMP_InputField))]
public class NumberInput : MonoBehaviour
{
    [Header("General")]
    [SerializeField]
    private TMP_InputField inputField;

    [SerializeField]
    private TMP_FontAsset fontAsset;

    [SerializeField]
    private int fontSize = 26;

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
    private float initialValue = 0;

    [SerializeField]
    [Tooltip("Minimum value the number should be")]
    private float minimumValue = 0;

    [SerializeField]
    [Tooltip("Maximum value the number should be")]
    private float maximumValue = 100;

    [SerializeField]
    [Tooltip("Value which we increment and decrement our number with, everytime we click on the increment or decrement button")]
    private float changeFactor = 1;

    [SerializeField]
    private int CharacterLimit = 5;

    [Header("Long Click")]
    [SerializeField]
    [Tooltip("When user long clicks on the increment or decrement button, the number increases or decreases by change factor. This value determines how frequent the long click action will be invoked")]
    [Range(0.05f, 1.00f)]
    private float longClickInvokeRate = 0.1f;

    [Foldout("Info")]
    [ReadOnly]
    [SerializeField]
    private float value;

    void OnValidate()
    {
        if (inputField != null)
        {
            if (inputField.textComponent != null) inputField.textComponent.fontSize = fontSize;
            if (inputField.placeholder != null)
            {
                TMP_Text placeholder = (TMP_Text)inputField.placeholder;
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
                if (newValue != null && newValue != string.Empty)
                {
                    value = float.Parse(newValue);
                }
                else
                {
                    value = minimumValue;
                }

                ClampNumberAndSetInputField();
            });

            inputField.characterLimit = CharacterLimit;

            inputField.fontAsset = fontAsset;
            if (inputField.textComponent != null) inputField.textComponent.fontSize = fontSize;
            if (inputField.placeholder != null)
            {
                TMP_Text placeholder = (TMP_Text)inputField.placeholder;
                placeholder.fontSize = fontSize;
                placeholder.text = placeholderText;
            }
        }
        else
        {
            Debug.LogWarning($"InputField not set in {gameObject.name}.NumberInput");
        }

        value = initialValue;
        ClampNumberAndSetInputField();

        if (incrementButton != null)
        {
            incrementButton.longClickInvokeRate = longClickInvokeRate;
            incrementButton.button.onClick.AddListener(Increment);
            incrementButton.longClickAction = Increment;
        }
        else
        {
            Debug.LogWarning($"IncrementButton not set in {gameObject.name}.NumberInput");
        }

        if (decrementButton != null)
        {
            decrementButton.longClickInvokeRate = longClickInvokeRate;
            decrementButton.button.onClick.AddListener(Decrement);
            decrementButton.longClickAction = Decrement;
        }
        else
        {
            Debug.LogWarning($"DecrementButton not set in {gameObject.name}.NumberInput");
        }
    }

    private void Increment()
    {
        value += changeFactor;
        ClampNumberAndSetInputField();
    }

    private void Decrement()
    {
        value -= changeFactor;
        ClampNumberAndSetInputField();
    }

    private void ClampNumberAndSetInputField()
    {
        value = Mathf.Clamp(value, minimumValue, maximumValue);

        if (inputField != null)
        {
            inputField.text = value.ToString();
        }
    }
}