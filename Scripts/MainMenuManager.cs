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
    public MainMenuButton[] menuButtons;
    public CanvasGroup sideIllustration; // 新增：右侧滑入的立绘插图

    // 保存初始位置
    private Vector2[] originalBtnPos;
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
        
        bool isFromOpening = GameManager.JustFinishedOpening;
        
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
            {
                var cg = menuButtons[i].GetComponent<CanvasGroup>();
                cg.alpha = isFromOpening ? 0.3f : 1f;
                float startX = isFromOpening ? originalBtnPos[i].x - 1200f : originalBtnPos[i].x;
                menuButtons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(startX, originalBtnPos[i].y);
            }
        }

        if (sideIllustration != null)
        {
            sideIllustration.alpha = isFromOpening ? 0.3f : 1f;
            float startX = isFromOpening ? originalIllusPos.x + 650f : originalIllusPos.x;
            sideIllustration.GetComponent<RectTransform>().anchoredPosition = new Vector2(startX, originalIllusPos.y);
        }

        if (isFromOpening)
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
        
        StartCoroutine(PlayEntranceSequence(isFromOpening));
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

    private IEnumerator PlayEntranceSequence(bool isFromOpening)
    {
        if (isFromOpening && bgBlurPanel != null)
        {
            (0.01f / bgBlurPanel.MSineOut(x => x.alpha, 1f.ToThis())).Play();
            yield return new WaitForSeconds(0.3f);
        }
        
        if (sideIllustration != null)
        {
            var animRect = sideIllustration.GetComponent<RectTransform>();
            if (isFromOpening)
            {
                MAni.Make(
                    0.8f / sideIllustration.MSineOut(x => x.alpha, 1f.ToThis()),
                    0.4f / animRect.MSineOut(x => x.anchoredPosition, originalIllusPos.ToThis()) // 从右侧弹入
                ).Play(() => {
                    illusFloatingAnim = MAni.Make(
                        2.0f / animRect.MSineIO(x => x.anchoredPosition, (originalIllusPos + new Vector2(0f, -15f)).ToThis())
                    ).Then(
                        2.0f / animRect.MSineIO(x => x.anchoredPosition, originalIllusPos.ToThis())
                    ).EnableLooping();
                    illusFloatingAnim.Play();
                });
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                illusFloatingAnim = MAni.Make(
                    2.0f / animRect.MSineIO(x => x.anchoredPosition, (originalIllusPos + new Vector2(0f, -15f)).ToThis())
                ).Then(
                    2.0f / animRect.MSineIO(x => x.anchoredPosition, originalIllusPos.ToThis())
                ).EnableLooping();
                illusFloatingAnim.Play();
            }
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

                if (isFromOpening)
                {
                    MAni.Make(
                        0.3f / cg.MSineOut(x => x.alpha, 1f.ToThis()),
                        0.6f / rect.MBackOut(x => x.anchoredPosition, targetPos.ToThis()) // 滑入
                    ).Play(() => {
                        floatingAnims[index] = MAni.Make(
                            1.5f / rect.MSineIO(x => x.anchoredPosition, (targetPos + new Vector2(8f, 0f)).ToThis())
                        ).Then(
                            1.5f / rect.MSineIO(x => x.anchoredPosition, targetPos.ToThis())
                        ).EnableLooping();
                        floatingAnims[index].Play();
                    });
                    yield return new WaitForSeconds(0.12f);
                }
                else
                {
                    floatingAnims[index] = MAni.Make(
                        1.5f / rect.MSineIO(x => x.anchoredPosition, (targetPos + new Vector2(8f, 0f)).ToThis())
                    ).Then(
                        1.5f / rect.MSineIO(x => x.anchoredPosition, targetPos.ToThis())
                    ).EnableLooping();
                    floatingAnims[index].Play();
                }
            }
        }
    }
}
