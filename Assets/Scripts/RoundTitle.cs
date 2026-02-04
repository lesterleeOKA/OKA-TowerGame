using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RoundTitle : MonoBehaviour
{
    public static RoundTitle Instance = null;
    public CanvasGroup cg;
    public RawImage roundTex;
    public Texture[] roundTitleTextures;
    public int roundIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        if(this.cg == null)
        {
            this.cg = this.GetComponent<CanvasGroup>();
        }
    }

    public void ShowRoundTitle(int round)
    {
        this.roundIndex = Mathf.Clamp(round, 0, this.roundTitleTextures.Length - 1);
        this.roundTex.texture = this.roundTitleTextures[this.roundIndex];
        SetUI.Set(this.cg, true, 0.5f);
        StartCoroutine(HideRoundTitleAfterDelay(2f));
    }

    IEnumerator HideRoundTitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetUI.Set(this.cg, false, 0.5f);
    }
}
