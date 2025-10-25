using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LanguageController : MonoBehaviour
{
    public enum ForComponent
    {
        None,
        Text,
        Image,
        RawImage,
        Object,
    }

    public ForComponent forComponent = ForComponent.None;
    public Image image;
    public RawImage rawImage;
    public Texture[] langTextures;
    public TextMeshProUGUI text;
    public string[] lang_content;
    public GameObject[] langObjects;

    // Start is called before the first frame update
    void Start()
    {
        this.setContent(LoaderConfig.Instance.gameSetup.lang);
    }

    void setContent(int langId)
    {
        switch (this.forComponent)
        {
            case ForComponent.Text:
                if (this.text != null)
                {
                    this.text.text = this.lang_content[langId];
                }
                break;

            case ForComponent.Image:
                if (this.image != null)
                {
                    this.image.sprite = SetUI.ConvertTextureToSprite((Texture2D)langTextures[langId]);
                }
                break;

            case ForComponent.RawImage:
                if (this.rawImage != null)
                {
                    this.rawImage.texture = langTextures[langId];
                }
                break;

            case ForComponent.Object:
                SetUI.SetObject(this.langObjects, langId);
                break;
        }
    }
}