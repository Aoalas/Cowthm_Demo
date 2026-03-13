using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Milease.DSL;
using Milease.Core.Animator;

public class SongSelectItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("结构组件 (务必手动拖拽赋值)")]
    [Tooltip("卡片内容的统一缩放/平移层，也就是装文本和底图的父节点，别填成 Root节点")]
    public RectTransform contentContainer; 
    
    [Tooltip("选中时的高亮底图/渐变背景，透明度默认为 0")]
    public CanvasGroup highlightBg;

    [Header("文本组件 (务必手动拖拽赋值)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;
    public TextMeshProUGUI difficultyText;

    private Button button;
    private CanvasGroup rootCanvasGroup;

    private int myIndex;
    private SongSelectManager manager;
    private bool isSelected = false;

    private MilInstantAnimator currentAnim;

    void Awake()
    {
        button = GetComponent<Button>();
        rootCanvasGroup = GetComponent<CanvasGroup>();
        if (rootCanvasGroup == null) rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(int index, SongSelectManager mgr, string title, string artist, float difficulty)
    {
        myIndex = index;
        manager = mgr;

        if (titleText != null) titleText.text = title;
        if (artistText != null) artistText.text = artist;
        if (difficultyText != null) 
        {
            if (difficulty <= -999f)
            {
                difficultyText.text = "?";
            }
            else if (difficulty < 0)
            {
                difficultyText.text = ((int)difficulty).ToString();
            }
            else
            {
                int baseLevel = Mathf.FloorToInt(difficulty);
                float decimalPart = difficulty - baseLevel;
                string displayStr = baseLevel.ToString();
                if (decimalPart >= 0.7f) displayStr += "+";
                
                difficultyText.text = displayStr;
            }
        }
        else {
            difficultyText.text = "--";
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => manager.OnSongItemClicked(myIndex));
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        PlayStateAnimation(selected ? "Selected" : "Normal");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected) PlayStateAnimation("Hover");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected) PlayStateAnimation("Normal");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayStateAnimation("Click");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isSelected) PlayStateAnimation("Hover");
        else PlayStateAnimation("Selected");
    }

    private void PlayStateAnimation(string state)
    {
        if (currentAnim != null) currentAnim.Stop();
        if (contentContainer == null) return; // 保护：如果没有配置容器就不播动画

        float duration = 0.2f;

        switch (state)
        {
            case "Normal":
                currentAnim = MAni.Make(
                    duration / contentContainer.MSineOut(x => x.anchoredPosition, new Vector2(0f, contentContainer.anchoredPosition.y).ToThis()),
                    duration / contentContainer.MSineOut(x => x.localScale, Vector3.one.ToThis()),
                    duration / rootCanvasGroup.MSineOut(x => x.alpha, 0.6f.ToThis()), // 未选中时整体变暗避开焦点
                    (highlightBg != null ? duration / highlightBg.MSineOut(x => x.alpha, 0f.ToThis()) : null)
                );
                break;
            case "Hover":
                currentAnim = MAni.Make(
                    duration / contentContainer.MSineOut(x => x.anchoredPosition, new Vector2(15f, contentContainer.anchoredPosition.y).ToThis()), // 悬停内容略微右移
                    duration / contentContainer.MSineOut(x => x.localScale, new Vector3(1.02f, 1.02f, 1f).ToThis()),
                    duration / rootCanvasGroup.MSineOut(x => x.alpha, 0.8f.ToThis()),
                    (highlightBg != null ? duration / highlightBg.MSineOut(x => x.alpha, 0.3f.ToThis()) : null)
                );
                break;
            case "Selected":
                currentAnim = MAni.Make(
                    0.3f / contentContainer.MBackOut(x => x.anchoredPosition, new Vector2(40f, contentContainer.anchoredPosition.y).ToThis()), // 选中内容大幅右移凸出
                    0.3f / contentContainer.MBackOut(x => x.localScale, new Vector3(1.05f, 1.05f, 1f).ToThis()),
                    0.3f / rootCanvasGroup.MSineOut(x => x.alpha, 1f.ToThis()), // 完全点亮
                    (highlightBg != null ? 0.3f / highlightBg.MSineOut(x => x.alpha, 1f.ToThis()) : null) // 紫色/主题色高亮条充满
                );
                break;
            case "Click":
                currentAnim = MAni.Make(
                    0.1f / contentContainer.MSineOut(x => x.localScale, new Vector3(0.95f, 0.95f, 1f).ToThis())
                );
                break;
        }

        currentAnim.Play();
    }
}
