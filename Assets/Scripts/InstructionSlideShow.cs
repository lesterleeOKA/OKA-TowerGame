using UnityEngine;

public class InstructionSlideShow : MonoBehaviour
{
    public static InstructionSlideShow Instance = null;
    public CanvasGroup instructionPopupCg, howtoPlayBtn;
    public CanvasGroup[] slides;
    public bool isInitialized = false;
    public int slideIndex = 0;

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
        SetUI.Set(instructionPopupCg, status);
        if (!status)
        {
            this.controlSlide(0);
        }

        this.ShowHowToPlayBtn(!status);
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
