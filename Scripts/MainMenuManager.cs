using UnityEngine;
using System.Collections;
using Milease.Core.Animator;
using Milease.Utils;
using Milease.DSL;
using Milease.Enums;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI 动画组件引用")]
    public CanvasGroup bgBlurPanel;
    public CanvasGroup topBar;
    public MainMenuButton[] menuButtons;
    public CanvasGroup sideIllustration; // 新增：右侧滑入的立绘插图

    // 保存初始位置
    private Vector2[] originalBtnPos;
    private Vector2 originalTopBarPos;
    private Vector2 originalIllusPos;    // 新增：立绘初始位置

    void Awake()
    {
        if (menuButtons != null && menuButtons.Length > 0)
        {
            originalBtnPos = new Vector2[menuButtons.Length];
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i] != null)
                {
                    originalBtnPos[i] = menuButtons[i].GetComponent<RectTransform>().anchoredPosition;

                    if (menuButtons[i].GetComponent<CanvasGroup>() == null)
                        menuButtons[i].gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        if (topBar != null)
        {
            originalTopBarPos = topBar.GetComponent<RectTransform>().anchoredPosition;
        }

        if (sideIllustration != null)
        {
            originalIllusPos = sideIllustration.GetComponent<RectTransform>().anchoredPosition;
        }
    }

    private MilInstantAnimator[] floatingAnims;
    private MilInstantAnimator illusFloatingAnim; // 新增：立绘的浮动动画引用
    
    void OnEnable()
    {
        StopFloatingAnims();
        
        if (bgBlurPanel != null) bgBlurPanel.alpha = 1f;
        
        if (topBar != null) 
        {
            topBar.alpha = 0f;
            // 向上偏移 50 像素
            topBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(originalTopBarPos.x, originalTopBarPos.y + 50f);
        }
        
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
            {
                var cg = menuButtons[i].GetComponent<CanvasGroup>();
                cg.alpha = 0.3f;
                // 改为屏幕最外侧很远的距离，比如向左偏移极大，实现完全从外部滑入 (-1000f)
                menuButtons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(originalBtnPos[i].x - 1200f, originalBtnPos[i].y);
            }
        }

        if (sideIllustration != null)
        {
            sideIllustration.alpha = 0.3f;
            sideIllustration.GetComponent<RectTransform>().anchoredPosition = new Vector2(originalIllusPos.x + 650f, originalIllusPos.y);
        }

        if (GameManager.JustFinishedOpening)
        {
            GameManager.JustFinishedOpening = false;
            
            GameObject tempBlackObj = new GameObject("TempBlackScreen");
            
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            tempBlackObj.transform.SetParent(parentCanvas != null ? parentCanvas.transform : this.transform, false);
            tempBlackObj.transform.SetAsLastSibling(); 
            
            var rect = tempBlackObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            var img = tempBlackObj.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.black;
            var cg = tempBlackObj.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            
            StartCoroutine(FadeOutBlackScreen(cg, tempBlackObj));
        }
        
        StartCoroutine(PlayEntranceSequence());
    }

    private void StopFloatingAnims()
    {
        if (floatingAnims != null)
        {
            foreach (var anim in floatingAnims)
            {
                if (anim != null) anim.Stop();
            }
        }
        if (illusFloatingAnim != null)
        {
            illusFloatingAnim.Stop();
        }
    }

    private void OnDisable()
    {
        StopFloatingAnims();
    }

    private IEnumerator FadeOutBlackScreen(CanvasGroup cg, GameObject obj)
    {
        yield return new WaitForSeconds(0.2f);
        float dur = 1.3f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, t / dur);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private IEnumerator PlayEntranceSequence()
    {
        if (bgBlurPanel != null)
        {
            (0.01f / bgBlurPanel.MSineOut(x => x.alpha, 1f.ToThis())).Play();
            yield return new WaitForSeconds(0.3f);
        }
        
        if (topBar != null)
        {
            var topRect = topBar.GetComponent<RectTransform>();
            MAni.Make(
                0.4f / topBar.MSineOut(x => x.alpha, 1f.ToThis()),
                0.4f / topRect.MBackOut(x => x.anchoredPosition, originalTopBarPos.ToThis()) // MBackOut 带有弹性
            ).Play();
        }
        
        if (sideIllustration != null)
        {
            var animRect = sideIllustration.GetComponent<RectTransform>();
            MAni.Make(
                0.8f / sideIllustration.MSineOut(x => x.alpha, 1f.ToThis()),
                0.4f / animRect.MSineOut(x => x.anchoredPosition, originalIllusPos.ToThis()) // 从右侧弹入
            ).Play(() => {
                // 滑入完毕后，增加轻微的上下漂浮呼吸效果
                illusFloatingAnim = MAni.Make(
                    2.0f / animRect.MSineIO(x => x.anchoredPosition, (originalIllusPos + new Vector2(0f, -15f)).ToThis())
                ).Then(
                    2.0f / animRect.MSineIO(x => x.anchoredPosition, originalIllusPos.ToThis())
                ).EnableLooping();
                illusFloatingAnim.Play();
            });
            yield return new WaitForSeconds(0.2f);
        }
        
        if (floatingAnims == null || floatingAnims.Length != menuButtons.Length) 
            floatingAnims = new MilInstantAnimator[menuButtons.Length];

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
            {
                var cg = menuButtons[i].GetComponent<CanvasGroup>();
                var rect = menuButtons[i].GetComponent<RectTransform>();
                var targetPos = originalBtnPos[i];
                var index = i; // 捕获循环变量

                MAni.Make(
                    0.3f / cg.MSineOut(x => x.alpha, 1f.ToThis()),
                    0.6f / rect.MBackOut(x => x.anchoredPosition, targetPos.ToThis()) // 滑入
                ).Play(() => {
                    // 入场到位后，开始微妙的周期飘动动画
                    floatingAnims[index] = MAni.Make(
                        1.5f / rect.MSineIO(x => x.anchoredPosition, (targetPos + new Vector2(8f, 0f)).ToThis())
                    ).Then(
                        1.5f / rect.MSineIO(x => x.anchoredPosition, targetPos.ToThis())
                    ).EnableLooping();
                    
                    floatingAnims[index].Play();
                });
                
                yield return new WaitForSeconds(0.12f);
            }
        }
    }
}
