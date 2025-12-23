using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class textOutline : MonoBehaviour
{

    public Color32 outlineColor = new Color32(255, 128, 0, 255);
    public float outlineWidth = 0.2f;
    // Start is called before the first frame update
    void Start()
    {
        Material mat = this.GetComponent<TextMeshProUGUI>().fontMaterial;

        mat.EnableKeyword("OUTLINE_ON");

        mat.SetFloat("_OutlineWidth", outlineWidth); 
        mat.SetColor("_OutlineColor", outlineColor);

        Debug.Log("textOutline: "+ outlineColor + " - " + mat.GetColor("_OutlineColor"));

        this.GetComponent<TextMeshProUGUI>().UpdateMeshPadding();

    }
}
