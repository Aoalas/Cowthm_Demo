using UnityEngine;
using TMPro;
using Milease.DSL;
using System.Collections; // 必须引入这个来使用协程(Coroutine)

public class UIManager : MonoBehaviour
{
    [Header("UI 文本组件引用")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboLabelText; 
    public TextMeshProUGUI comboNumberText; 
    
    [Header("结算 UI")]
    public TextMeshProUGUI completeText; // 【新增】拖入你的 Complete 文本

    // 用于滚分的变量
    private float displayScore = 0f;
    private float targetScore = 0f;
    
    // 用于管理 Miss 动画，防止动画冲突
    private Coroutine missCoroutine; 

    void OnEnable()
    {
        ScoreManager.OnScoreUpdated += UpdateUI;
        ScoreManager.OnGameStart += StartGameUI; 
        ScoreManager.OnGameComplete += ShowCompleteUI; // 订阅完成事件
    }

    void OnDisable()
    {
        ScoreManager.OnScoreUpdated -= UpdateUI;
        ScoreManager.OnGameStart -= StartGameUI; 
        ScoreManager.OnGameComplete -= ShowCompleteUI;
    }

    private void StartGameUI(string songName)
    {
        if (nameText != null) nameText.text = songName;
        if (completeText != null) completeText.gameObject.SetActive(false); // 开局隐藏 Complete
        
        displayScore = 0f;
        targetScore = 0f;
        if (scoreText != null) scoreText.text = "0000000";
        
        if (comboLabelText != null) comboLabelText.gameObject.SetActive(false);
        if (comboNumberText != null) comboNumberText.gameObject.SetActive(false);
        
        // 确保 Combo 的颜色是白色的（因为 Miss 后可能会变红）
        if (comboLabelText != null) comboLabelText.color = Color.white;
        if (comboNumberText != null) comboNumberText.color = Color.white;
    }

    void Update()
    {
        // 1. 【分数滚动动画】：如果当前显示的分数还没追上目标分数
        if (displayScore < targetScore)
        {
            // 使用 Lerp 平滑追赶，15f 是追赶速度系数，可以自己微调
            displayScore = Mathf.Lerp(displayScore, targetScore, Time.deltaTime * 30f);
            
            // 如果差距极小了，就直接对齐，防止一直卡在 999999
            if (targetScore - displayScore < 5f) displayScore = targetScore;
            
            if (scoreText != null) 
                scoreText.text = Mathf.RoundToInt(displayScore).ToString("D7");
        }
    }

    private void UpdateUI(int score, int combo)
    {
        targetScore = score; // 更新目标分数，让 Update 去滚字

        if (combo > 0)
        {
            // 如果之前在播放 Miss 动画，打断它，因为玩家又重新连上了！
            if (missCoroutine != null) StopCoroutine(missCoroutine);
            
            comboLabelText.gameObject.SetActive(true);
            comboNumberText.gameObject.SetActive(true);
            
            // 恢复正常的白色
            comboLabelText.color = Color.white;
            comboNumberText.color = Color.white;
            comboNumberText.text = combo.ToString();

            // 2. 【Combo 保持弹跳动画 (Milease)】
            // 瞬间把它放大到 1.3 倍，然后在 0.15 秒内 Q 弹地缩回 1.0 倍
            comboNumberText.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            (0.4f / comboNumberText.transform.MBackOut(t => t.localScale, new Vector3(1.2f, 1.2f, 1.2f), Vector3.one)).Play();
        }
        else
        {
            // 断连了 (Miss)
            if (comboNumberText.gameObject.activeSelf)
            {
                // 启动抖动淡出协程
                missCoroutine = StartCoroutine(PlayMissAnimation());
            }
        }
    }

    // 3. 【Miss 抖动淡出动画】
    private IEnumerator PlayMissAnimation()
    {
        // 瞬间变红
        comboLabelText.color = Color.red;
        comboNumberText.color = Color.red;
        
        comboNumberText.text = "0";

        Vector3 originalPos = comboNumberText.transform.localPosition;
        float timer = 0;
        float duration = 0.2f; // 动画时长 0.3 秒

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            // 抖动效果 (随着时间推移，抖动幅度越来越小)
            float shakeForce = (1f - progress) * 15f; 
            comboNumberText.transform.localPosition = originalPos + new Vector3(Random.Range(-shakeForce, shakeForce), Random.Range(-shakeForce, shakeForce), 0);

            // 淡出效果 (Alpha 值从 1 降到 0)
            Color cLabel = comboLabelText.color;
            Color cNum = comboNumberText.color;
            cLabel.a = 1f - progress;
            cNum.a = 1f - progress;
            comboLabelText.color = cLabel;
            comboNumberText.color = cNum;

            yield return null; // 等待下一帧
        }

        // 动画结束，恢复原位并彻底隐藏
        comboNumberText.transform.localPosition = originalPos; 
        comboLabelText.gameObject.SetActive(false);
        comboNumberText.gameObject.SetActive(false);
    }

    // --- 结算逻辑 ---
    private void ShowCompleteUI()
    {
        StartCoroutine(CompleteRoutine());
    }

    private IEnumerator CompleteRoutine()
    {
        yield return new WaitForSeconds(0.3f);

        if (completeText != null)
        {
            completeText.gameObject.SetActive(true);
            
            // 4. 【Complete 史诗级入场 (Milease)】
            completeText.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            (1.8f / completeText.transform.MElasticOut(t => t.localScale, new Vector3(0.9f, 0.9f, 0.9f), Vector3.one)).Play();
            
            // 注：如果编译说找不到 MElasticOut，你可以把它换成 MBackOut
        }
    }
}