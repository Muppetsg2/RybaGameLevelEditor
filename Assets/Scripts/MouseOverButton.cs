using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MouseOverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("General")]
    public Button button;

    [HideIf("true")]
    public System.Action longClickAction;

    [HideIf("true")]
    public float longClickInvokeRate = 0;

    [Foldout("Info")]
    [ReadOnly]
    [SerializeField]
    private bool isMouseOver;

    [Foldout("Info")]
    [ReadOnly]
    [SerializeField]
    private bool didClickOnThisButton;

    [Foldout("Info")]
    [ReadOnly]
    [SerializeField]
    private float longClickCounter = 0;

    [ReadOnly]
    private readonly float longClickActionDelay = 1.1f;

    void Start()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            CancelInvoke(nameof(InvokeLongClickAction));

            didClickOnThisButton = false;
        }


        if (isMouseOver && Input.GetMouseButtonDown(0))
        {
            didClickOnThisButton = true;
        }


        if (didClickOnThisButton)
        {
            if (longClickCounter > longClickActionDelay)
            {
                InvokeRepeating(nameof(InvokeLongClickAction), 0, longClickInvokeRate);

                longClickCounter = 0;
            }
            else
            {
                longClickCounter += Time.deltaTime;
            }
        }
    }

    private void InvokeLongClickAction()
    {
        longClickAction?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
    }
}