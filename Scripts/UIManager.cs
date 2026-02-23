using UnityEngine;
using TMPro;
using Milease.DSL;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("UI 面板 (Panels)")]
    public GameObject topInfoBar;       
    public GameObject mainMenuPanel;    
    public GameObject songSelectPanel;  
    public GameObject gameplayPanel;    
    public GameObject pausePanel;       
    public GameObject resultPanel;      

    [Header("顶部信息栏引用")]
    public TextMeshProUGUI playerNameText;

    [Header("游玩 UI 文本组件引用")]
    public TextMeshProUGUI nameText; 
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboLabelText; 
    public TextMeshProUGUI comboNumberText; 
    public TextMeshProUGUI completeText; 

    [Header("结算面板 UI")]
    public CanvasGroup resultPanelCanvasGroup; // 【新增】用于整个面板的渐显
    public TextMeshProUGUI resultSongNameText;
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

    void Start() { if (playerNameText != null) playerNameText.text = "Aoalas"; }

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
        if (topInfoBar != null) topInfoBar.SetActive(state == GameState.MainMenu || state == GameState.SongSelect);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(state == GameState.MainMenu);
        if (songSelectPanel != null) songSelectPanel.SetActive(state == GameState.SongSelect);
        if (gameplayPanel != null) gameplayPanel.SetActive(state == GameState.Playing);
        if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
        
        // 【新增】处理结算面板的激活和动画触发
        if (resultPanel != null)
        {
            bool isResult = (state == GameState.Result);
            resultPanel.SetActive(isResult);
            if (isResult)
            {
                StartCoroutine(ShowResultUIAnimation());
            }
        }
    }

    private void StartGameUI(string songName, ChartUIConfig uiConfig)
    {
        currentChartUIConfig = uiConfig;
        if (nameText != null) nameText.text = songName;
        if (completeText != null) completeText.gameObject.SetActive(false); 
        displayScore = 0f; targetScore = 0f;
        if (scoreText != null) scoreText.text = "0000000";
        if (comboLabelText != null) comboLabelText.gameObject.SetActive(false);
        if (comboNumberText != null) comboNumberText.gameObject.SetActive(false);
    }

    private void UpdateScoreUI(int score, int combo)
    {
        targetScore = score; 
        if (combo > 0)
        {
            if (missCoroutine != null) StopCoroutine(missCoroutine);
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
            if (comboNumberText.gameObject.activeSelf) missCoroutine = StartCoroutine(PlayMissAnimation());
        }
    }

    private IEnumerator PlayMissAnimation()
    {
        comboLabelText.color = Color.red; comboNumberText.color = Color.red; comboNumberText.text = "0";
        Vector3 originalPos = comboNumberText.transform.localPosition;
        float timer = 0, duration = 0.2f; 
        while (timer < duration)
        {
            timer += Time.deltaTime; float progress = timer / duration;
            float shakeForce = (1f - progress) * 15f; 
            comboNumberText.transform.localPosition = originalPos + new Vector3(Random.Range(-shakeForce, shakeForce), Random.Range(-shakeForce, shakeForce), 0);
            Color cLabel = comboLabelText.color; Color cNum = comboNumberText.color;
            cLabel.a = 1f - progress; cNum.a = 1f - progress;
            comboLabelText.color = cLabel; comboNumberText.color = cNum;
            yield return null; 
        }
        comboNumberText.transform.localPosition = originalPos; 
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
        
        // 【核心修改】：等待 5.5 秒，加上前面的 0.3 秒，就是大约 6 秒
        yield return new WaitForSeconds(5.5f);
        
        // 告诉状态机：是时候打开结算界面了！
        GameManager.Instance.ChangeState(GameState.Result);
    }

    // 【新增】华丽的数值滚动动画
    private IEnumerator ShowResultUIAnimation()
    {
        if (resultSongNameText != null && nameText != null) resultSongNameText.text = nameText.text;

        // 1. 面板淡入
        if (resultPanelCanvasGroup != null)
        {
            resultPanelCanvasGroup.alpha = 0f;
            float t = 0;
            while (t < 0.5f) { t += Time.deltaTime; resultPanelCanvasGroup.alpha = t / 0.5f; yield return null; }
            resultPanelCanvasGroup.alpha = 1f;
        }

        // 2. 数值动态暴涨 (用 1.5 秒的时间涨满)
        float duration = 1.5f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            // 缓动曲线，让数字一开始涨得快，最后慢下来
            float easeOut = 1f - Mathf.Pow(1f - progress, 3);

            if (resultScoreText != null) resultScoreText.text = Mathf.RoundToInt(ScoreManager.CurrentScore * easeOut).ToString("D7");
            if (resultPerfectText != null) resultPerfectText.text = Mathf.RoundToInt(ScoreManager.PerfectCount * easeOut).ToString();
            if (resultGoodText != null) resultGoodText.text = Mathf.RoundToInt(ScoreManager.GoodCount * easeOut).ToString();
            if (resultMissText != null) resultMissText.text = Mathf.RoundToInt(ScoreManager.MissCount * easeOut).ToString();
            if (resultMaxComboText != null) resultMaxComboText.text = Mathf.RoundToInt(ScoreManager.MaxCombo * easeOut).ToString();

            yield return null;
        }
        
        // 3. 最终校准数值，防止浮点误差
        if (resultScoreText != null) resultScoreText.text = Mathf.RoundToInt(ScoreManager.CurrentScore).ToString("D7");
        if (resultPerfectText != null) resultPerfectText.text = ScoreManager.PerfectCount.ToString();
        if (resultGoodText != null) resultGoodText.text = ScoreManager.GoodCount.ToString();
        if (resultMissText != null) resultMissText.text = ScoreManager.MissCount.ToString();
        if (resultMaxComboText != null) resultMaxComboText.text = ScoreManager.MaxCombo.ToString();
    }
}