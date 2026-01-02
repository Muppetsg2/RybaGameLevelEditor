using System;
using UnityEngine;

public class PopupSystem : MonoBehaviour
{
    public static PopupSystem instance = null;

    [SerializeField] private PopupWindow windowPrefab = null;
    [SerializeField] private Transform popupParent = null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        instance = this;
    }

    public static PopupWindow CreateWindow(string title, string message, string opt1Text, Action opt1, string opt2Text, Action opt2)
    {
        PopupWindow window = Instantiate(instance.windowPrefab, instance.popupParent);
        window.Title = title;
        window.Message = message;
        window.Opt1Text = opt1Text;
        window.opt1 = opt1;
        window.Opt2Text = opt2Text;
        window.opt2 = opt2;
        return window;
    }
}
