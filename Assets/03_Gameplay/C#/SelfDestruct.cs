using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    // 1秒后自动毁灭，防止飞出屏幕后还在消耗性能
    public float lifetime = 1.0f; 

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}