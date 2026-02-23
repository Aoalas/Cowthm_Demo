using System.Collections.Generic;

[System.Serializable]
public class TrackData
{
    public int trackId;
    public float startX; public float startY;
    public float endX; public float endY;
    
    // -- 新增：轨道视觉参数 --
    public float width = 0.1f;       // 轨道粗细 (缩放)
    public float opacity = 1.0f;     // 透明度 (0~1)
    public bool isGlow = false;      // 是否发光
    public float rotation = 0f;      // 整体旋转角度 (以起点为中心)
    public int judgeShape = 0;       // 判定线形状 (0=默认圆, 1=方框等)

    public int curveType;
    public float amplitude;
    public float frequency;
    public bool isHidden;
}

[System.Serializable]
public class NoteData
{
    public int trackId;
    public float time;
    public int type;
    public float speed;
    public bool isFake;

    // -- 新增：音符视觉参数 --
    public int shape = 0; // 音符形状 (0=默认圆, 1=方块, 2=特殊形状)
}

[System.Serializable]
public class ChartData
{
    public string songName = "Unknown Song";
    public float chartSpeed = 2.0f;
    
    public List<TrackData> tracks;
    public List<NoteData> notes;
}