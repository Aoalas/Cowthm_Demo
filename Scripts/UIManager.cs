using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Milease.DSL;
using System.Collections;
using Milease.Core.Animator;

public class UIManager : MonoBehaviour
{
    [Header("UI 面板 (Panels)")]
    public GameObject openingPanel;
    public GameObject topInfoBar;       
    public GameObject mainMenuPanel;    
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject songSelectPanel;  
    public GameObject gameplayPanel;    
    public GameObject pausePanel;       
    public GameObject resultPanel;      

    [Header("顶部信息栏引用")]
    public TextMeshProUGUI playerNameText;

    [Header("设置面板 UI (Sliders & Inputs)")]
    public Slider volumeSlider;
    public TMP_InputField volumeInput;
    public Slider audioOffsetSlider;
    public TMP_InputField audioOffsetInput;
    public Slider visualOffsetSlider;
    public TMP_InputField visualOffsetInput;
    public TMP_Dropdown pauseModeDropdown;
    public Slider scrollSpeedSlider;
    public TMP_InputField scrollSpeedInput;


    [Header("游玩 UI 文本组件引用")]
    public TextMeshProUGUI nameText; 
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboLabelText; 
    public TextMeshProUGUI comboNumberText; 
    public TextMeshProUGUI completeText; 

    [Header("结算面板 UI")]
    public CanvasGroup resultPanelCanvasGroup; 
    public TextMeshProUGUI resultSongNameText;
    public TextMeshProUGUI resultArtistText;
    public UnityEngine.UI.Image resultCoverImage; 
    public TextMeshProUGUI resultScoreText;
    public TextMeshProUGUI resultPerfectText;
    public TextMeshProUGUI resultGoodText;
    public TextMeshProUGUI resultMissText;
    public TextMeshProUGUI resultMaxComboText;
    
    private float displayScore = 0f;
    private float targetScore = 0f;
    private Coroutine missCoroutine; 
    private ChartUIConfig currentChartUIConfig;
    private GameState lastState = (GameState)(-1);
    
    private Vector3 initialComboPos;
    private bool hasInitComboPos = false;
    private float lastPauseTapTime = 0f; 

    
    
    void OnEnable()
    {
        ScoreManager.OnScoreUpdated += UpdateScoreUI;
        ScoreManager.OnGameStart += StartGameUI; 
        ScoreManager.OnGameComplete += ShowCompleteUI; 
    }

    void OnDisable()
    {
        ScoreManager.OnScoreUpdated -= UpdateScoreUI;
        ScoreManager.OnGameStart -= StartGameUI; 
        ScoreManager.OnGameComplete -= ShowCompleteUI;
    }

    void Start() 
    { 
        if (playerNameText != null) playerNameText.text = "Aoalas"; 
        
        if (volumeSlider != null) { volumeSlider.minValue = 0f; volumeSlider.maxValue = 1f; }
        if (audioOffsetSlider != null) { audioOffsetSlider.minValue = -0.3f; audioOffsetSlider.maxValue = 0.3f; }
        if (visualOffsetSlider != null) { visualOffsetSlider.minValue = -0.2f; visualOffsetSlider.maxValue = 0.2f; }
        if (scrollSpeedSlider != null) { scrollSpeedSlider.minValue = 1.0f; scrollSpeedSlider.maxValue = 9.9f; }

        if (topInfoBar != null)
        {
            originalTopBarPos = topInfoBar.GetComponent<RectTransform>().anchoredPosition;
            
            // 永远保持前置：赋予独立的 Canvas 并修改 Sorting Order
            Canvas canvas = topInfoBar.GetComponent<Canvas>();
            if (canvas == null) canvas = topInfoBar.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 99; // 给一个极高的层级，确保在其他 UI 和黑屏面板之上
            
            GraphicRaycaster gr = topInfoBar.GetComponent<GraphicRaycaster>();
            if (gr == null) topInfoBar.AddComponent<GraphicRaycaster>();
        }

        RefreshSettingsUI();
    }

