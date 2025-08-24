using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private Image progressBarImage;
    public float startX = 0f; // The starting position of the progress bar
    private RectTransform progressBarRect;

    private void Start()
    {
        if (this.progressBarImage != null)
        {
            this.progressBarRect = this.progressBarImage.rectTransform;
            this.startX = this.progressBarRect.localPosition.x;
        }
    }

    public void SetProgress(float progress, Action onCompleted=null)
    {
        progress = Mathf.Clamp01(progress);
        float newX = Mathf.Lerp(this.startX, 0f, progress);
        //LogController.Instance?.debug($"Setting progress bar to {progress * 100}% (new X position: {newX})");
        this.progressBarRect.DOLocalMoveX(newX, 0.5f).OnComplete(()=>
        {
            onCompleted?.Invoke();
        });
    }
}
