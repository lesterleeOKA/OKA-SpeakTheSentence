using UnityEngine;

public class FixChildRotation : MonoBehaviour
{
    public Transform parentObject;
    public Transform childObject;

    private void Start()
    {
        if(this.childObject == null) this.childObject = this.transform;
    }
    void Update()
    {
        if(this.parentObject != null && this.childObject != null)
        {
            Quaternion parentRotation = parentObject.rotation;
            this.childObject.rotation = Quaternion.Inverse(parentRotation);
        }
    }
}
