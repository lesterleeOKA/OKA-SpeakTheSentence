using UnityEngine;
using UnityEngine.UI;

public class ResponsiveUI : MonoBehaviour
{
    private RectTransform rectTransform;
    public RectTransform parentRect;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        UpdateUIPosition();
    }

    void Update()
    {
        UpdateUIPosition();
    }

    void UpdateUIPosition()
    {
        if (rectTransform == null || this.parentRect == null)
            return;

        var canvasScaler = this.parentRect.GetComponent<CanvasScaler>();
        if (canvasScaler == null || this.parentRect == null)
            return;

        var referenceResolution = canvasScaler.referenceResolution;
        var sizeDelta = this.parentRect.sizeDelta;

        //LogController.Instance?.debug("parentRect size" + this.parentRect.sizeDelta);
        float referenceWidth = referenceResolution.x;
        if (sizeDelta.x <= referenceWidth)
        {
            //LogController.Instance?.debug("scale smaller!");
            this.rectTransform.sizeDelta = sizeDelta;
        }
        else
        {
            this.rectTransform.sizeDelta = referenceResolution;
        }
    }
}