using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;
using Milease.DSL;
using Minity.Pooling; 

// 1. 扩展对象池 ID，支持多种形状
public enum RhythmPoolID
{
    NoteShape0, // 默认圆形
    NoteShape1, // 方块
    NoteShape2  // 菱形/其他
}

public class ChartManager : MonoBehaviour
{
    public TextAsset chartJsonFile; 
    
    [Header("视觉资源配置")]
    [Tooltip("放入对应的音符Prefab，下标0对应Shape0")]
    public PoolableObject[] notePrefabs; 
    public GameObject trackPrefab;
    [Tooltip("判定点形状图片，下标0对应Shape0")]
    public Sprite[] judgeSprites;

    [Header("材质配置")]
    public Material normalMat; // 普通材质
    public Material glowMat;   // 发光材质 (使用带 HDR 或 Emission 的材质)

    public ChartData currentChart;

    void Start()
    {
        // 2. 批量注册所有音符预制体到各自的对象池
        if (notePrefabs != null)
        {
            for (int i = 0; i < notePrefabs.Length; i++)
            {
                if (notePrefabs[i] != null)
                {
                    ObjectPool.EnsurePrefabRegistered((RhythmPoolID)i, notePrefabs[i].gameObject, 20);
                }
            }
        }
        LoadChart();
        SpawnEntities();
    }

    void LoadChart()
    {
        if (chartJsonFile != null)
        {
            // 将 JSON 文本解析为 ChartData 对象
            currentChart = JsonUtility.FromJson<ChartData>(chartJsonFile.text);
            Debug.Log($"成功加载谱面！包含 {currentChart.tracks.Count} 条轨迹，和 {currentChart.notes.Count} 个音符。");
        }
        else
        {
            Debug.LogError("没有找到谱面文件！请检查 Chart Json File 是否已经在面板上赋值。");
        }
    }

    void SpawnEntities()
    {
        if (currentChart == null) return;
        ScoreManager.Init(currentChart.notes.Count, currentChart.songName);

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        Dictionary<int, TrackData> trackDict = new Dictionary<int, TrackData>();
        
        // --- 生成轨迹 ---
        foreach (var track in currentChart.tracks)
        {
            // 【核心数学】：计算旋转后的终点坐标！
            // 以 StartPos 为圆心，旋转 rotation 角度
            float rad = track.rotation * Mathf.Deg2Rad;
            float dx = track.endX - track.startX;
            float dy = track.endY - track.startY;
            float finalEndX = track.startX + (dx * Mathf.Cos(rad) - dy * Mathf.Sin(rad));
            float finalEndY = track.startY + (dx * Mathf.Sin(rad) + dy * Mathf.Cos(rad));

            // 更新数据字典，让后续的音符读取到旋转后的真实坐标
            track.endX = finalEndX;
            track.endY = finalEndY;
            trackDict[track.trackId] = track;

            Entity trackEntity = entityManager.CreateEntity(typeof(TrackComponent));
            entityManager.SetComponentData(trackEntity, new TrackComponent
            {
                TrackId = track.trackId,
                StartPos = new float2(track.startX, track.startY),
                EndPos = new float2(track.endX, track.endY), // 使用旋转后的坐标
                CurveType = track.curveType,
                Amplitude = track.amplitude,
                Frequency = track.frequency
            });

            // 【视觉渲染】：宽度、透明度、发光、判定形状
            if (trackPrefab != null && !track.isHidden)
            {
                GameObject trackVisual = Instantiate(trackPrefab);
                trackVisual.name = $"Track_{track.trackId}";

                LineRenderer lr = trackVisual.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    // 设置宽度 (缩放)
                    lr.startWidth = track.width > 0 ? track.width : 0.1f;
                    lr.endWidth = lr.startWidth;

                    // 设置透明度
                    Color tColor = lr.startColor;
                    tColor.a = track.opacity;
                    lr.startColor = tColor;
                    lr.endColor = tColor;

                    // 设置发光材质
                    if (track.isGlow && glowMat != null) lr.material = glowMat;
                    else if (normalMat != null) lr.material = normalMat;

                    lr.SetPosition(0, new Vector3(track.startX, track.startY, 0));
                    lr.SetPosition(1, new Vector3(track.endX, track.endY, 0));
                }

                // 生成判定点指示器 (圆圈、方框等)
                if (judgeSprites != null && track.judgeShape >= 0 && track.judgeShape < judgeSprites.Length)
                {
                    GameObject judgeObj = new GameObject("JudgeIndicator");
                    judgeObj.transform.SetParent(trackVisual.transform);
                    judgeObj.transform.position = new Vector3(track.endX, track.endY, 0);
                    // 顺便让判定图也旋转对应的角度
                    judgeObj.transform.rotation = Quaternion.Euler(0, 0, track.rotation);

                    SpriteRenderer sr = judgeObj.AddComponent<SpriteRenderer>();
                    sr.sprite = judgeSprites[track.judgeShape];
                    sr.color = new Color(1, 1, 1, track.opacity);
                    if (track.isGlow && glowMat != null) sr.material = glowMat;
                }
            }
        }

        // --- 生成音符 ---
        float globalSpeedFactor = currentChart.chartSpeed / 2.0f;

        foreach (var note in currentChart.notes)
        {
            Entity noteEntity = entityManager.CreateEntity(
                typeof(NoteComponent), typeof(NotePosition), typeof(NoteViewComponent)
            );
            
            TrackData myTrack = trackDict[note.trackId]; 

            entityManager.SetComponentData(noteEntity, new NoteComponent
            {
                TrackId = note.trackId,
                TargetTime = note.time,
                Type = note.type,
                Speed = note.speed * globalSpeedFactor, 
                IsFake = note.isFake,
                Shape = note.shape, 
                StartPos = new float2(myTrack.startX, myTrack.startY),
                EndPos = new float2(myTrack.endX, myTrack.endY) 
            });

            entityManager.SetComponentData(noteEntity, new NotePosition { Value = new float2(myTrack.startX, myTrack.startY) });
            entityManager.SetComponentData(noteEntity, new NoteViewComponent { VisualTransform = null });
        }
    }
}