using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class OpeningManager : MonoBehaviour
{
    [Header("视频与背景")]
    public VideoPlayer videoPlayer;
    public GameObject videoScreen; 
    public GameObject staticBg;    

    [Header("UI 引用")]
    public GameObject tapToStartText; 
    public CanvasGroup textCanvasGroup; 
    
    [Header("转场黑幕")]
    public CanvasGroup fadeScreen; 

    [Header("音频与跳跃设置")]
    public AudioSource openingBGM; 
    public float audioSkipTime = 15f; 
    public float enterMenuFadeDuration = 1.0f;

    [Header("高级呼吸旋转特效")]
    public float scaleSpeed = 1.0f;     
    public float minScale = 1.05f;      
    public float maxScale = 1.15f;      
    public float rotationSpeed = 0.5f;  
    public float maxRotation = 2.0f;    

    [Header("交互与音量控制")]
    public float unskippableDuration = 2.0f; 
    [Tooltip("开屏BGM的独立音量倍率 (0.1 ~ 3.0，允许放大3倍)")]
    public float bgmVolumeMultiplier = 1.0f; 

    private bool isVideoFinishedOrSkipped = false;
    private bool isTransitioning = false; 
    private float breatheTimer = 0f;
    private bool hasStartedBGM = false;
    
    private float openingTimer = 0f; 
    private Vector3 initialBgPos;
    private bool hasInitBgPos = false;

    // 【新增】用于处理超过1.0放大的实时混合音量
    private float currentFadeVolume = 1.0f; 

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
            
            currentFadeVolume = bgmVolumeMultiplier;
            // 如果音量倍率>1，我们让自带 volume 满载为1，多余的部分交给底层放大器
            openingBGM.volume = Mathf.Min(currentFadeVolume, 1.0f);  
            
            openingBGM.Play();
            hasStartedBGM = true;
        }
        
        if (tapToStartText != null) tapToStartText.SetActive(false);
        if (staticBg != null) 
        {
            staticBg.SetActive(false);
            staticBg.transform.localScale = new Vector3(minScale, minScale, 1f);
        }
        if (fadeScreen != null) 
        {
            fadeScreen.alpha = 0f;
            fadeScreen.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Opening) return;

        openingTimer += Time.deltaTime; 

        if (hasStartedBGM && openingBGM != null && openingBGM.clip != null)
        {
            if (!openingBGM.isPlaying || openingBGM.time >= openingBGM.clip.length - 0.05f)
            {
                openingBGM.Stop();
                openingBGM.time = audioSkipTime; 
                openingBGM.Play();               
            }
        }

        if (staticBg != null && staticBg.activeSelf)
        {
            float pulse = (Mathf.Sin(Time.time * scaleSpeed) + 1f) / 2f; 
            float currentScale = Mathf.Lerp(minScale, maxScale, pulse);
            staticBg.transform.localScale = new Vector3(currentScale, currentScale, 1f);

            float rot = Mathf.Sin(Time.time * rotationSpeed) * maxRotation;
            staticBg.transform.localRotation = Quaternion.Euler(0, 0, rot);
        }

        if (isVideoFinishedOrSkipped && textCanvasGroup != null)
        {
            breatheTimer += Time.deltaTime * 3f;
            textCanvasGroup.alpha = 0.5f + Mathf.Sin(breatheTimer) * 0.5f; 
        }

        if (isTransitioning || openingTimer < unskippableDuration) return;

        bool isTapped = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (isTapped)
        {
            if (!isVideoFinishedOrSkipped) StartCoroutine(TransitionToStaticBgRoutine(true)); 
            else StartCoroutine(EnterMainMenuRoutine()); 
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        if (!isVideoFinishedOrSkipped && !isTransitioning) StartCoroutine(TransitionToStaticBgRoutine(false));
    }

    private IEnumerator TransitionToStaticBgRoutine(bool isSkipped)
    {
        isTransitioning = true;
        
        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);
            float timer = 0;
            while (timer < 0.2f) { timer += Time.deltaTime; fadeScreen.alpha = timer / 0.2f; yield return null; }
            fadeScreen.alpha = 1f;
        }

        SwitchToStaticBg();

        if (isSkipped && openingBGM != null)
        {
            openingBGM.time = audioSkipTime; 
            if (!openingBGM.isPlaying) openingBGM.Play();
        }

        if (fadeScreen != null)
        {
            float timer = 0;
            while (timer < 0.3f) { timer += Time.deltaTime; fadeScreen.alpha = 1f - (timer / 0.3f); yield return null; }
            fadeScreen.alpha = 0f;
            fadeScreen.gameObject.SetActive(false);
        }

        ShowTapToStart();
        isTransitioning = false;
    }

    private IEnumerator EnterMainMenuRoutine()
    {
        isTransitioning = true;
        if (tapToStartText != null) tapToStartText.SetActive(false);

        float startVolume = currentFadeVolume;

        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);
            float timer = 0;
            while (timer < enterMenuFadeDuration) 
            { 
                timer += Time.deltaTime; 
                float progress = timer / enterMenuFadeDuration;
                fadeScreen.alpha = progress + 0.3f;
                
                if (openingBGM != null) 
                {
                    currentFadeVolume = Mathf.Lerp(startVolume, 0f, progress);
                    openingBGM.volume = Mathf.Min(currentFadeVolume, 1.0f);
                }
                
                yield return null; 
            }
            fadeScreen.alpha = 1f;
        }

        if (openingBGM != null) 
        {
            openingBGM.Stop();
            currentFadeVolume = bgmVolumeMultiplier; 
        }

        GameManager.Instance.FinishOpening();
    }

    private void SwitchToStaticBg()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (videoScreen != null) videoScreen.SetActive(false);
        
        if (staticBg != null) 
        {
            if (!hasInitBgPos)
            {
                initialBgPos = staticBg.transform.localPosition;
                hasInitBgPos = true;
            }
            staticBg.SetActive(true);
        }
    }

    private void ShowTapToStart()
    {
        isVideoFinishedOrSkipped = true;
        if (tapToStartText != null) tapToStartText.SetActive(true);
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