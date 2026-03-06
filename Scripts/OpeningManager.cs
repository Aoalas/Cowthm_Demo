using UnityEngine;
using UnityEngine.Video;
using Milease.Core.Animator; // 提供 MilInstantAnimator.Start
using Milease.Utils; // 提供真正的 .Milease() 表达式扩展和 .Then()
using Milease.DSL;
using Milease.Core; // For EaseFunction, EaseType etc. if needed
using Milease.Enums; // 确保加载到 EaseFunction 和 EaseType

public class OpeningManager : MonoBehaviour
{
    [Header("视频与背景")] public VideoPlayer videoPlayer;
    public GameObject videoScreen;
    public GameObject staticBg;

    [Header("UI 引用")] public GameObject tapToStartText;
    public CanvasGroup fadeScreen;
    public CanvasGroup textCanvasGroup; // 呼吸文字用

    [Header("音频设置")] public AudioSource openingBGM;
    public float audioSkipTime = 15f;

    [Header("高级呼吸旋转特效")] public float scaleSpeed = 1.0f;
    public float minScale = 1.05f;
    public float maxScale = 1.15f;
    public float rotationSpeed = 0.5f;
    public float maxRotation = 2.0f;

    [Header("交互与音量控制")] public float unskippableDuration = 2.0f;

    [Tooltip("开屏BGM的独立音量倍率 (0.1 ~ 3.0，允许放大3倍)")]
    public float bgmVolumeMultiplier = 1.0f;

    private bool isVideoFinishedOrSkipped = false;
    private bool isTransitioning = false;
    private float openingTimer = 0f;

    private float currentFadeVolume = 1.0f;

