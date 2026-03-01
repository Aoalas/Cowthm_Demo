using UnityEngine;
using System;

public static class ScoreManager
{
    public static float CurrentScore { get; private set; }
    public static int Combo { get; private set; }
    public static int MaxCombo { get; private set; }
    
    public static int PerfectCount { get; private set; }
    public static int GoodCount { get; private set; }
    public static int MissCount { get; private set; }

    public const int MAX_SCORE = 1000000;
    private static float scorePerNote;

    private static int totalNotesCount;
    private static int processedNotesCount;

    public static Action<int, int> OnScoreUpdated;
    public static Action<string, ChartUIConfig> OnGameStart;
    public static Action OnGameComplete;

    public static void Init(int totalNotes, string songName, ChartUIConfig uiConfig)
    {
        CurrentScore = 0f;
        Combo = 0;
        MaxCombo = 0;
        PerfectCount = 0;
        GoodCount = 0;
        MissCount = 0;
        
        scorePerNote = totalNotes > 0 ? (float)MAX_SCORE / totalNotes : 0f;
        totalNotesCount = totalNotes;
        processedNotesCount = 0;

        OnGameStart?.Invoke(songName, uiConfig);
        OnScoreUpdated?.Invoke(0, 0);
    }

    public static void AddHit(string judgment)
    {
        if (judgment == "Perfect") 
        { 
            CurrentScore += scorePerNote; 
            Combo++; 
            PerfectCount++; 
        }
        else if (judgment == "Good") 
        { 
            CurrentScore += scorePerNote * 0.5f; 
            Combo++; 
            GoodCount++; 
        }
        else if (judgment == "Miss") 
        { 
            Combo = 0; 
            MissCount++; 
        }
        
        if (Combo > MaxCombo) MaxCombo = Combo;

        OnScoreUpdated?.Invoke(Mathf.RoundToInt(CurrentScore), Combo);

        processedNotesCount++;
        if (processedNotesCount >= totalNotesCount)
        {
            OnGameComplete?.Invoke();
        }
    }
}