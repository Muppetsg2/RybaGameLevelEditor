using System;
using UnityEngine;
using TMPro;

public class PopupWindow : MonoBehaviour
{
    public Action opt1 = null;
    public Action opt2 = null;

    private string _title = "%Title%";
    public string Title
    {
        get
        {
            return _title;
        }
        set
        {
            if (_title != value)
            {
                _title = value;
                titleHandler.text = _title;
            }
        }
    }

    private string _message = "%Message%";
    public string Message
    {
        get
        {
            return _message;
        }
        set
        {
            if (_message != value)
            {
                _message = value;
                messageHandler.text = _message;
            }
        }
    }

    private string _opt1text = "%Option1%";
    public string Opt1Text
    {
        get
        {
            return _opt1text;
        }
        set
        {
            if (_opt1text != value)
            {
                _opt1text = value;
                opt1Handler.text = _opt1text;
            }
        }
    }

    private string _opt2text = "%Option2%";
    public string Opt2Text
    {
        get
        {
            return _opt2text;
        }
        set
        {
            if (_opt2text != value)
            {
                _opt2text = value;
                opt2Handler.text = _opt2text;
            }
        }
    }

    [SerializeField] private TextMeshProUGUI titleHandler = null;
    [SerializeField] private TextMeshProUGUI messageHandler = null;
    [SerializeField] private TextMeshProUGUI opt1Handler = null;
    [SerializeField] private TextMeshProUGUI opt2Handler = null;

    public void Opt1Button()
    {
        opt1?.Invoke();
        Destroy(this.gameObject);
    }

    public void Opt2Button()
    {
        opt2?.Invoke();
        Destroy(this.gameObject);
    }
}
