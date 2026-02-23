using UnityEngine;
using Minity.Pooling;

public class EffectRecycler : MonoBehaviour
{
    public float lifeTime = 0.2f; // 存活半秒钟
    private float timer;
    
    private SpriteRenderer sr;
    private Vector3 startScale = new Vector3(0.15f, 0.15f, 0.15f); // 初始大小
    private Vector3 endScale = new Vector3(0.25f, 0.25f, 0.25f);   // 最终放大到的微微膨胀大小

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        timer = 0f;
        transform.localScale = startScale;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifeTime;
        
        // 1. 缓慢放大效果 (EaseOut曲线让膨胀更有力)
        float easeOutProgress = 1f - (1f - progress) * (1f - progress);
        transform.localScale = Vector3.Lerp(startScale, endScale, easeOutProgress);

        // 2. 颜色透明度淡出效果
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - progress; // Alpha 值从 1 逐渐降到 0
            sr.color = c;
        }

        // 3. 寿命耗尽，把自己扔回回收站
        if (timer >= lifeTime)
        {
            ObjectPool.ReturnToPool(gameObject);
        }
    }
}