    // 依然保留这个方便的属性管理以支持底层声音放大防爆音
    public float CurrentFadeVolume
    {
        get => currentFadeVolume;
        set
        {
            currentFadeVolume = value;
            if (openingBGM != null)
            {
                openingBGM.volume = Mathf.Min(currentFadeVolume, 1.0f);
            }
        }
    }

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoEnd;
            videoPlayer.Play();
        }

        if (openingBGM != null)
        {
            openingBGM.loop = false;
            openingBGM.time = 0f;
            CurrentFadeVolume = bgmVolumeMultiplier;
            openingBGM.Play();
        }

        if (tapToStartText != null) tapToStartText.SetActive(false);
        if (fadeScreen != null)
        {
            fadeScreen.alpha = 0f;
            fadeScreen.gameObject.SetActive(false);
        }

        // 初始化背景图片
        if (staticBg != null)
        {
            staticBg.SetActive(false);
            staticBg.transform.localScale = new Vector3(minScale, minScale, 1f);
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Opening) return;

        openingTimer += Time.deltaTime;

        if (openingBGM != null && openingBGM.clip != null &&
            (!openingBGM.isPlaying || openingBGM.time >= openingBGM.clip.length - 0.05f))
        {
            openingBGM.Stop();
            openingBGM.time = audioSkipTime;
            openingBGM.Play();
        }

        // 【回调旧版】针对非常连续、周期相位相差且无状态改变的周期性背景动画，
        // 最平滑完美的做法依然是基于 Time.time 来计算，这就避免了因为不同动画机 Loop 循环时间卡点造成的“一瞬间切回原角度闪动”。
        // 这个数学 Sin 函数运算性能极高且绝对不会出现状态闪回的问题。
        if (staticBg != null && staticBg.activeSelf)
        {
            float pulse = (Mathf.Sin(Time.time * scaleSpeed) + 1f) / 2f;
            float currentScale = Mathf.Lerp(minScale, maxScale, pulse);
            staticBg.transform.localScale = new Vector3(currentScale, currentScale, 1f);

            float rot = Mathf.Sin(Time.time * rotationSpeed) * maxRotation;
            staticBg.transform.localRotation = Quaternion.Euler(0, 0, rot);
        }

        if (isTransitioning || openingTimer < unskippableDuration) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            if (!isVideoFinishedOrSkipped) TransitionToStaticBg(true);
            else EnterMainMenu();
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        if (!isVideoFinishedOrSkipped && !isTransitioning) TransitionToStaticBg(false);
    }

    private void TransitionToStaticBg(bool isSkipped)
    {
        isTransitioning = true;

        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);

            MilInstantAnimator.Start(
                0.2f / fadeScreen.Milease(x => x.alpha, 0f, 1f)
            ).Play(() =>
            {
                SwitchToStaticBg();

                if (isSkipped && openingBGM != null)
                {
                    openingBGM.time = audioSkipTime;
                    if (!openingBGM.isPlaying) openingBGM.Play();
                }

                ShowTapToStart();

                MilInstantAnimator.Start(
                    0.3f / fadeScreen.Milease(x => x.alpha, 1f, 0f)
                ).Play(() =>
                {
                    fadeScreen.gameObject.SetActive(false);
                    isTransitioning = false;
                });
            });
        }
        else
        {
            SwitchToStaticBg();
            if (isSkipped && openingBGM != null)
            {
                openingBGM.time = audioSkipTime;
                if (!openingBGM.isPlaying) openingBGM.Play();
            }

            ShowTapToStart();
            isTransitioning = false;
        }
    }

    private void EnterMainMenu()
    {
        isTransitioning = true;
        if (tapToStartText != null) tapToStartText.SetActive(false);

        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);

            var animator = MilInstantAnimator.Start(
                1.0f / fadeScreen.Milease(x => x.alpha, fadeScreen.alpha, 1f)
            );

            if (openingBGM != null)
            {
                float startVolume = currentFadeVolume;
                // 使用自定义的 HandleFunction 来混合音量！
                // 完美解决 "[Milease] Accessors is not generated, use Reflection..." 性能警告。
                var volumeAnim = this.Milease((e) => { CurrentFadeVolume = Mathf.Lerp(startVolume, 0f, e.Progress); },
                    null, 1.0f, 0f, EaseFunction.Linear, EaseType.In);

                animator = animator.And(volumeAnim);
            }

            animator.Play(() =>
            {
                if (openingBGM != null) openingBGM.Stop();
                GameManager.Instance.FinishOpening();
            });
        }
        else
        {
            if (openingBGM != null) openingBGM.Stop();
            GameManager.Instance.FinishOpening();
        }
    }

    private void SwitchToStaticBg()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (videoScreen != null) videoScreen.SetActive(false);

        if (staticBg != null)
        {
            staticBg.SetActive(true);
        }
    }

    private void ShowTapToStart()
    {
        isVideoFinishedOrSkipped = true;
        if (tapToStartText != null)
        {
            tapToStartText.SetActive(true);

            if (textCanvasGroup != null)
            {
                textCanvasGroup.alpha = 0f;

                // 改用 Custom HandleFunction 驱动文字呼吸，确保能够无任何警告地运行平滑 SineIO 曲线
                MAni.Make(
                    this.Milease((e) => { textCanvasGroup.alpha = Mathf.Lerp(0f, 1f, e.Progress); }, null, 1f, 0f,
                        EaseFunction.Sine, EaseType.IO)
                ).Then(
                    this.Milease((e) => { textCanvasGroup.alpha = Mathf.Lerp(1f, 0f, e.Progress); }, null, 1f, 0f,
                        EaseFunction.Sine, EaseType.IO)
                ).EnableLooping().Play();
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        // 如果音量 <= 1，不需要底层强行放大，Unity 属性处理得很好
        if (currentFadeVolume <= 1.0f) return;

        // 如果设置了 > 1.0（比如3.0），手动暴力放大每个音频采样点的波形
        float boost = currentFadeVolume;
        for (int i = 0; i < data.Length; i++)
        {
            float val = data[i] * boost;
            // 限制防爆音（Clipping），确保声音不会“炸麦”
            if (val > 1f) val = 1f;
            else if (val < -1f) val = -1f;
            data[i] = val;
        }
    }
}