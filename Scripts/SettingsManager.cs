using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    
    public float GlobalVolume { get; private set; } = 1.0f;
    public float AudioOffset { get; private set; } = 0.0f;
    public float VisualOffset { get; private set; } = 0.0f;
    public int PauseMode { get; private set; } = 0;

    public float GlobalScrollSpeed { get; private set; } = 1.0f;
    
    void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            LoadSettings(); 
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }


    public void LoadSettings()
    {
        GlobalVolume = PlayerPrefs.GetFloat("GlobalVolume", 1.0f);
        AudioOffset = PlayerPrefs.GetFloat("AudioOffset", 0.0f);
        VisualOffset = PlayerPrefs.GetFloat("VisualOffset", 0.0f);
        PauseMode = PlayerPrefs.GetInt("PauseMode", 0);
        GlobalScrollSpeed = PlayerPrefs.GetFloat("GlobalScrollSpeed", 1.0f);
        
        ApplyVolume();
    }


    public void SaveSettings(float vol, float aOff, float vOff, int pMode, float scrollSpd)
    {
        GlobalVolume = vol;
        AudioOffset = aOff;
        VisualOffset = vOff;
        PauseMode = pMode;
        GlobalScrollSpeed = scrollSpd;

        PlayerPrefs.SetFloat("GlobalVolume", vol);
        PlayerPrefs.SetFloat("AudioOffset", aOff);
        PlayerPrefs.SetFloat("VisualOffset", vOff);
        PlayerPrefs.SetInt("PauseMode", pMode);
        PlayerPrefs.SetFloat("GlobalScrollSpeed", scrollSpd); // 【保存】
        
        PlayerPrefs.Save();
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        AudioListener.volume = GlobalVolume;
    }
}