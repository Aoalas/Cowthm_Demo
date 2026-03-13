using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class MagneticScrollRect : MonoBehaviour
{
    public RectTransform contentPanel;
    public ScrollRect scrollRect;

    [Header("外观控制 (Curved UI)")]
    public float centerOffset = 0f; // 中心点Y轴偏移量
    public float maxDistance = 500f; // 开始发生形变和透明度下降的最大距离
    public float unselectedScale = 0.85f; // 远离中心的卡片缩放比例
    public float unselectedAlpha = 0.35f; // 远离中心的卡片透明度
    public float scrollFocusSpeed = 8f; // 点击后居中的平滑移动速度

    private int itemCount;
    private RectTransform[] listItems;
    private CanvasGroup[] itemCanvasGroups;

    private bool isFocusing = false;
    private float targetY = 0f;

    void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (contentPanel == null) contentPanel = scrollRect.content;
        
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        
        scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
    }

    void Start()
    {
        Invoke("InitItems", 0.1f);
    }

    public void InitItems()
    {
        if (contentPanel == null) return;
        itemCount = contentPanel.childCount;
        listItems = new RectTransform[itemCount];
        itemCanvasGroups = new CanvasGroup[itemCount];

        for (int i = 0; i < itemCount; i++)
        {
            listItems[i] = contentPanel.GetChild(i).GetComponent<RectTransform>();
            itemCanvasGroups[i] = contentPanel.GetChild(i).GetComponent<CanvasGroup>();
        }
    }

    // 暴露给外部的安全锁，防止滑动时手滑点进歌曲
    public bool IsScrolling()
    {
        // 如果速度很快，或者正在被强制居中，都不允许点击
        return Mathf.Abs(scrollRect.velocity.y) > 30f || isFocusing;
    }

    private void OnScrollRectValueChanged(Vector2 pos)
    {
        // 一旦玩家滑动产生的速度变大，说明是玩家在强行接管，立刻打断自动寻路
        if (Mathf.Abs(scrollRect.velocity.y) > 10f)
        {
            isFocusing = false;
        }
    }

    void Update()
    {
        if (itemCount == 0 || listItems == null || listItems.Length != itemCount) return;

        float viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : ((RectTransform)scrollRect.transform).rect.height;
        float centerPosition = viewportHeight / 2f + centerOffset;

        for (int i = 0; i < itemCount; i++)
        {
            if (listItems[i] == null) continue;

            float itemY = listItems[i].position.y - transform.position.y + centerPosition;
            float distanceFromCenter = Mathf.Abs(itemY);

            float normalizedDist = Mathf.Clamp01(distanceFromCenter / maxDistance);
            float curve = 1f - normalizedDist;

            float scale = Mathf.Lerp(unselectedScale, 1f, curve);
            float alpha = Mathf.Lerp(unselectedAlpha, 1f, curve);

            listItems[i].localScale = new Vector3(scale, scale, 1f);
            
            if (itemCanvasGroups[i] != null) 
            {
                itemCanvasGroups[i].alpha = alpha;
            }
        }

        // 持续平滑追踪：通过 Update 手动推演，彻底摆脱一切原生组件动画的阻力打架！
        if (isFocusing)
        {
            float currentY = contentPanel.anchoredPosition.y;
            contentPanel.anchoredPosition = new Vector2(
                contentPanel.anchoredPosition.x, 
                Mathf.Lerp(currentY, targetY, Time.deltaTime * scrollFocusSpeed)
            );

            // 当足够接近时，停止寻路
            if (Mathf.Abs(contentPanel.anchoredPosition.y - targetY) < 1.0f)
            {
                contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x, targetY);
                isFocusing = false;
            }
        }
    }

    // 由 SongSelectManager 选中歌曲时调用
    public void FocusOnItem(int index)
    {
        if (index < 0 || index >= itemCount || listItems[index] == null) return;

        // 停止一切物理推力
        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;

        // 第一步：获取 Viewport（准星）在世界空间中的中心点绝对坐标！
        Vector3[] viewportCorners = new Vector3[4];
        (scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform).GetWorldCorners(viewportCorners);
        float viewportCenterWorldY = (viewportCorners[0].y + viewportCorners[1].y) / 2f + centerOffset; // 0和1是左下和左上，(0.y + 1.y)/2 就是中间

        // 第二步：获取 当前所选卡片 在世界空间中的中心点绝对坐标！
        Vector3[] itemCorners = new Vector3[4];
        listItems[index].GetWorldCorners(itemCorners);
        float itemCenterWorldY = (itemCorners[0].y + itemCorners[1].y) / 2f;

        // 第三步：算出这两个世界坐标高度差！(目标就是要弥补这个差值)
        float differenceWorldY = viewportCenterWorldY - itemCenterWorldY;

        // 第四步：把世界高度差转化为 Content 内部的局部滑动距离
        float differenceLocalY = differenceWorldY / contentPanel.lossyScale.y;

        // 得出真正的、绝对完美的理论目标Y！
        float calculatedTargetY = contentPanel.anchoredPosition.y + differenceLocalY;

        // 极限保护：检查是否到底了或者到顶了
        float viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : ((RectTransform)scrollRect.transform).rect.height;
        float contentHeight = contentPanel.rect.height;
        float maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);
        
        targetY = Mathf.Clamp(calculatedTargetY, 0f, maxScrollY);

        if (Mathf.Abs(contentPanel.anchoredPosition.y - targetY) > 1f)
        {
            isFocusing = true;
        }
    }
}