    public void RefreshSettingsUI()
    {
        if (SettingsManager.Instance != null)
        {
            // 使用 SetValueWithoutNotify 避免触发 OnValueChanged 导致死循环或错误覆盖存档
            if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(SettingsManager.Instance.GlobalVolume);
            if (audioOffsetSlider != null) audioOffsetSlider.SetValueWithoutNotify(SettingsManager.Instance.AudioOffset);
            if (visualOffsetSlider != null) visualOffsetSlider.SetValueWithoutNotify(SettingsManager.Instance.VisualOffset);
            if (pauseModeDropdown != null) pauseModeDropdown.SetValueWithoutNotify(SettingsManager.Instance.PauseMode);
            if (scrollSpeedSlider != null) scrollSpeedSlider.SetValueWithoutNotify(SettingsManager.Instance.GlobalScrollSpeed);
            
            UpdateSettingsInputs();
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != lastState)
        {
            GameState previousState = lastState;
            lastState = GameManager.Instance.CurrentState;
            UpdatePanelVisibility(lastState, previousState);
        }

        if (displayScore < targetScore)
        {
            displayScore = Mathf.Lerp(displayScore, targetScore, Time.deltaTime * 30f);
            if (targetScore - displayScore < 5f) displayScore = targetScore;
            if (scoreText != null) scoreText.text = Mathf.RoundToInt(displayScore).ToString("D7");
        }
    }

    private MilInstantAnimator topBarAnim;
    private Vector2 originalTopBarPos;

    private void UpdatePanelVisibility(GameState state, GameState prevState)
    {
        if (openingPanel != null) openingPanel.SetActive(state == GameState.Opening);
        
        if (settingsPanel != null) 
        {
            bool isSettings = (state == GameState.Settings);
            settingsPanel.SetActive(isSettings);
            if (isSettings) RefreshSettingsUI(); 
        }
        
        if (creditsPanel != null) creditsPanel.SetActive(state == GameState.Credits);
        
        bool isTopBarState = (state == GameState.MainMenu || state == GameState.SongSelect || state == GameState.Result);
        bool wasTopBarState = (prevState == GameState.MainMenu || prevState == GameState.SongSelect || prevState == GameState.Result);
        bool isInitialLoad = ((int)prevState == -1); // 捕获重新加载场景时的初始状态
        
        if (topInfoBar != null)
        {
            // 如果是在打歌画面，且来自于前一个有TopBar的界面或者是点击Restart直接重载进入打歌界面的情况
            if (state == GameState.Playing && (wasTopBarState || isInitialLoad))
            {
                PlayTopBarExitAnim();
            }
            // 正常的非初始状态下切入有TopBar的界面（比如从游玩结束回到结算界面）
            else if (isTopBarState && !wasTopBarState && !isInitialLoad)
            {
                topInfoBar.SetActive(true);
                PlayTopBarEnterAnim();
            }
            // 已经是TopBar的界面之间切换，或是从结算直接返回选曲导致重新加载场景
            else if (isTopBarState && (wasTopBarState || isInitialLoad))
            {
                topInfoBar.SetActive(true);
            }
            else if (!isTopBarState && state != GameState.Playing)
            {
                topInfoBar.SetActive(false);
            }
        }
        
        if (mainMenuPanel != null) mainMenuPanel.SetActive(state == GameState.MainMenu);
        if (songSelectPanel != null) songSelectPanel.SetActive(state == GameState.SongSelect);
        if (gameplayPanel != null) gameplayPanel.SetActive(state == GameState.Playing);
        if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
        
        if (resultPanel != null)
        {
            bool isResult = (state == GameState.Result);
            resultPanel.SetActive(isResult);
            if (isResult) StartCoroutine(ShowResultUIAnimation());
        }
    }

    private void PlayTopBarEnterAnim()
    {
        var cg = topInfoBar.GetComponent<CanvasGroup>();
        if (cg == null) cg = topInfoBar.AddComponent<CanvasGroup>();
        var rect = topInfoBar.GetComponent<RectTransform>();
        
        if (topBarAnim != null) topBarAnim.Stop();
        
        cg.alpha = 0f;
        rect.anchoredPosition = new Vector2(originalTopBarPos.x, originalTopBarPos.y + 50f);
        
        topBarAnim = MAni.Make(
            0.4f / cg.MSineOut(x => x.alpha, 1f.ToThis()),
            0.4f / rect.MBackOut(x => x.anchoredPosition, originalTopBarPos.ToThis())
        );
        topBarAnim.Play();
    }

