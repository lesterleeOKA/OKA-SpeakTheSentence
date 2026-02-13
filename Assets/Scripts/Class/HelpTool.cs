using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class HelpTool : MonoBehaviour
{
    public bool loginPlayer = false;
    public bool displayNum = true;
    public int numberOfHelp = 0;
    public CanvasGroup cg;
    public RawImage popNumBg;
    public Texture[] popNumBgTextures;
    public TextMeshProUGUI help_number_text;
    public TextMeshProUGUI help_tool_name;
    private AudioSource audioEffect = null;
    public Material grayScaleMat;
    public HelpToolInventory currentInventory = null;

    private void Start()
    {
        if (LoaderConfig.Instance == null)
            return;

        this.numberOfHelp = 5;
        this.audioEffect ??= this.GetComponent<AudioSource>();

        var apiManager = LoaderConfig.Instance.apiManager;
        bool isLoginedStarwishPartySite = APIConstant.isLoginedStarwishPartySite(LoaderConfig.Instance);

        if (isLoginedStarwishPartySite)
        {
            SetUI.Set(this.popNumBg.GetComponent<CanvasGroup>(), true);

            if (this.loginPlayer)
            {
                StartCoroutine(apiManager.getHelpToolInventory(() =>
                {
                    this.UpdateInventoryAndUI();
                }));
            }
            else
            {
                this.SetHelpNumberAndUI(0);
            }
        }
        else
        {
            this.SetHelpNumberAndUI(this.numberOfHelp);
            if (!this.displayNum)
                SetUI.Set(this.popNumBg.GetComponent<CanvasGroup>(), false);

            this.setHelpTool(false);
        }
    }

    private void UpdateInventoryAndUI()
    {
        var inventoryData = LoaderConfig.Instance.gameSetup.inventory.data;
        this.numberOfHelp = 0;
        foreach (var item in inventoryData)
        {
            if (item.help_tool_id == LoaderConfig.Instance.gameSetup.helpItemTypeOfId)
            {
                this.currentInventory = item;
                this.numberOfHelp = item.amount;
                if (this.help_tool_name != null)
                    this.help_tool_name.text = item.help_tool_name;
                break;
            }
        }
        SetHelpNumberAndUI(this.numberOfHelp);
    }

    private void SetHelpNumberAndUI(int helpNum)
    {
        this.numberOfHelp = helpNum;
        if (this.help_number_text != null)
            this.help_number_text.text = helpNum.ToString();
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

    public void DisableTool()
    {
        this.setBtn(true);
        this.controlPopStatus(false);
        SetUI.SetTarget(this.cg, false, 1f);
    }

    public void setBtn(bool status)
    {
        SetUI.SetScale(this.cg, status, 1f, 1f, DG.Tweening.Ease.InOutQuint);
    }

    public void Deduct(Action onCompleted = null)
    {

        if (this.enabled && this.cg.interactable)
        {
            if (this.numberOfHelp > 0)
            {
                this.numberOfHelp -= 1;
                if (this.numberOfHelp <= 0)
                {
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

            var loader = LoaderConfig.Instance;
            if (loader != null &&
                this.currentInventory != null &&
                this.currentInventory.amount > 0 &&
                APIConstant.isLoginedStarwishPartySite(loader))
            {
                StartCoroutine(loader.apiManager.useHelpTool(
                    this.currentInventory.help_tool_id)
                    );
            }

            onCompleted?.Invoke();
        }
    }

    void controlPopStatus(bool status = false)
    {
        this.grayScaleMat?.SetFloat("_GrayAmount", status ? 0f : 1f);
        if (this.popNumBg != null) this.popNumBg.texture = this.popNumBgTextures[status ? 0 : 1];
    }
}
