using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Minity.Pooling; 

[UpdateBefore(typeof(NoteVisualSystem))]
public partial class NoteInputSystem : SystemBase
{
    private readonly KeyCode[] trackKeys = new KeyCode[] { KeyCode.S, KeyCode.D, KeyCode.J, KeyCode.K };

    protected override void OnUpdate()
    {
        // 【新增拦截】必须处于游玩状态才接受输入并结算 Miss
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        // 【修改时间源】
        float currentTime = GameManager.Instance.CurrentGameTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // ==========================================
        // 1. 移动端多点触控检测
        // ==========================================
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            // 只有当手指刚刚按下的那一帧，才触发判定
            if (touch.phase == TouchPhase.Began)
            {
                // 将屏幕宽度分为4等份，计算当前触摸点落在哪个轨道 (0, 1, 2, 3)
                float screenWidth = Screen.width;
                int trackId = (int)((touch.position.x / screenWidth) * 4);
                trackId = math.clamp(trackId, 0, 3); // 确保不越界

                TryHitNote(trackId, currentTime, ref ecb);
            }
        }

        // ==========================================
        // 2. 电脑键盘检测 (用于 Editor 测试)
        // ==========================================
        for (int trackId = 0; trackId < trackKeys.Length; trackId++)
        {
            if (Input.GetKeyDown(trackKeys[trackId]))
            {
                TryHitNote(trackId, currentTime, ref ecb);
            }
        }

        // ==========================================
        // 3. 自动 Miss 检测 (放过未击打的音符)
        // ==========================================
        foreach (var (note, view, pos, entity) in SystemAPI.Query<RefRO<NoteComponent>, NoteViewComponent, RefRO<NotePosition>>().WithEntityAccess())
        {
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
    }

    // 【重构】将判定逻辑抽离成独立方法，方便触摸和键盘共同调用
    private void TryHitNote(int trackId, float currentTime, ref EntityCommandBuffer ecb)
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
                float timeDiff = note.ValueRO.TargetTime - currentTime;
                if (timeDiff >= -0.15f && timeDiff <= 0.20f)
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
            JudgeHit(hitTimeDiff, hitShape, hitPos);

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
        catch (System.Exception e)
        {
            Debug.LogWarning($"获取打击特效失败: {e.Message}");
        }
    }
}