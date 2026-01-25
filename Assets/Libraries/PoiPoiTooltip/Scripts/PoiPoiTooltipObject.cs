using UnityEngine;
using TMPro;

namespace PPTooltip
{
    [DisallowMultipleComponent]
    public class PoiPoiTooltipObject : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI infoText;

        private void Awake() {
        }

        private void Start() {
        }

        public void SetInfo(string text)
        {
            if (infoText == null) {
                return;
            }

            infoText.text = text;
        }
    }
}