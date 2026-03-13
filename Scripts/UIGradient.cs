using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    public Color colorTop = Color.white;
    public Color colorBottom = new Color(0.3f, 0.1f, 0.6f); // 暗紫色

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        var count = vh.currentVertCount;
        if (count == 0) return;

        var vertex = new UIVertex();
        vh.PopulateUIVertex(ref vertex, 0);
        var bottomY = vertex.position.y;
        var topY = vertex.position.y;

        for (int i = 1; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            var y = vertex.position.y;
            if (y > topY) topY = y;
            else if (y < bottomY) bottomY = y;
        }

        var height = topY - bottomY;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            var normalizedY = (vertex.position.y - bottomY) / height;
            vertex.color = Color32.Lerp(colorBottom, colorTop, normalizedY);
            vh.SetUIVertex(vertex, i);
        }
    }
}
