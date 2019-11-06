using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PluginTest : MonoBehaviour {

    const string pluginName = "com.dawntaylorgames.unity.MyPlugin";

    class AlertViewCallback : AndroidJavaProxy
    {
        private System.Action<int> alertHandler;
        public AlertViewCallback(System.Action<int> alertHandlerIn) : base(pluginName + "$AlertViewCallback")
        {
            alertHandler = alertHandlerIn;
        }
        public void OnButtonTapped(int index)
        {
            Debug.Log("Button tapped: " + index);
            if (alertHandler != null)
                alertHandler(index);
        }
    }

    class ShareImageCallback : AndroidJavaProxy
    {
        private System.Action<int> shareHandler;
        public ShareImageCallback(System.Action<int> shareHandlerIn) : base (pluginName + "$ShareImageCallback")
        {
            shareHandler = shareHandlerIn;
        }
        public void OnShareComplete(int result)
        {
            Debug.Log("ShareComplete: " + result);
            isSharingScreenShot = false;
            if (shareHandler != null)
                shareHandler(result);
        }
    }

    static AndroidJavaClass _pluginClass;
    static AndroidJavaObject _pluginInstance;
    float _elapsedTime;
    public Text mainText;
    public Button shareButton;
    public Text timeStamp;
    static bool isSharingScreenShot;
    public RectTransform webPanel;
    public RectTransform buttonStrip;

    static public AndroidJavaClass pluginClass
    {
        get
        {
            if (_pluginClass == null)
            {
                _pluginClass = new AndroidJavaClass(pluginName);
                AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
                _pluginClass.SetStatic<AndroidJavaObject>("mainActivity", activity);
            }
            return _pluginClass;
        }
    }
    static public AndroidJavaObject pluginInstance
    {
        get
        {
            if(_pluginInstance == null)
            {
                _pluginInstance = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
            }
            return _pluginInstance;
        }
    }

    private void Start()
    {
        Debug.Log("Elapsed Time " + GetElapsedTime());
        if (timeStamp != null)
            timeStamp.gameObject.SetActive(false);
    }   

    //void Update()
    //{

    //    _elapsedTime += Time.deltaTime;
    //    if (_elapsedTime >= 5)
    //    {
    //        _elapsedTime -= 5;
    //        mainText.text = "Tick: " + GetElapsedTime();
    //    }
    //    if (Input.GetMouseButtonDown(0))//Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
    //    {
    //        ShowAlertDialog(new string[] { "Alert Title", "Alert Message", "Button 1", "Button 2" }, (int obj) =>
    //         {
    //             Debug.Log("Local Handler called: " + obj);
    //         });
    //    }
    //}

    double GetElapsedTime()
    {
        if (Application.platform == RuntimePlatform.Android)
            return pluginInstance.Call<double>("getElapsedTime");
        Debug.LogWarning("Wrong platform");
        return 0.0;
    }

    void ShowAlertDialog(string[] strings, System.Action<int> handler = null)
    {
        if (strings.Length < 3)
        {
            Debug.LogError("AlertView requires at least 3 strings");
            return;
        }
        if (Application.platform == RuntimePlatform.Android)
            pluginInstance.Call("showAlertView", new object[] { strings, new AlertViewCallback(handler) });
        else
            Debug.LogWarning("AlertView not suppoerted on this platform");
    }

    public void ShareButtonTapped()
    {
        if (shareButton != null)
            shareButton.gameObject.SetActive(false);
        if(timeStamp != null)
        {
            timeStamp.text = System.DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            timeStamp.gameObject.SetActive(true);
        }
        ShareScreenShot(Application.productName + " screenshot", (int result) =>
        {
            Debug.Log("Share complete with: " + result);
            ShowAlertDialog(new string[] { "Share Complete", "Share completed with: " + result, "OK" });
            if (shareButton != null)
                shareButton.gameObject.SetActive(true);
            if (timeStamp != null)
                timeStamp.gameObject.SetActive(false);
        });

    }

    public void ShareScreenShot(string caption, System.Action<int> shareComplete)
    {
        if (isSharingScreenShot)
        {
            Debug.LogError("Already sharing screenshot - aborting");
            return;
        }
        isSharingScreenShot = true;
        StartCoroutine(WaitForEndOfFrameCoroutine(caption, shareComplete));
    }

    IEnumerator WaitForEndOfFrameCoroutine(string caption, System.Action<int> shareComplete)
    {
        yield return new WaitForEndOfFrame();
        Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
        Debug.Log("Image size: " + image.width + " x " + image.height);
        byte[] imagePNG = image.EncodeToPNG();
        Debug.Log("PNG size: " + imagePNG.Length);
        if(Application.platform == RuntimePlatform.Android)
        {
            pluginInstance.Call("shareImage", new object[] { imagePNG, caption, new ShareImageCallback(shareComplete) });
        }
        Destroy(image);
    }

    public void OpenWebView(string URL, int pixelShift)
    {
        if (Application.platform == RuntimePlatform.Android)
            pluginInstance.Call("showWebView", new object[] { URL, pixelShift });
    }

    public void CloseWebView(System.Action<int> closeComplete)
    {
        if (Application.platform == RuntimePlatform.Android)
            pluginInstance.Call("closeWebView", new object[] { new ShareImageCallback(closeComplete) });
        else
            closeComplete(0);
    }

    public void OpenWebViewTapped()
    {
        Canvas parentCanvas = buttonStrip.GetComponentInParent<Canvas>();
        int stripHeight = (int)(buttonStrip.rect.height * parentCanvas.scaleFactor + 0.5f);
        webPanel.gameObject.SetActive(true);
        OpenWebView("http://www.cwgtech.com", stripHeight);
    }

    public void CloseWebViewTapped()
    {
        CloseWebView((int result) =>
        {
            webPanel.gameObject.SetActive(false);
        });
    }
}
