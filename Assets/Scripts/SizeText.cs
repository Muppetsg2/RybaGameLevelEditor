using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SizeText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Color valuesColor;
    [SerializeField] private Color xColor;

    private string valuesHex;
    private string xHex;

    private void Awake()
    {
        if (!text) text = GetComponent<TextMeshProUGUI>();

        valuesHex = ColorUtility.ToHtmlStringRGB(valuesColor);
        xHex = ColorUtility.ToHtmlStringRGB(xColor);
    }

    public void SetSize(int width, int height)
    {
        text.text = $"<color=#{valuesHex}><b>{width}</b></color> <color=#{xHex}>×</color> <color=#{valuesHex}><b>{height}</b></color>";
    }
}
