using UnityEngine;
using UnityEngine.UI;

public class ShiningEffect : MonoBehaviour
{
    public float speed = 1f;
    public Color startColor = Color.white;
    public Color endColor = Color.yellow;
    private Image image;
    private Color initialColor; // Store initial color
    private bool isEffectActive = false; // Track if the effect is active

    private void Start()
    {
        this.image = GetComponent<Image>();
        this.initialColor = this.image.color;
    }

    private void Update()
    {
        if (this.isEffectActive)
        {
            float lerp = Mathf.PingPong(Time.time * speed, 1f);
            this.image.color = Color.Lerp(startColor, endColor, lerp);
        }
    }

    private void OnEnable()
    {
        this.isEffectActive = true; // Set the effect as active
    }

    private void OnDisable()
    {
        this.isEffectActive = false; // Set the effect as inactive
        if (this.image != null)
        {
            this.image.color = initialColor; // Reset to initial color
        }
    }
}