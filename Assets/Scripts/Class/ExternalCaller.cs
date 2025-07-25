using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public static class ExternalCaller
{
    public static string GetCurrentDomainName
    {
        get
        {
            string absoluteUrl = Application.absoluteURL;
            Uri url = new Uri(absoluteUrl);
            if (LogController.Instance != null) LogController.Instance.debug("Host Name:" + url.Host);
            return url.Host;
        }
    }

    public static void ReLoadCurrentPage()
    {
#if !UNITY_EDITOR
        Application.ExternalEval("location.reload();");
#else
        LoaderConfig.Instance?.changeScene(1);
#endif
    }

    public static void BackToHomeUrlPage(bool isLogined = false)
    {
#if !UNITY_EDITOR
        if (isLogined)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if(LoaderConfig.Instance.gameSetup.gameExitType == 1)
                {
                    string javascript = $@"
                        if (window.self !== window.top) {{
                            console.log('This page is inside an iframe');
                            window.parent.postMessage({{ action: 'exit' }}, '*');
                        }}
                        else {{
                            history.back();
                        }}
                    ";
                    Application.ExternalEval(javascript);
                }
                else if (LoaderConfig.Instance.gameSetup.gameExitType == 2)
                {
                    LoaderConfig.Instance?.changeScene(1);
                    return;
                }
            }
            else
            {
                // for rainbowone.app
                Application.ExternalEval($"location.hash = 'exit'");
            }
        }
        else
        {
            if (LoaderConfig.Instance.gameSetup.lang == 1)
            {
                LoaderConfig.Instance?.changeScene(1);
                return;
            }

            if (!string.IsNullOrEmpty(LoaderConfig.Instance.gameSetup.returnUrl))
            {
                string javascript = $@"
                    if (window.self !== window.top) {{
                        window.parent.postMessage('closeIframe', '*');
                    }} else {{
                        window.location.replace('{LoaderConfig.Instance.gameSetup.returnUrl}');
                    }}
                ";
                Application.ExternalEval(javascript);
                return;
            }

            string hostname = GetCurrentDomainName;
            if (hostname.Contains("dev.openknowledge.hk"))
            {
                string baseUrl = GetCurrentDomainName;
                string newUrl = $"https://{baseUrl}/RainbowOne/webapp/OKAGames/SelectGames/";
                if (LogController.Instance != null) LogController.Instance.debug("full url:" + newUrl);

                string javascript = $@"
                    if (window.self !== window.top) {{
                        console.log('This page is inside an iframe');
                        window.parent.postMessage('closeIframe', '*');
                    }}
                    else {{
                        window.location.replace('{newUrl}');
                    }}
                ";
                Application.ExternalEval(javascript);
            }
            else if (hostname.Contains("www.rainbowone.app"))
            {
                  string Production = "https://www.starwishparty.com/";
                  string javascript = $@"
                    if (window.self !== window.top) {{
                        console.log('This page is inside an iframe');
                        window.parent.postMessage('closeIframe', '*');
                    }}
                    else {{
                        window.location.replace('{Production}');
                    }}
                ";
                Application.ExternalEval(javascript);
            }
            else if (hostname.Contains("localhost"))
            {
                LoaderConfig.Instance?.changeScene(1);
            }
            else
            {
                Application.ExternalEval($"location.hash = 'exit'");
            }
        }   
#else
        LoaderConfig.Instance?.changeScene(1);
#endif
    }

    public static void HiddenLoadingBar()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval("hiddenLoadingBar()");  
        /*Application.ExternalEval("replaceUrlPart()"); */
#endif
    }

    public static void UpdateLoadBarStatus(string status = "")
    {
        LogController.Instance?.debug(status);
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval($"updateLoadingText('{status}')");
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    public static extern void SetWebPageTitle(string title);
    [DllImport("__Internal")]
    private static extern int GetDeviceType();
#else
    public static void SetWebPageTitle(string title) { }
#endif

    // 0: Other, 1: iOS (iPad/iPhone), 2: Windows
    public static int DeviceType
    {
        get
        {
            int deviceType = 0;
#if UNITY_WEBGL && !UNITY_EDITOR
            deviceType = GetDeviceType();
#endif
            return deviceType;
        }
    }

    public static void RemoveReturnUrlFromAddressBar()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval(@"
        (function() {
            var url = new URL(window.location.href);
            url.searchParams.delete('returnUrl');
            window.history.replaceState({}, document.title, url.pathname + url.search + url.hash);
        })();
    ");
#endif
    }
}

