using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Milease.Core.Animator;
using Milease.Utils;
using Milease.DSL;

public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public TextMeshProUGUI buttonText;
    public Image leftIndicator;
    public UnityEngine.Events.UnityEvent onClick;

    private RectTransform rectTransform;
    private Image bgImage;
    private MilInstantAnimator currentAnim;
    private bool isClicked = false;
    private Vector2 originalTextPos;
    private bool hasInitVisuals = false; // 新增初始化标记以解决位置恰好为(0,0)导致的短路Bug

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        bgImage = GetComponent<Image>();
    }

    private void Start()
    {
        if (buttonText != null)
        {
            originalTextPos = buttonText.rectTransform.anchoredPosition;
        }
        hasInitVisuals = true; 
        ResetVisuals();
    }

    private void OnEnable()
    {
        isClicked = false;
        
        // 强制重置。修复原因：之前用 originalTextPos!=Vector2.zero 判定，
        // 若其他按钮（如设置/感谢）文本没有X轴偏移，坐标恰好等于(0,0)，就会被错误跳过重置！
        if (Application.isPlaying && hasInitVisuals) 
        {
            ResetVisuals();
        }
    }

    private void ResetVisuals()
    {
        if (currentAnim != null) currentAnim.Stop();

        if (buttonText != null)
        {
            buttonText.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            buttonText.rectTransform.anchoredPosition = originalTextPos;
        }

        if (bgImage != null)
        {
            bgImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        }
        
        if (leftIndicator != null)
        {
            leftIndicator.rectTransform.sizeDelta = new Vector2(6f, 120f); 
            leftIndicator.color = new Color(1f, 1f, 1f, 0f);
        }

        rectTransform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isClicked) return;

        if (currentAnim != null) 
            currentAnim.Pause();

        currentAnim = MAni.Make(
            0.15f / buttonText.MSineOut(x => x.color, new Color(1f, 1f, 1f, 1f).ToThis()),
            0.2f / buttonText.rectTransform.MSineOut(x => x.anchoredPosition, (originalTextPos + new Vector2(2f, 0f)).ToThis()),
            0.2f / leftIndicator.rectTransform.MSineOut(x => x.sizeDelta, new Vector2(360f, 120f).ToThis()),
            0.2f / leftIndicator.MSineOut(x => x.color, new Color(0.1f, 0.15f, 0.5f, 0.85f).ToThis()),
            0.2f / rectTransform.MSineOut(x => x.localScale, new Vector3(1.05f, 1.05f, 1f).ToThis())
        );

        if (bgImage != null)
        {
            currentAnim = currentAnim.And(
                0.2f / bgImage.MSineOut(x => x.color, new Color(0.8f, 0.9f, 0.9f, 0.8f).ToThis())
            );
        }

        currentAnim.Play();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isClicked) return;
        
        if (currentAnim != null) 
            currentAnim.Pause();

        currentAnim = MAni.Make(
            0.2f / buttonText.MSineOut(x => x.color, new Color(0.05f, 0.05f, 0.05f, 0.95f).ToThis()),
            0.2f / buttonText.rectTransform.MSineOut(x => x.anchoredPosition, originalTextPos.ToThis()),
            0.2f / leftIndicator.rectTransform.MSineOut(x => x.sizeDelta, new Vector2(6f, 120f).ToThis()),
            0.2f / leftIndicator.MSineOut(x => x.color, new Color(1f, 1f, 1f, 0f).ToThis()),
            0.4f / rectTransform.MSineOut(x => x.localScale, Vector3.one.ToThis())
        );

        if (bgImage != null)
        {
            currentAnim = currentAnim.And(
                0.2f / bgImage.MSineOut(x => x.color, new Color(0.95f, 0.95f, 0.95f, 1f).ToThis())
            );
        }

        currentAnim.Play();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isClicked) return;
        isClicked = true;

        if (currentAnim != null) 
            currentAnim.Pause();
        
        currentAnim = MAni.Make(

            0.05f / buttonText.rectTransform.MSineOut(x => x.anchoredPosition, (originalTextPos + new Vector2(15f, 0f)).ToThis()),
            0.05f / leftIndicator.rectTransform.MSineOut(x => x.sizeDelta, new Vector2(360f, 120f).ToThis()),
            0.1f / rectTransform.MLinear(x => x.localScale, new Vector3(0.95f, 0.95f, 1f).ToThis()),
            0.1f / leftIndicator.MSineOut(x => x.color, new Color(1f, 1f, 1f, 0.3f).ToThis()),
            0.1f / buttonText.rectTransform.MSineOut(x => x.anchoredPosition, (originalTextPos + new Vector2(1.2f, 0f)).ToThis())
        ).Then(
            0.3f / rectTransform.MBounceOut(x => x.localScale, new Vector3(1.05f, 1.05f, 1f).ToThis()),
            0.3f / leftIndicator.MSineOut(x => x.color, new Color(0.1f, 0.3f, 0.6f, 0.2f).ToThis())
        );
        
        currentAnim.Play(() => 
        {
            onClick?.Invoke();
        });
    }
}
