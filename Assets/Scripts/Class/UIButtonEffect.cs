using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

[RequireComponent(typeof(EventTrigger))]
public class UIButtonEffect : MonoBehaviour
{
    private EventTrigger eventTrigger = null;
    public float scaleRatio = 0.75f;
    public bool pointDown = false;
    
    // Start is called before the first frame update
    void Start()
    {
        this.eventTrigger = this.GetComponent<EventTrigger>();
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener(scaleSmallBtn);
        eventTrigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener(scaleToOriginal);
        eventTrigger.triggers.Add(pointerUpEntry);
    }

    void scaleSmallBtn(BaseEventData data)
    {
        if(this.eventTrigger != null)
        {
            if (this.eventTrigger.enabled)
            {
                this.transform.DOScale(this.scaleRatio, 0.3f);
                this.pointDown = true;
            }
        }    
    }

    void scaleToOriginal(BaseEventData data)
    {
        if (this.eventTrigger != null)
        {
            if (this.eventTrigger.enabled) { 
                this.transform.DOScale(1f, 0.3f);
                this.pointDown = false;
            }
        }
    }
}
