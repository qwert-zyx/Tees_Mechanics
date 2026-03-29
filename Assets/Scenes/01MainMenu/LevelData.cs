using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "RhythmGame/LevelData")]
public class LevelData : ScriptableObject
{
    public string levelName;      
    public AudioClip musicClip;   
    public List<NoteData> notes;  
    
    // 【新增】：这一关打完后，下一关是谁？
    public LevelData nextLevel;   
}

[System.Serializable]
public class NoteData 
{
    public float time; 
    public int type;   
}