using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class VersionText : MonoBehaviour
{
    private TextMeshProUGUI versionText;

    void Start()
    {
        versionText = GetComponent<TextMeshProUGUI>();
        if (versionText != null)
        {
            versionText.text = "Version: " + Application.version.ToString();
        }
    }
}