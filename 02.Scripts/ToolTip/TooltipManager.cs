using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance { get; private set; }

    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private TextMeshProUGUI tooltipText2;

    [SerializeField] private RectTransform tooltipRect => tooltipPanel.GetComponent<RectTransform>();

    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(instance);
        }

        if(tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
    private void Update()
    {
        // 툴팁이 활성화되어 있으면 마우스 위치를 따라다니도록 함
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            Vector2 mousePosition = Input.mousePosition;
            tooltipRect.position = mousePosition + offset;
        }
    }

    public void ShowTooltip(string content, string content2)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipText.text = content;
        tooltipText2.text = content2;

        tooltipPanel.SetActive(true);
        // Content Size Fitter가 즉시 적용되도록 강제 업데이트
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
