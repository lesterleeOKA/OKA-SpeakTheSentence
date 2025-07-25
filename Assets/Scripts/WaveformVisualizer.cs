using UnityEngine;
using UnityEngine.UI;

public class WaveformVisualizer : MonoBehaviour
{
    [Header("UI Components")]
    public RawImage waveformImage;

    [Header("Waveform Settings")]
    public int textureWidth = 256;
    public int textureHeight = 64;
    public Color waveformColor = Color.green;
    public Color backgroundColor = Color.black;
    public float waveFormGain = 10f;
    public int pixelSpacing = 2;
    public int baselineHeight = 2;
    public int pixelWidth = 3;

    [Header("Waveform Speed")]
    public float waveformSpeed = 1f;

    private float speedAccumulator = 0f;

    private Texture2D waveformTexture;
    private float[] circularBuffer;
    private int bufferIndex = 0;

    void Start()
    {
        if(this.waveformImage == null) this.waveformImage = GetComponent<RawImage>();
        waveformTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        waveformTexture.filterMode = FilterMode.Point;
        waveformTexture.wrapMode = TextureWrapMode.Clamp;

        if (waveformImage)
        {
            waveformImage.texture = waveformTexture;
        }

        ClearTexture();

        // Initialize the circular buffer
        circularBuffer = new float[textureWidth];
    }

    public void UpdateWaveform(float[] audioSamples)
    {
        if (waveformTexture == null || audioSamples == null) return;
        // Add new audio samples to the circular buffer
        foreach (float sample in audioSamples)
        {
            circularBuffer[bufferIndex] = sample * waveFormGain;
            bufferIndex = (bufferIndex + 1) % circularBuffer.Length;
        }

        // Draw the new waveform on the right
        DrawWaveform();
    }


    private void DrawWaveform()
    {
        // Accumulate the speed
        speedAccumulator += waveformSpeed;

        // Only shift pixels when the accumulated speed reaches or exceeds 1
        if (speedAccumulator < 1f)
        {
            return; // Skip this frame if the accumulated speed is less than 1
        }

        // Calculate how many pixels to shift
        int pixelsToShift = Mathf.FloorToInt(speedAccumulator);
        speedAccumulator -= pixelsToShift; // Subtract the shifted amount from the accumulator

        // Get the current pixels of the texture
        Color[] pixels = waveformTexture.GetPixels();

        // Shift all pixels to the left by the calculated number of pixels
        for (int x = pixelsToShift * (pixelWidth + pixelSpacing); x < textureWidth; x++)
        {
            for (int y = 0; y < textureHeight; y++)
            {
                pixels[(y * textureWidth) + (x - pixelsToShift * (pixelWidth + pixelSpacing))] = pixels[(y * textureWidth) + x];
            }
        }

        // Clear the rightmost columns based on pixelWidth, pixelSpacing, and pixelsToShift
        for (int x = textureWidth - pixelsToShift * (pixelWidth + pixelSpacing); x < textureWidth; x++)
        {
            for (int y = 0; y < textureHeight; y++)
            {
                pixels[(y * textureWidth) + x] = backgroundColor;
            }
        }

        // Draw the new waveform pixels on the rightmost columns
        int centerY = textureHeight / 2;
        float sampleValue = circularBuffer[(bufferIndex - 1 + circularBuffer.Length) % circularBuffer.Length];
        int lineHeight = Mathf.FloorToInt(Mathf.Abs(sampleValue) * (textureHeight / 2));

        lineHeight = Mathf.Max(lineHeight, this.baselineHeight);

        for (int x = textureWidth - pixelsToShift * (pixelWidth + pixelSpacing); x < textureWidth - pixelSpacing; x++)
        {
            for (int y = centerY - lineHeight; y <= centerY + lineHeight; y++)
            {
                if (y >= 0 && y < textureHeight)
                {
                    pixels[(y * textureWidth) + x] = waveformColor;
                }
            }
        }

        // Apply the updated pixels to the texture
        waveformTexture.SetPixels(pixels);
        waveformTexture.Apply();
    }

    public void ClearTexture()
    {
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }
        waveformTexture.SetPixels(pixels);
        waveformTexture.Apply();
    }
}