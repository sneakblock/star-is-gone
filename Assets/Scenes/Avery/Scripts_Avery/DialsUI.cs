using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialsUI : MonoBehaviour
{
    [SerializeField] Transform handle;
    [SerializeField] Image fill;
    [SerializeField] Text valTxt;
    [SerializeField] Button channelOff;
    [SerializeField] Button channel1;
    [SerializeField] Button channel2;
    [SerializeField] Button channel3;
    [SerializeField] Button channel4;
    [SerializeField] Button channel5;
    [SerializeField] Button channel6;
    [SerializeField] Button channel7;
    [SerializeField] Button channel8;
    [SerializeField] Button channel9;
    [SerializeField] Button channel10;
    [SerializeField] Button exit;
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject knob;
    public VideoRenderer vid;
    public bool hasBeenTriggered = false;
    public int chan = 0;
    float defaultAngle = 360;

    void Start()
    {
        canvas = GetComponentInChildren<Canvas>();
        canvas.enabled = false;
        hasBeenTriggered = false;
        HideUI();

    }

    void Update()
    {

    }

    public void ShowUI()
    {
        canvas.enabled = true;
        Cursor.visible = true;
    }

    public void HideUI()
    {
        canvas.enabled = false;
        Cursor.visible = false;
    }

    public void OnClickExit()
    {
        HideUI();
    }
    public void OnTurnOff()
    {
        valTxt.text = "Off";
        Debug.Log("Off");
        Quaternion r = Quaternion.AngleAxis(defaultAngle, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(0, 0, 90);
        vid.currChannel = 0;
    }
    public void OnChannel1()
    {
        valTxt.text = "1";
        Debug.Log("Channel 1");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 27, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-360 / 11, 0, 90);
        vid.currChannel = 1;
    }
    public void OnChannel2()
    {
        valTxt.text = "2";
        Debug.Log("Channel 2");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 54, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-(2 * 360) / 11, 0, 90);
        vid.currChannel = 2;
    }
    public void OnChannel3()
    {
        valTxt.text = "3";
        Debug.Log("Channel 3");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 81, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-(3 * 360) / 11, 0, 90);
        vid.currChannel = 3;
    }
    public void OnChannel4()
    {
        valTxt.text = "4";
        Debug.Log("Channel 4");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 108, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-(4 * 360) / 11, 0, 90);
        vid.currChannel = 4;
    }
    public void OnChannel5()
    {
        valTxt.text = "5";
        Debug.Log("Channel 5");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 135, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-(5 * 360) / 11, 0, 90);
        vid.currChannel = 5;
    }
    public void OnChannel6()
    {
        valTxt.text = "6";
        Debug.Log("Channel 6");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 162, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-(6 * 360) / 11, 0, 90);
        vid.currChannel = 6;
    }
    public void OnChannel7()
    {
        valTxt.text = "7";
        Debug.Log("Channel 7");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 189, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-(7 * 360) / 11, 0, 90);
        vid.currChannel = 7;
    }
    public void OnChannel8()
    {
        valTxt.text = "8";
        Debug.Log("Channel 8");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 216, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-(8 * 360) / 11, 0, 90);
        vid.currChannel = 8;
    }
    public void OnChannel9()
    {
        valTxt.text = "9";
        Debug.Log("Channel 9");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 243, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-(9 * 360) / 11, 0, 90);
        vid.currChannel = 9;
    }
    public void OnChannel10()
    {
        valTxt.text = "10";
        Debug.Log("Channel 10");
        Quaternion r = Quaternion.AngleAxis(defaultAngle - 270, Vector3.forward);
        handle.rotation = r;
        knob.transform.rotation = Quaternion.Euler(-(10 * 360) / 11, 0, 90);
        vid.currChannel = 10;
    }
}