    private void PlayTopBarExitAnim()
    {
        var cg = topInfoBar.GetComponent<CanvasGroup>();
        if (cg == null) cg = topInfoBar.AddComponent<CanvasGroup>();
        var rect = topInfoBar.GetComponent<RectTransform>();
        
        if (topBarAnim != null) topBarAnim.Stop();
        
        topBarAnim = MAni.Make(
            0.3f / cg.MSineOut(x => x.alpha, 0f.ToThis()),
            0.3f / rect.MSineOut(x => x.anchoredPosition, new Vector2(originalTopBarPos.x, originalTopBarPos.y + 50f).ToThis())
        );
        topBarAnim.Play(() => {
            topInfoBar.SetActive(false);
        });
    }
    
    
    public void OnSettingsChanged()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings(
                volumeSlider != null ? volumeSlider.value : 1f,
                audioOffsetSlider != null ? audioOffsetSlider.value : 0f,
                visualOffsetSlider != null ? visualOffsetSlider.value : 0f,
                pauseModeDropdown != null ? pauseModeDropdown.value : 0,
                scrollSpeedSlider != null ? scrollSpeedSlider.value : 1f
            );
            UpdateSettingsInputs();
        }
    }
    
    private void UpdateSettingsInputs()
    {
        if (volumeInput != null && volumeSlider != null) volumeInput.text = (volumeSlider.value * 100f).ToString("0");
        if (audioOffsetInput != null && audioOffsetSlider != null) audioOffsetInput.text = (audioOffsetSlider.value * 1000f).ToString("0");
        if (visualOffsetInput != null && visualOffsetSlider != null) visualOffsetInput.text = (visualOffsetSlider.value * 1000f).ToString("0");
        if (scrollSpeedInput != null && scrollSpeedSlider != null) scrollSpeedInput.text = scrollSpeedSlider.value.ToString("0.0");
    }


    public void OnVolumeInputEnded(string val)
    {
        if (float.TryParse(val, out float result))
        {
            float v = Mathf.Clamp(result / 100f, 0f, 1f); 
            if (volumeSlider != null) volumeSlider.value = v; 
        }
        else UpdateSettingsInputs();
    }
    
    public void OnAudioOffsetInputEnded(string val)
    {
        if (float.TryParse(val, out float result))
        {
            float v = Mathf.Clamp(result / 1000f, -0.3f, 0.3f);
            if (audioOffsetSlider != null) audioOffsetSlider.value = v;
        }
        else UpdateSettingsInputs();
    }
    
    public void OnVisualOffsetInputEnded(string val)
    {
        if (float.TryParse(val, out float result))
        {
            float v = Mathf.Clamp(result / 1000f, -0.2f, 0.2f);
            if (visualOffsetSlider != null) visualOffsetSlider.value = v;
        }
        else UpdateSettingsInputs();
    }


    public void OnGameplayPauseButtonClicked()
    {
        if (SettingsManager.Instance.PauseMode == 0) GameManager.Instance.PauseGame();
        else
        {
            if (Time.unscaledTime - lastPauseTapTime < 0.3f)
            {
                GameManager.Instance.PauseGame();
                lastPauseTapTime = 0f;
            }
            else lastPauseTapTime = Time.unscaledTime;
        }
    }
    
    public void OnScrollSpeedInputEnded(string val)
    {
        if (float.TryParse(val, out float result))
        {
            float v = Mathf.Clamp(result, 1.0f, 9.9f); 
            if (scrollSpeedSlider != null) scrollSpeedSlider.value = v;
        }
        else UpdateSettingsInputs();
    }

    private void StartGameUI(string songName, ChartUIConfig uiConfig)
    {
        currentChartUIConfig = uiConfig;
        if (nameText != null) nameText.text = songName;
        if (completeText != null) completeText.gameObject.SetActive(false); 
        displayScore = 0f; targetScore = 0f;
        if (scoreText != null) scoreText.text = "0000000";

        if (comboNumberText != null) 
        {
            if (!hasInitComboPos)
            {
                initialComboPos = comboNumberText.transform.localPosition;
                hasInitComboPos = true;
            }
            comboNumberText.transform.localPosition = initialComboPos; 
            comboNumberText.gameObject.SetActive(false);
        }
        
        if (comboLabelText != null) comboLabelText.gameObject.SetActive(false);
        if (comboLabelText != null) comboLabelText.color = Color.white;
        if (comboNumberText != null) comboNumberText.color = Color.white;
    }

    private void UpdateScoreUI(int score, int combo)
    {
        targetScore = score; 
        if (combo > 0)
        {
            if (missCoroutine != null) 
            { 
                StopCoroutine(missCoroutine);
                missCoroutine = null;
                if (comboNumberText != null) comboNumberText.transform.localPosition = initialComboPos;
            }

            if (currentChartUIConfig != null && currentChartUIConfig.hideCombo) return;
            
            comboLabelText.gameObject.SetActive(true); comboNumberText.gameObject.SetActive(true);
            comboLabelText.color = Color.white; comboNumberText.color = Color.white;
            comboNumberText.text = combo.ToString();
            comboNumberText.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            (0.4f / comboNumberText.transform.MBackOut(t => t.localScale, new Vector3(1.2f, 1.2f, 1.2f), Vector3.one)).Play();
        }
        else
        {
            if (currentChartUIConfig != null && currentChartUIConfig.hideCombo) return;
            if (comboNumberText.gameObject.activeSelf) 
            {
                missCoroutine = StartCoroutine(PlayMissAnimation());
            }
        }
    }

    private IEnumerator PlayMissAnimation()
    {
        comboLabelText.color = Color.red; comboNumberText.color = Color.red; comboNumberText.text = "0";
        float timer = 0, duration = 0.2f; 
        while (timer < duration)
        {
            timer += Time.deltaTime; float progress = timer / duration;
            float shakeForce = (1f - progress) * 15f; 
            
            comboNumberText.transform.localPosition = initialComboPos + new Vector3(Random.Range(-shakeForce, shakeForce), Random.Range(-shakeForce, shakeForce), 0);
            
            Color cLabel = comboLabelText.color; Color cNum = comboNumberText.color;
            cLabel.a = 1f - progress; cNum.a = 1f - progress;
            comboLabelText.color = cLabel; comboNumberText.color = cNum;
            yield return null; 
        }

        comboNumberText.transform.localPosition = initialComboPos; 
        comboLabelText.gameObject.SetActive(false); comboNumberText.gameObject.SetActive(false);
    }

    private void ShowCompleteUI() { StartCoroutine(CompleteRoutine()); }

    private IEnumerator CompleteRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        if (currentChartUIConfig == null || !currentChartUIConfig.disableComplete)
        {
            if (completeText != null)
            {
                completeText.gameObject.SetActive(true);
                completeText.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                (1.8f / completeText.transform.MElasticOut(t => t.localScale, new Vector3(0.9f, 0.9f, 0.9f), Vector3.one)).Play();
            }
        }
        
        yield return new WaitForSeconds(5.5f);
        GameManager.Instance.ChangeState(GameState.Result);
    }

    private IEnumerator ShowResultUIAnimation()
    {
        if (GameManager.currentSong != null)
        {
            if (resultSongNameText != null) resultSongNameText.text = GameManager.currentSong.songName;
            if (resultArtistText != null) resultArtistText.text = GameManager.currentSong.artist;
            if (resultCoverImage != null && GameManager.currentSong.coverArt != null) 
            {
                resultCoverImage.sprite = GameManager.currentSong.coverArt;
            }
        }

        if (resultPanelCanvasGroup != null)
        {
            resultPanelCanvasGroup.alpha = 0f;
            float t = 0;
            while (t < 0.5f) { t += Time.deltaTime; resultPanelCanvasGroup.alpha = t / 0.5f; yield return null; }
            resultPanelCanvasGroup.alpha = 1f;
        }

        float duration = 1.5f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            float easeOut = 1f - Mathf.Pow(1f - progress, 3);

            if (resultScoreText != null) resultScoreText.text = Mathf.RoundToInt(ScoreManager.CurrentScore * easeOut).ToString("D7");
            if (resultPerfectText != null) resultPerfectText.text = Mathf.RoundToInt(ScoreManager.PerfectCount * easeOut).ToString();
            if (resultGoodText != null) resultGoodText.text = Mathf.RoundToInt(ScoreManager.GoodCount * easeOut).ToString();
            if (resultMissText != null) resultMissText.text = Mathf.RoundToInt(ScoreManager.MissCount * easeOut).ToString();
            if (resultMaxComboText != null) resultMaxComboText.text = Mathf.RoundToInt(ScoreManager.MaxCombo * easeOut).ToString();

            yield return null;
        }
        
        if (resultScoreText != null) resultScoreText.text = Mathf.RoundToInt(ScoreManager.CurrentScore).ToString("D7");
        if (resultPerfectText != null) resultPerfectText.text = ScoreManager.PerfectCount.ToString();
        if (resultGoodText != null) resultGoodText.text = ScoreManager.GoodCount.ToString();
        if (resultMissText != null) resultMissText.text = ScoreManager.MissCount.ToString();
        if (resultMaxComboText != null) resultMaxComboText.text = ScoreManager.MaxCombo.ToString();
    }
}