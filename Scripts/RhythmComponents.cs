using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct TrackComponent : IComponentData
{
    public int TrackId;
    public float2 StartPos;
    public float2 EndPos;
    public int CurveType;
    public float Amplitude;
    public float Frequency;
}

public struct NoteComponent : IComponentData
{
    public int TrackId;
    public float TargetTime;
    public int Type;
    public float Speed;
    public bool IsFake;
    
    public int Shape;
    
    // 轨迹信息，让ECS能快速计算方向
    public float2 StartPos;
    public float2 EndPos;
}

public struct NotePosition : IComponentData
{
    public float2 Value;
}

// 混合ECS特有组件：用来装GameObject的肉体
public class NoteViewComponent : IComponentData
{
    public Transform VisualTransform;
}