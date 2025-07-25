using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class HelpTool : MonoBehaviour
{
    public int numberOfHelp = 0;
    public CanvasGroup cg;
    public RawImage popNumBg;
    public Texture[] popNumBgTextures;
    public TextMeshProUGUI help_number_text;
    private AudioSource audioEffect = null;
    public Material grayScaleMat;
    private float originalScale;

    void Start()
    {
        this.originalScale = 1f;
        if (LoaderConfig.Instance != null) 
            this.numberOfHelp = LoaderConfig.Instance.gameSetup.numberOfHelpItem;

        if (this.help_number_text != null)
            this.help_number_text.text = this.numberOfHelp.ToString();

        if(this.audioEffect == null)
            this.audioEffect = this.GetComponent<AudioSource>();

        this.setHelpTool(false);
    }

    public void setHelpTool(bool status)
    {
        if (this.numberOfHelp <= 0)
        {
            SetUI.SetTarget(this.cg, false, 1f);
            this.controlPopStatus(false);
            this.setBtn(status);
        }
        else
        {
            this.setBtn(status);
            this.controlPopStatus(true);
        }
    }

    public void setBtn(bool status)
    {
        SetUI.SetScale(this.cg, status, this.originalScale, 1f, DG.Tweening.Ease.InOutQuint);
    }

    public void Deduct(Action onCompleted = null) { 
        
        if(this.enabled && this.cg.interactable)
        {
            if (this.numberOfHelp > 0) { 
                this.numberOfHelp -= 1;
                if(this.numberOfHelp <= 0) {
                    SetUI.SetTarget(this.cg, false, 1f);
                    this.controlPopStatus(false);
                }
            }

            if (this.help_number_text != null)
                this.help_number_text.text = this.numberOfHelp.ToString();

            if (this.audioEffect != null)
            {
                this.audioEffect.Play();
            }
            this.setBtn(false);
            onCompleted?.Invoke();
        }
    }

    void controlPopStatus(bool status = false)
    {
        this.grayScaleMat?.SetFloat("_GrayAmount", status? 0f : 1f);
        if (this.popNumBg != null) this.popNumBg.texture = this.popNumBgTextures[status? 0 : 1];
    }
}
