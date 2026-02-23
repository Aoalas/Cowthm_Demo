using UnityEngine;
using System;

public static class ScoreManager
{
    public static float CurrentScore { get; private set; }
    public static int Combo { get; private set; }
    public const int MAX_SCORE = 1000000;
    private static float scorePerNote;

    // 【新增】追踪谱面完成度
    private static int totalNotesCount;
    private static int processedNotesCount;

    public static Action<int, int> OnScoreUpdated;
    public static Action<string> OnGameStart; 
    public static Action OnGameComplete;

    public static void Init(int totalNotes, string songName) 
    {
        CurrentScore = 0f;
        Combo = 0;
        scorePerNote = totalNotes > 0 ? (float)MAX_SCORE / totalNotes : 0f;
        
        // 初始化进度
        totalNotesCount = totalNotes;
        processedNotesCount = 0;

        OnGameStart?.Invoke(songName);
        OnScoreUpdated?.Invoke(0, 0);
    }

    public static void AddHit(string judgment)
    {
        if (judgment == "Perfect") { CurrentScore += scorePerNote; Combo++; }
        else if (judgment == "Good") { CurrentScore += scorePerNote * 0.5f; Combo++; }
        else if (judgment == "Miss") { Combo = 0; }

        OnScoreUpdated?.Invoke(Mathf.RoundToInt(CurrentScore), Combo);

        // 【新增】每次判定后，处理数量+1。当处理完所有音符，触发结束事件
        processedNotesCount++;
        if (processedNotesCount >= totalNotesCount)
        {
            OnGameComplete?.Invoke();
        }
    }
}