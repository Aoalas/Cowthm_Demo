using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Minity.Pooling; 

[UpdateBefore(typeof(NoteVisualSystem))]
public partial class NoteInputSystem : SystemBase
{
    private readonly KeyCode[] trackKeys = new KeyCode[] { KeyCode.S, KeyCode.D, KeyCode.J, KeyCode.K };

    // 定义一个轨道信息结构体，用于存储向量
    private struct TrackInfo
    {
        public float2 StartPos;
        public float2 EndPos;
        public float2 Dir;  // 轨道方向向量
        public float2 Norm; // 垂直于轨道的法向向量
    }

    protected override void OnUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        float currentTime = GameManager.Instance.CurrentAudioTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        NativeHashSet<Entity> processedEntities = new NativeHashSet<Entity>(32, Allocator.Temp);

        // ==========================================
        // 【核心数学升级】：计算每条轨道的方向向量与法向量
        // ==========================================
        NativeArray<TrackInfo> trackInfos = new NativeArray<TrackInfo>(4, Allocator.Temp);
        
        // 赋默认值防错
        for (int i = 0; i < 4; i++)
        {
            trackInfos[i] = new TrackInfo { 
                StartPos = new float2(-3 + i*2, 6), 
                EndPos = new float2(-3 + i*2, -3), 
                Dir = new float2(0, 1), 
                Norm = new float2(1, 0) 
            };
        }

        foreach (var track in SystemAPI.Query<RefRO<TrackComponent>>())
        {
            int tId = track.ValueRO.TrackId;
            if(tId >= 0 && tId < 4)
            {
                float2 s = track.ValueRO.StartPos;
                float2 e = track.ValueRO.EndPos;
                
                // 算出沿着轨道的方向向量 (从终点指向起点)
                float2 dir = math.normalizesafe(s - e); 
                // 算出垂直于轨道的法向量
                float2 norm = new float2(-dir.y, dir.x); 
                
                trackInfos[tId] = new TrackInfo { StartPos = s, EndPos = e, Dir = dir, Norm = norm };
            }
        }

        Camera mainCam = Camera.main;
        


        float maxTransverseDist = 1.20f; 
        
        // 纵向长度：沿着轨道方向的距离。设为 2.5 甚至 3.0，
        float maxLongitudinalDist = 6.0f; 

