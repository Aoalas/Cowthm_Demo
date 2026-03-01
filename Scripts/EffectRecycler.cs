using UnityEngine;
using Minity.Pooling;

public class EffectRecycler : MonoBehaviour
{
    public float lifeTime = 0.2f;
    private float timer;
    
    private SpriteRenderer sr;
    private Vector3 startScale = new Vector3(0.12f, 0.12f, 0.12f);
    private Vector3 endScale = new Vector3(0.22f, 0.22f, 0.22f);

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
        
        float easeOutProgress = 1f - (1f - progress) * (1f - progress);
        transform.localScale = Vector3.Lerp(startScale, endScale, easeOutProgress);
        
        if (sr != null)
        {
            Color c = sr.color;
            c.a = (1f - progress) * (1f - progress);
            sr.color = c;
        }
        
        if (timer >= lifeTime)
        {
            ObjectPool.ReturnToPool(gameObject);
        }
    }
}