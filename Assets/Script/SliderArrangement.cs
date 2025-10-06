using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderArrangement : MonoBehaviour
{
    [System.Serializable]
    public class SliderImagePair
    {
        public UnityEngine.UI.Slider slider;
        public UnityEngine.UI.Image image;
    }

    public List<SliderImagePair> sliderImages = new List<SliderImagePair>();
    public float spacing = 10f; // 每個 image 間距
    public float minX = 0f;     // 最左邊起始座標

    void Update()
    {
        float currentX = minX;
        for (int i = 0; i < sliderImages.Count; i++)
        {
            var pair = sliderImages[i];
            if (pair.image == null) continue;
            var rt = pair.image.rectTransform;
            float width = rt.rect.width;
            if (i == 0)
            {
                // 第一個 bar 固定 minX 置左
                rt.anchoredPosition = new Vector2(minX, rt.anchoredPosition.y);
                currentX = minX + width;
            }
            else
            {
                // 後續 bar 根據前一個 bar 的右緣加 spacing
                rt.anchoredPosition = new Vector2(currentX + spacing, rt.anchoredPosition.y);
                currentX += spacing + width;
            }
        }
    }
}
