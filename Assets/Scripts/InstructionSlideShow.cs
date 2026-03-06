using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InstructionSlideShow : MonoBehaviour
{
    public static InstructionSlideShow Instance = null;
    public CanvasGroup instructionPopupCg, howtoPlayBtn, block;
    public CanvasGroup[] slides;
    public int slideIndex = 0;
    public Vector3 originalPos;

    private void Awake()
    {
        if(Instance == null) 
            Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        this.controlSlide(0);
    }

    public void ShowInstructionPopup(bool status)
    {
        this.showInstructionPopupAnimation(status);

        if (!status)
        {
            this.controlSlide(0);
        }

        this.ShowHowToPlayBtn(!status);
    }

    void showInstructionPopupAnimation(bool status)
    {
        if (!status) SetUI.Set(this.block, false);
        SetUI.SetScale(instructionPopupCg, status, 1f, 0.5f);
        this.instructionPopupCg.GetComponent<RectTransform>().DOLocalMove(status ? Vector3.zero : this.originalPos, 0.5f).OnComplete(()=>
        {
            if(status) SetUI.Set(this.block, true);
        });
    }

    public void ShowHowToPlayBtn(bool status)
    {
        SetUI.Set(this.howtoPlayBtn, status);
    }

    public void controlSlide(int i=0)
    {
        this.slideIndex = i;
        SetUI.SetGroup(this.slides, this.slideIndex);
    }
}
