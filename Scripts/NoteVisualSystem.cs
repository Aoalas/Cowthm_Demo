using Unity.Entities;
using Unity.Mathematics; // 引入数学库计算距离
using UnityEngine;
using Milease.DSL;
using Minity.Pooling; 

[UpdateAfter(typeof(NoteMovementSystem))]
public partial class NoteVisualSystem : SystemBase
{

    protected override void OnUpdate()
    {
        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        
        foreach (var (note, pos, view) in SystemAPI.Query<RefRO<NoteComponent>, RefRO<NotePosition>, NoteViewComponent>())
        {
            float timeDiff = note.ValueRO.TargetTime - currentTime;

            // 【核心数学】：动态计算音符应该提前多久生成！
            // 1. 算出轨道的总长度
            float trackLength = math.distance(note.ValueRO.StartPos, note.ValueRO.EndPos);
            // 2. 根据该音符的实际速度，算出它跑完全程需要的时间
            float spawnTimeAhead = trackLength / note.ValueRO.Speed;

            // 1. 【生成表现】如果进入了它专属的提前生成时间，且还没过判定线
            if (view.VisualTransform == null && timeDiff > -0.15f && timeDiff <= spawnTimeAhead)
            {
                RhythmPoolID poolId = (RhythmPoolID)note.ValueRO.Shape;
        
                GameObject visualObj = ObjectPool.RequestGameObject(poolId);
                visualObj.SetActive(true);
        
                visualObj.transform.position = new Vector3(pos.ValueRO.Value.x, pos.ValueRO.Value.y, 0);
                visualObj.transform.localScale = Vector3.zero;

                (0.4f / visualObj.transform.MBackOut(t => t.localScale, Vector3.zero, new Vector3(1.0f, 1.0f, 1.0f))).Play();

                view.VisualTransform = visualObj.transform;
            }
        }
    }
}