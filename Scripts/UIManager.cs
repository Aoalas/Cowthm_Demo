using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Milease.DSL;
using System.Collections;

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
        if (SettingsManager.Instance != null)
        {
            if (volumeSlider != null) volumeSlider.value = SettingsManager.Instance.GlobalVolume;
            if (audioOffsetSlider != null) audioOffsetSlider.value = SettingsManager.Instance.AudioOffset;
            if (visualOffsetSlider != null) visualOffsetSlider.value = SettingsManager.Instance.VisualOffset;
            if (pauseModeDropdown != null) pauseModeDropdown.value = SettingsManager.Instance.PauseMode;
            if (scrollSpeedSlider != null) scrollSpeedSlider.value = SettingsManager.Instance.GlobalScrollSpeed;
            
            UpdateSettingsInputs();
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != lastState)
        {
            lastState = GameManager.Instance.CurrentState;
            UpdatePanelVisibility(lastState);
        }

        if (displayScore < targetScore)
        {
            displayScore = Mathf.Lerp(displayScore, targetScore, Time.deltaTime * 30f);
            if (targetScore - displayScore < 5f) displayScore = targetScore;
            if (scoreText != null) scoreText.text = Mathf.RoundToInt(displayScore).ToString("D7");
        }
    }

    private void UpdatePanelVisibility(GameState state)
    {
        if (openingPanel != null) openingPanel.SetActive(state == GameState.Opening);
        if (settingsPanel != null) settingsPanel.SetActive(state == GameState.Settings);
        if (creditsPanel != null) creditsPanel.SetActive(state == GameState.Credits);
        
        if (topInfoBar != null) topInfoBar.SetActive(state == GameState.MainMenu || state == GameState.SongSelect);
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
    

    // 1. 供 Slider 和 Dropdown 滑动时调用
    public void OnSettingsChanged()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings(
                volumeSlider != null ? volumeSlider.value : 1f,
                audioOffsetSlider != null ? audioOffsetSlider.value : 0f,
                visualOffsetSlider != null ? visualOffsetSlider.value : 0f,
                pauseModeDropdown != null ? pauseModeDropdown.value : 0,
                scrollSpeedSlider != null ? scrollSpeedSlider.value : 1f // 【新增】
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

    // 2. 供 音量 InputField 输入完成时调用
// 2. 供 音量 InputField 输入完成时调用
    public void OnVolumeInputEnded(string val)
    {
        if (float.TryParse(val, out float result))
        {
            float v = Mathf.Clamp(result / 100f, 0f, 1f); 
            if (volumeSlider != null) volumeSlider.value = v; 
        }
        else UpdateSettingsInputs();
    }

    // 3. 供 音频偏移 InputField 输入完成时调用
    public void OnAudioOffsetInputEnded(string val)
    {
        if (float.TryParse(val, out float result))
        {
            float v = Mathf.Clamp(result / 1000f, -0.3f, 0.3f); // 输入毫秒，限制在正负 300ms
            if (audioOffsetSlider != null) audioOffsetSlider.value = v;
        }
        else UpdateSettingsInputs();
    }

    // 4. 供 视觉偏移 InputField 输入完成时调用
    public void OnVisualOffsetInputEnded(string val)
    {
        if (float.TryParse(val, out float result))
        {
            float v = Mathf.Clamp(result / 1000f, -0.2f, 0.2f); // 输入毫秒，限制在正负 200ms
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
            // 限制在 1.0 到 9.9 之间
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