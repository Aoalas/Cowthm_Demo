using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// SystemBase 是 ECS 系统的基类
public partial class NoteMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float currentTime = (float)SystemAPI.Time.ElapsedTime;

        foreach (var (note, pos, view) in SystemAPI.Query<RefRO<NoteComponent>, RefRW<NotePosition>, NoteViewComponent>())
        {
            float timeDiff = note.ValueRO.TargetTime - currentTime;
            float2 direction = math.normalizesafe(note.ValueRO.StartPos - note.ValueRO.EndPos);
            float2 currentPos = note.ValueRO.EndPos + direction * (timeDiff * note.ValueRO.Speed);
            
            pos.ValueRW.Value = currentPos;

            // 【重点修改】只有当视觉实体真正存在时，才去同步它的 Transform 坐标
            if (view.VisualTransform != null)
            {
                view.VisualTransform.position = new Vector3(currentPos.x, currentPos.y, 0);
            }
        }
    }
}