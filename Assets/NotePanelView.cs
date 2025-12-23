using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class NotePanelView : MonoBehaviour
{
    [Header("Wired in Inspector")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    private CanvasGroup _cg;

    public CanvasGroup CanvasGroup => _cg ? _cg : (_cg = GetComponent<CanvasGroup>());

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
    }

    public void SetContent(string title, string body)
    {
        if (titleText) titleText.text = title ?? "";
        if (bodyText) bodyText.text = body ?? "";
    }

    public void SetAlpha(float a)
    {
        CanvasGroup.alpha = Mathf.Clamp01(a);
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
    }
}
