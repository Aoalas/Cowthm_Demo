using UnityEngine;
using UnityEngine.SceneManagement; 

public enum GameState { MainMenu, SongSelect, Playing, Paused, Result }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public float CurrentGameTime { get; private set; } = 0f;

    public static bool autoStartNextTime = false; 
    public static TextAsset currentPlayingChart; 
    
    // 【新增】利用静态变量告诉下一个场景：直接切到选曲界面！
    public static bool returnToSongSelect = false; 

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (autoStartNextTime)
        {
            autoStartNextTime = false;
            ChartManager.Instance.LoadAndSpawnChart(currentPlayingChart);
            StartGame();
        }
        else if (returnToSongSelect) // 【新增】如果是打完歌退回来的
        {
            returnToSongSelect = false;
            ChangeState(GameState.SongSelect); // 直接去选曲界面
        }
        else
        {
            ChangeState(GameState.MainMenu);
        }
    }

    void Update()
    {
        if (CurrentState == GameState.Playing) CurrentGameTime += Time.deltaTime;
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"<color=yellow>游戏状态切换至: {newState}</color>");
    }

    public void GoToSongSelect() { ChangeState(GameState.SongSelect); }
    public void StartGame() { CurrentGameTime = 0f; ChangeState(GameState.Playing); }
    public void PauseGame() { if (CurrentState == GameState.Playing) ChangeState(GameState.Paused); }
    public void ResumeGame() { if (CurrentState == GameState.Paused) ChangeState(GameState.Playing); }
    
    public void RestartGame()
    {
        autoStartNextTime = true; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 【新增】从结算界面返回选曲
    public void BackToSongSelect()
    {
        returnToSongSelect = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        autoStartNextTime = false; 
        returnToSongSelect = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame() { Application.Quit(); }
}