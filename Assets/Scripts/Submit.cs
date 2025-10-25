using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Submit : MonoBehaviour
{
    public RawImage SubmitItemImage;
    public TextMeshProUGUI SubmitText;
    public Image dropShadowImage;

    private void Start()
    {
        this.clear();
    }

    public void setContent(Texture itemImage = null, string content="")
    {
        if(this.SubmitItemImage != null && this.SubmitText != null)
        {
            if(!string.IsNullOrEmpty(content))
            {
                AudioController.Instance?.PlayAudio(3, false);
                this.SubmitText.text = content;
                this.SubmitItemImage.texture = itemImage;
                this.dropShadowImage.enabled = true;
                this.SubmitItemImage.DOFade(1f, 0.5f);
            }
            else
            {
                this.SubmitText.text = "";
                this.SubmitItemImage.DOFade(0f, 0f).OnComplete(() =>
                {
                    this.SubmitItemImage.texture = null;
                    this.dropShadowImage.enabled = false;
                });
            }
        }
    }

    public bool IsContainContent
    {
        get{
            return this.SubmitItemImage.texture != null && !string.IsNullOrEmpty(this.SubmitText.text);
        }
    }

    public void clear()
    {
        this.setContent(null, "");
    }
}
