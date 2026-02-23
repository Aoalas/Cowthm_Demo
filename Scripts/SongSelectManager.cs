using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Milease.DSL; // 如果用到 Milease 动画的话

// 定义单首歌曲的“身份证”
[System.Serializable]
public class SongMetaData
{
    public string songName;
    public string artist;
    public Sprite coverArt;
    public TextAsset chartJson; // 对应的谱面文件
}

public class SongSelectManager : MonoBehaviour
{
    [Header("曲库数据库")]
    public List<SongMetaData> songDatabase;

    [Header("左侧滑动列表 UI")]
    public Transform scrollContentParent; // ScrollView 的 Content 物体
    public GameObject songListItemPrefab; // 列表里的单个按钮预制体

    [Header("右侧详情 UI")]
    public TextMeshProUGUI rightSongNameText;
    public TextMeshProUGUI rightArtistText;
    public Image rightCoverImage;
    
    [Header("转场特效")]
    public CanvasGroup transitionScreen; // 用来做黑屏渐变的 CanvasGroup

    private int currentSelectedIndex = -1;

    void Start()
    {
        GenerateList();
        if (songDatabase.Count > 0)
        {
            SelectSong(0); // 默认选中第一首
        }
    }

    private void GenerateList()
    {
        // 遍历曲库，生成左侧按钮
        for (int i = 0; i < songDatabase.Count; i++)
        {
            int index = i; // 闭包捕获
            GameObject item = Instantiate(songListItemPrefab, scrollContentParent);
            
            // 假设预制体里有两个 TextMeshPro 分别显示歌名和作者
            TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = songDatabase[i].songName;
            if (texts.Length > 1) texts[1].text = songDatabase[i].artist;

            // 绑定点击事件
            Button btn = item.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSongItemClicked(index));
        }
    }

    public void OnSongItemClicked(int index)
    {
        if (currentSelectedIndex == index)
        {
            // 【核心逻辑】：预览状态下再次点击同一首 -> 进入游戏！
            StartCoroutine(TransitionAndPlay(index));
        }
        else
        {
            // 点了别的歌 -> 切换右侧预览
            SelectSong(index);
        }
    }

    private void SelectSong(int index)
    {
        currentSelectedIndex = index;
        SongMetaData data = songDatabase[index];

        if (rightSongNameText != null) rightSongNameText.text = data.songName;
        if (rightArtistText != null) rightArtistText.text = data.artist;
        if (rightCoverImage != null) rightCoverImage.sprite = data.coverArt;
    }

    private IEnumerator TransitionAndPlay(int index)
    {
        // 1. 黑屏渐入 (预加载动画)
        if (transitionScreen != null)
        {
            transitionScreen.gameObject.SetActive(true);
            transitionScreen.alpha = 0f;
            
            // 手写简单的渐变，防止 Milease 版本不兼容
            float timer = 0;
            while (timer < 0.4f)
            {
                timer += Time.deltaTime;
                transitionScreen.alpha = timer / 0.4f;
                yield return null;
            }
        }

        // 2. 将选中的 JSON 交给 ChartManager 解析生成
        ChartManager.Instance.LoadAndSpawnChart(songDatabase[index].chartJson);

        // 3. 通知状态机切换到 Playing
        GameManager.Instance.StartGame();

        // 4. 黑屏渐出
        if (transitionScreen != null)
        {
            float timer = 0;
            while (timer < 0.4f)
            {
                timer += Time.deltaTime;
                transitionScreen.alpha = 1f - (timer / 0.4f);
                yield return null;
            }
            transitionScreen.gameObject.SetActive(false);
        }
    }
}