        // 移动端多点触控精准检测
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && mainCam != null)
            {
                Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, Mathf.Abs(mainCam.transform.position.z)));
                float2 touchPos = new float2(worldPos.x, worldPos.y);

                int hitTrackId = -1;
                float minTransDist = float.MaxValue;

                // 判断手指到底属于哪条轨道
                for (int t = 0; t < 4; t++)
                {
                    // 算出判定线到手指的向量
                    float2 v = touchPos - trackInfos[t].EndPos;
                    
                    // 利用点乘(Dot)将距离拆分为横向偏移和纵向偏移
                    float transDist = math.abs(math.dot(v, trackInfos[t].Norm)); // 离轨道左右偏了多少
                    float longDist = math.abs(math.dot(v, trackInfos[t].Dir));   // 离判定线上下偏了多少

                    // 必须同时满足在一个“细长的长方形”区域内
                    if (transDist <= maxTransverseDist && longDist <= maxLongitudinalDist)
                    {
                        // 如果同时在两个轨道的重合边缘，优先判定给横向更靠近的那条轨道
                        if (transDist < minTransDist)
                        {
                            minTransDist = transDist;
                            hitTrackId = t;
                        }
                    }
                }

                if (hitTrackId != -1)
                {
                    TryHitNote(hitTrackId, currentTime, ref ecb, ref processedEntities);
                }
            }
        }

        // 2. 电脑键盘检测
        for (int trackId = 0; trackId < trackKeys.Length; trackId++)
        {
            if (Input.GetKeyDown(trackKeys[trackId]))
            {
                TryHitNote(trackId, currentTime, ref ecb, ref processedEntities);
            }
        }

        // 3. 自动 Miss 检测
        foreach (var (note, view, pos, entity) in SystemAPI.Query<RefRO<NoteComponent>, NoteViewComponent, RefRO<NotePosition>>().WithEntityAccess())
        {
            if (processedEntities.Contains(entity)) continue;

            float timeDiff = note.ValueRO.TargetTime - currentTime;
            if (timeDiff < -0.15f)
            {
                ScoreManager.AddHit("Miss");
                Vector3 missPos = new Vector3(pos.ValueRO.Value.x, pos.ValueRO.Value.y, 0);
                PlayHitEffect(note.ValueRO.Shape, missPos, new Color(1f, 0.6f, 0.6f, 1f));

                if (view != null && view.VisualTransform != null)
                {
                    ObjectPool.ReturnToPool(view.VisualTransform.gameObject);
                    view.VisualTransform = null;
                }
                ecb.DestroyEntity(entity);
            }
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
        processedEntities.Dispose(); 
        trackInfos.Dispose(); 
    }

    private void TryHitNote(int trackId, float currentTime, ref EntityCommandBuffer ecb, ref NativeHashSet<Entity> processedEntities)
    {
        Entity earliestNote = Entity.Null;
        float minTargetTime = float.MaxValue;
        float hitTimeDiff = 0f;
        NoteViewComponent hitView = null;
        int hitShape = 0; 
        Vector3 hitPos = Vector3.zero;

        foreach (var (note, view, pos, entity) in SystemAPI.Query<RefRO<NoteComponent>, NoteViewComponent, RefRO<NotePosition>>().WithEntityAccess())
        {
            if (note.ValueRO.TrackId == trackId)
            {
                if (processedEntities.Contains(entity)) continue;

                float timeDiff = note.ValueRO.TargetTime - currentTime;
                if (timeDiff >= -0.15f && timeDiff <= 0.2f)
                {
                    if (note.ValueRO.TargetTime < minTargetTime)
                    {
                        minTargetTime = note.ValueRO.TargetTime;
                        earliestNote = entity;
                        hitTimeDiff = timeDiff;
                        hitView = view;
                        hitShape = note.ValueRO.Shape;
                        hitPos = new Vector3(pos.ValueRO.Value.x, pos.ValueRO.Value.y, 0);
                    }
                }
            }
        }

        if (earliestNote != Entity.Null)
        {
            processedEntities.Add(earliestNote);

            try { JudgeHit(hitTimeDiff, hitShape, hitPos); }
            catch (System.Exception e) { Debug.LogWarning($"UI异常已拦截: {e.Message}"); }

            if (hitView != null && hitView.VisualTransform != null)
            {
                ObjectPool.ReturnToPool(hitView.VisualTransform.gameObject);
                hitView.VisualTransform = null;
            }
            ecb.DestroyEntity(earliestNote);
        }
    }

    private void JudgeHit(float timeDiff, int shape, Vector3 pos)
    {
        float absDiff = Mathf.Abs(timeDiff);
        string judgment;
        Color effectColor;

        if (absDiff <= 0.07f) { judgment = "Perfect"; effectColor = new Color(0.8f, 0.6f, 1f, 1f); }
        else if (absDiff <= 0.15f) { judgment = "Good"; effectColor = new Color(0.6f, 1f, 0.6f, 1f); }
        else { judgment = "Miss"; effectColor = new Color(1f, 0.6f, 0.6f, 1f); }

        ScoreManager.AddHit(judgment);
        PlayHitEffect(shape, pos, effectColor);
    }

    private void PlayHitEffect(int shape, Vector3 pos, Color color)
    {
        if (shape > 1) { shape = 0; }
        RhythmPoolID poolId = (RhythmPoolID)(shape + 3); 
        try
        {
            GameObject effectObj = ObjectPool.RequestGameObject(poolId);
            if (effectObj == null) return;
            effectObj.SetActive(true);
            effectObj.transform.position = pos;
            SpriteRenderer sr = effectObj.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.color = color; }
        }
        catch (System.Exception e) { Debug.LogWarning($"获取打击特效失败: {e.Message}"); }
    }
}