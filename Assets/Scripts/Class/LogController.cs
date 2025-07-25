using UnityEngine;
using UnityEngine.UI;

public class LogController : MonoBehaviour
{
    public static LogController Instance = null;
    [Tooltip("The environment allow to show debug log")]
    public bool showDebugLog = true;
    public Text versionText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        Debug.Log("Current Environment--------------------------------------------------" + LoaderConfig.Instance?.currentHostName.ToString());
    }

    public void UpdateVersion(string env)
    {
        if (this.versionText != null) this.versionText.text = env + ": " + this.versionText.text;
    }

    public void debug(string _message = "")
    {
        if (LoaderConfig.Instance != null)
        {
            if (this.showDebugLog)
            {
                Debug.Log(_message);
            }
        }
    }

    public void debugError(string _message = "")
    {
        if (LoaderConfig.Instance != null)
        {
            if (this.showDebugLog)
            {
                Debug.Log(_message);
            }
        }
    }
}
