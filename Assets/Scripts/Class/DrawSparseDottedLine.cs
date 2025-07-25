using UnityEngine;
using UnityEngine.UI;

public class DrawSparseDottedLine : MonoBehaviour
{
    public enum LineOrientation
    {
        Horizontal,
        Vertical
    }

    public Color lineColor = Color.white; // Line color
    public float dashLength = 20f; // Length of each dash in pixels
    public float gapLength = 10f;  // Gap between dashes in pixels
    public float lineLength = 200f;  // Total length of the line in pixels
    public float lineWidth = 2f; // Width of the line
    public LineOrientation orientation = LineOrientation.Horizontal; // Line orientation

    private void Start()
    {
        DrawDottedLine();
    }

    private void DrawDottedLine()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        float start = -lineLength / 2f; // Start point of the line
        float current = start;

        while (current < start + lineLength)
        {
            float dashEnd = Mathf.Min(current + dashLength, start + lineLength); // Ensure it doesn't exceed the total length

            // Create a new UI Image for each dash
            GameObject dash = new GameObject("Dash", typeof(Image));
            dash.transform.SetParent(transform, false);
            Image dashImage = dash.GetComponent<Image>();
            dashImage.color = lineColor;

            RectTransform dashRectTransform = dash.GetComponent<RectTransform>();
            if (orientation == LineOrientation.Horizontal)
            {
                dashRectTransform.sizeDelta = new Vector2(dashEnd - current, lineWidth);
                dashRectTransform.anchoredPosition = new Vector2(current + (dashEnd - current) / 2f, 0);
            }
            else // Vertical
            {
                dashRectTransform.sizeDelta = new Vector2(lineWidth, dashEnd - current);
                dashRectTransform.anchoredPosition = new Vector2(0, current + (dashEnd - current) / 2f);
            }

            current = dashEnd + gapLength; // Move to the next dash position
        }
    }
}