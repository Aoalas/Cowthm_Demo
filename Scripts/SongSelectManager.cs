using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class SongSelectManager : MonoBehaviour
{
    private List<SongMetaData> songDatabase = new List<SongMetaData>();

    [Header("左侧滑动列表 UI")]
    public Transform scrollContentParent; 
    public GameObject songListItemPrefab; 

    [Header("右侧详情 UI")]
    public TextMeshProUGUI rightSongNameText;
    public TextMeshProUGUI rightArtistText;
    public Image rightCoverImage;
    
    [Header("转场特效")]
    public CanvasGroup transitionScreen; 

    private int currentSelectedIndex = -1;

    void Start()
    {
        LoadSongsFromResources();
        
        GenerateList();
        
        if (songDatabase.Count > 0)
        {
            SelectSong(0); 
        }
        else
        {
            Debug.LogWarning("曲库为空或所有谱面均已损坏，选曲列表生成跳过。");
        }
    }
    
    private void LoadSongsFromResources()
    {
        TextAsset[] chartFiles = Resources.LoadAll<TextAsset>("Charts");
        
        foreach (var file in chartFiles)
        {
            try 
            {
                ChartData data = JsonUtility.FromJson<ChartData>(file.text);
                
                if (data == null || string.IsNullOrEmpty(data.chartId))
                {
                    Debug.LogError($"<color=red>【谱面拦截】</color> 文件 {file.name} JSON格式损坏，已隔离！");
                    continue; 
                }
                
                Sprite cover = Resources.Load<Sprite>("Covers/" + data.chartId);
                if (cover == null)
                {
                    Debug.LogError($"<color=orange>【谱面拦截】</color> 曲目 {data.chartId} 缺少曲绘，已隔离！");
                    continue; 
                }
                
                AudioClip audio = Resources.Load<AudioClip>("Audio/" + data.songName);
                
                if (audio == null)
                {
                    if (data.requireAudio)
                    {
                        Debug.LogError($"<color=orange>【谱面拦截】</color> 曲目 {data.songName} 强制要求音频，但未在 Audio 文件夹下找到同名文件，已隔离！");
                        continue;
                    }
                    else
                    {
                        Debug.LogWarning($"<color=yellow>【提示】</color> 曲目 {data.songName} 未找到音频，将进行无声游玩。");
                    }
                }
                
                SongMetaData meta = new SongMetaData
                {
                    songName = string.IsNullOrEmpty(data.songName) ? "Unknown Song" : data.songName,
                    artist = string.IsNullOrEmpty(data.artist) ? "Unknown Artist" : data.artist,
                    coverArt = cover,
                    chartJson = file,
                    songAudio = audio
                };
                
                songDatabase.Add(meta);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"<color=red>【解析崩溃】</color> 读取谱面 {file.name} 时发生异常: {e.Message}");
            }
        }
        
        Debug.Log($"<color=green>✓ 安全扫描完毕！共加载了 {songDatabase.Count} 首曲目。</color>");
    }

    private void GenerateList()
    {

        if (songDatabase.Count == 0) return;

        for (int i = 0; i < songDatabase.Count; i++)
        {
            int index = i; 
            GameObject item = Instantiate(songListItemPrefab, scrollContentParent);
            
            TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = songDatabase[i].songName;
            if (texts.Length > 1) texts[1].text = songDatabase[i].artist;

            Button btn = item.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSongItemClicked(index));
        }
    }

    public void OnSongItemClicked(int index)
    {
        if (currentSelectedIndex == index) StartCoroutine(TransitionAndPlay(index));
        else SelectSong(index);
    }

    private void SelectSong(int index)
    {

        if (songDatabase.Count == 0 || index < 0 || index >= songDatabase.Count) return;

        currentSelectedIndex = index;
        SongMetaData data = songDatabase[index];

        if (rightSongNameText != null) rightSongNameText.text = data.songName;
        if (rightArtistText != null) rightArtistText.text = data.artist;
        if (rightCoverImage != null) rightCoverImage.sprite = data.coverArt;
    }

    private IEnumerator TransitionAndPlay(int index)
    {
        if (transitionScreen != null)
        {
            transitionScreen.gameObject.SetActive(true);
            transitionScreen.alpha = 0f;
            float timer = 0;
            while (timer < 0.4f) { timer += Time.deltaTime; transitionScreen.alpha = timer / 0.4f; yield return null; }
        }

        GameManager.currentSong = songDatabase[index];
        
        ChartManager.Instance.LoadAndSpawnChart(songDatabase[index].chartJson);
        GameManager.Instance.StartGame();

        if (transitionScreen != null)
        {
            float timer = 0;
            while (timer < 0.4f) { timer += Time.deltaTime; transitionScreen.alpha = 1f - (timer / 0.4f); yield return null; }
            transitionScreen.gameObject.SetActive(false);
        }
    }
}