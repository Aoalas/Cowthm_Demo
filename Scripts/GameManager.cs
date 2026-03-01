using UnityEngine;
using UnityEngine.SceneManagement; 

public enum GameState { Opening, MainMenu, Settings, Credits, SongSelect, Playing, Paused, Result }

[System.Serializable]
public class SongMetaData
{
    public string songName;
    public string artist;
    public Sprite coverArt;
    public TextAsset chartJson; 
    public AudioClip songAudio; // 【新增】绑定的音频文件
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public float CurrentGameTime { get; private set; } = 0f;
    
    public float CurrentAudioTime => CurrentGameTime + SettingsManager.Instance.AudioOffset;
    public float CurrentVisualTime => CurrentAudioTime + SettingsManager.Instance.VisualOffset;

    public static bool autoStartNextTime = false; 
    public static bool returnToSongSelect = false; 
    public static bool hasSeenOpening = false; 
    
    public static SongMetaData currentSong; 

    [Header("游玩音频控制")]
    public AudioSource gameplayBGM;

    private float currentGameplayVolume = 1.0f;
    
    void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            
            Application.targetFrameRate = 120; 
            
            QualitySettings.vSyncCount = 0;    
            
            Screen.sleepTimeout = SleepTimeout.NeverSleep; 

        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    void Start()
    {
        if (autoStartNextTime)
        {
            autoStartNextTime = false;
            ChartManager.Instance.LoadAndSpawnChart(currentSong.chartJson);
            StartGame();
        }
        else if (returnToSongSelect)
        {
            returnToSongSelect = false;
            ChangeState(GameState.SongSelect);
        }
        else if (!hasSeenOpening) 
        {
            hasSeenOpening = true;
            ChangeState(GameState.Opening);
        }
        else
        {
            ChangeState(GameState.MainMenu);
        }
    }

    void Update() { if (CurrentState == GameState.Playing) CurrentGameTime += Time.deltaTime; }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"<color=yellow>游戏状态切换至: {newState}</color>");

        // 【新增】如果离开游玩状态（比如进入结算），停止音乐
        if (newState == GameState.Result && gameplayBGM != null)
        {
            gameplayBGM.Stop();
        }
    }
    
    public void GoToSettings() { ChangeState(GameState.Settings); }
    public void GoToCredits() { ChangeState(GameState.Credits); }
    public void BackToMainMenu() { ChangeState(GameState.MainMenu); }

    public void FinishOpening() { ChangeState(GameState.MainMenu); } 
    public void GoToSongSelect() { ChangeState(GameState.SongSelect); }
    
    public void StartGame() 
    { 
        CurrentGameTime = 0f; 
        
        if (gameplayBGM != null && currentSong != null && currentSong.songAudio != null)
        {
            gameplayBGM.clip = currentSong.songAudio;
            
            // 获取铺面设置的音量 (如果没有设，默认为1)
            float chartVol = ChartManager.Instance.currentChart != null ? ChartManager.Instance.currentChart.musicVolume : 1.0f;
            currentGameplayVolume = chartVol;
            
            // 物理音量最多只有 1.0
            gameplayBGM.volume = Mathf.Min(currentGameplayVolume, 1.0f);
            
            gameplayBGM.time = 0f;
            gameplayBGM.Play();
        }

        ChangeState(GameState.Playing); 
    }
    
    public void PauseGame() 
    { 
        if (CurrentState == GameState.Playing) 
        {
            // 暂停音乐
            if (gameplayBGM != null && gameplayBGM.isPlaying) gameplayBGM.Pause();
            ChangeState(GameState.Paused); 
        }
    }

    public void ResumeGame() 
    { 
        if (CurrentState == GameState.Paused) 
        {
            // 恢复音乐
            if (gameplayBGM != null && currentSong != null && currentSong.songAudio != null) gameplayBGM.Play();
            ChangeState(GameState.Playing); 
        }
    }

    public void RestartGame() { autoStartNextTime = true; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void BackToSongSelect() { returnToSongSelect = true; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void QuitToMenu() { autoStartNextTime = false; returnToSongSelect = false; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void QuitGame() { Application.Quit(); }
    
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (CurrentState != GameState.Playing || currentGameplayVolume <= 1.0f) return;

        float boost = currentGameplayVolume;
        for (int i = 0; i < data.Length; i++)
        {
            float val = data[i] * boost;
            if (val > 1f) val = 1f;
            else if (val < -1f) val = -1f;
            data[i] = val;
        }
    }
}