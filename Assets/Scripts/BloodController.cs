using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BloodController : MonoBehaviour
{
    public Image[] bloods;
    public Sprite[] bloodSprites;
    public int totalBload;
    // Start is called before the first frame update
    void Start()
    {
        this.InitialRetryTimes();
    }

    void InitialRetryTimes()
    {
        this.totalBload = LoaderConfig.Instance.gameSetup.retry_times;

        if (this.bloods.Length > this.totalBload)
        {
            for (int i = this.totalBload; i < this.bloods.Length; i++)
            {
                this.bloods[i].gameObject.SetActive(false);
            }
        }

        this.setBloods(true);
    }

    public void addBlood()
    {
        if (this.totalBload < this.bloods.Length)
        {
            this.totalBload += 1;
            if (this.bloods[this.totalBload - 1] != null)
            {
                this.bloods[this.totalBload - 1].sprite = this.bloodSprites[1];
                this.bloods[this.totalBload - 1].transform.DOScale(1.5f, 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }
    }

    public void setBloods(bool init = true, Action onCompleted = null)
    {
        if (!init)
        {
            if (this.totalBload > 0)
            {
                this.totalBload -= 1;
                if (this.bloods[this.totalBload] != null &&
                    this.bloods[this.totalBload].gameObject.activeInHierarchy)
                {
                    this.bloods[this.totalBload].sprite = this.bloodSprites[0];
                }
            }
            else
            {
                onCompleted?.Invoke();
            }
        }
        else
        {
            this.totalBload = LoaderConfig.Instance.gameSetup.retry_times;
            for (int i = 0; i < this.totalBload; i++)
            {
                if (this.bloods[i] != null && this.bloods[i].gameObject.activeInHierarchy)
                {
                    this.bloods[i].sprite = this.bloodSprites[1];
                }
            }
        }

    }
}
