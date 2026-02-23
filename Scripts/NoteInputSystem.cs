using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Minity.Pooling; // 对象池

// 保证输入判定在视觉更新之前执行，一旦判定命中，视觉表现立马被回收
[UpdateBefore(typeof(NoteVisualSystem))]
public partial class NoteInputSystem : SystemBase
{
    // 假设这是一个4K下落式音游，轨道 0,1,2,3 对应键盘 S,D,J,K
    // 等后续移植手机端，只需要把这里的按键检测换成屏幕点击区域的检测即可
    private readonly KeyCode[] trackKeys = new KeyCode[] { KeyCode.S, KeyCode.D, KeyCode.J, KeyCode.K };

    protected override void OnUpdate()
    {
        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // ==========================================
        // 1. 玩家主动击打检测 (循环每条轨道，原生支持多押)
        // ==========================================
        for (int trackId = 0; trackId < trackKeys.Length; trackId++)
        {
            if (Input.GetKeyDown(trackKeys[trackId]))
            {
                Entity earliestNote = Entity.Null;
                float minTargetTime = float.MaxValue;
                float hitTimeDiff = 0f;
                NoteViewComponent hitView = null;

                // 遍历所有音符，寻找【当前按下的轨道上】、【时间最靠前】、且【在可击打区间内】的音符
                foreach (var (note, view, entity) in SystemAPI.Query<RefRO<NoteComponent>, NoteViewComponent>().WithEntityAccess())
                {
                    if (note.ValueRO.TrackId == trackId)
                    {
                        // 计算时间差 = 音符目标时间 - 当前时间
                        // 正数代表音符还没到（提前打），负数代表音符已经过了（滞后打）
                        float timeDiff = note.ValueRO.TargetTime - currentTime;

                        // 判定区间：提前 200ms (+0.20f) 到 滞后 150ms (-0.15f) 
                        if (timeDiff >= -0.15f && timeDiff <= 0.20f)
                        {
                            // 寻找最旧的那个音符（防止两个音符靠得很近时，打中后面的）
                            if (note.ValueRO.TargetTime < minTargetTime)
                            {
                                minTargetTime = note.ValueRO.TargetTime;
                                earliestNote = entity;
                                hitTimeDiff = timeDiff;
                                hitView = view;
                            }
                        }
                    }
                }

                // 如果找到了有效音符，进行成绩结算并销毁
                if (earliestNote != Entity.Null)
                {
                    JudgeHit(hitTimeDiff);

                    // 剥离肉体还给对象池
                    if (hitView != null && hitView.VisualTransform != null)
                    {
                        ObjectPool.ReturnToPool(hitView.VisualTransform.gameObject);
                        hitView.VisualTransform = null;
                    }
                    // 销毁灵魂
                    ecb.DestroyEntity(earliestNote);
                }
            }
        }

        // ==========================================
        // 2. 自动 Miss 检测 (放过未击打的、直接漏掉的音符)
        // ==========================================
        foreach (var (note, view, entity) in SystemAPI.Query<RefRO<NoteComponent>, NoteViewComponent>().WithEntityAccess())
        {
            float timeDiff = note.ValueRO.TargetTime - currentTime;
            
            // 如果滞后超过 150ms，玩家已经无法挽回了，自动判定为 Miss
            if (timeDiff < -0.15f)
            {
                ScoreManager.AddHit("Miss");

                if (view != null && view.VisualTransform != null)
                {
                    ObjectPool.ReturnToPool(view.VisualTransform.gameObject);
                    view.VisualTransform = null;
                }
                ecb.DestroyEntity(entity);
            }
        }

        // 统一执行所有销毁操作
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    // 精确的评价判定逻辑
    private void JudgeHit(float timeDiff)
    {
        float absDiff = Mathf.Abs(timeDiff);

        if (absDiff <= 0.07f) // 正负 70ms 
        {
            ScoreManager.AddHit("Perfect");
        }
        else if (absDiff <= 0.15f) // 正负 150ms
        {
            ScoreManager.AddHit("Good");
        }
        else // 大于 150ms 且小于等于 200ms (只有提前击打才会进入这里)
        {
            ScoreManager.AddHit("Miss"); // 防糊惩罚：打得太早，直接算 Miss
        }
    }
}