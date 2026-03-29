using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "RhythmGame/LevelData")]
public class LevelData : ScriptableObject
{
    public string levelName;      
    public AudioClip musicClip;   
    public List<NoteData> notes;  
    public LevelData nextLevel;   
    
    // 【新增】：这关是否需要触发新手引导？
    public bool isTutorial = false; 
}

[System.Serializable]
public class NoteData 
{
    public float time; 
    public int type;   
}