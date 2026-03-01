using System.Collections.Generic;

[System.Serializable]
public class ChartUIConfig
{
    public bool hideCombo = false;
    public bool disableComplete = false;
}

[System.Serializable]
public class TrackData
{
    public int trackId;
    public float startX; public float startY;
    public float endX; public float endY;
    
    public float width = 0.1f;
    public float opacity = 1.0f;
    public bool isGlow = false;
    public float rotation = 0f;
    public int judgeShape = 0;

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
    
    public int shape = 0;
}

[System.Serializable]
public class ChartData
{
    public string chartId = "default_01";
    public string songName = "Unknown Song";
    public string artist = "Unknown Artist";
    public float bpm = 120.0f;
    public float chartSpeed = 2.0f;
    
    public float musicVolume = 1.0f;
    
    public bool requireAudio = false;
    
    public ChartUIConfig uiConfig;
    
    public List<TrackData> tracks;
    public List<NoteData> notes;
}