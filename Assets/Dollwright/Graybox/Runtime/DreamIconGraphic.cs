using UnityEngine;
using UnityEngine.UI;

namespace Dollwright.Graybox
{
    // Editable UI geometry: no external icon/font dependency for the bed and doll silhouettes.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DreamIconGraphic : MaskableGraphic
    {
        public enum Icon { Bed, Doll }
        public Icon icon;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (icon == Icon.Bed)
            {
                Quad(mesh, .10f, .16f, .07f, .65f, color);
                Quad(mesh, .84f, .16f, .06f, .49f, color);
                Quad(mesh, .15f, .30f, .73f, .10f, color);
                Quad(mesh, .18f, .42f, .63f, .19f, color);
                Quad(mesh, .21f, .63f, .20f, .09f, color);
            }
            else
            {
                Circle(mesh, new Vector2(.5f, .76f), new Vector2(.18f, .19f), color);
                Quad(mesh, .36f, .23f, .28f, .37f, color);
                Quad(mesh, .23f, .31f, .10f, .25f, color);
                Quad(mesh, .67f, .31f, .10f, .25f, color);
                Quad(mesh, .36f, .10f, .10f, .16f, color);
                Quad(mesh, .54f, .10f, .10f, .16f, color);
                var ink = new Color(.26f, .29f, .32f);
                Circle(mesh, new Vector2(.44f, .77f), Vector2.one * .018f, ink);
                Circle(mesh, new Vector2(.56f, .77f), Vector2.one * .018f, ink);
            }
        }
        Vector3 Point(float x, float y)
        {
            var r = rectTransform.rect;
            return new Vector3(r.xMin + r.width * x, r.yMin + r.height * y, 0);
        }
        void Quad(VertexHelper mesh, float x, float y, float w, float h, Color tint)
        {
            int i = mesh.currentVertCount;
            mesh.AddVert(Point(x, y), tint, Vector2.zero);
            mesh.AddVert(Point(x, y+h), tint, Vector2.zero);
            mesh.AddVert(Point(x+w, y+h), tint, Vector2.zero);
            mesh.AddVert(Point(x+w, y), tint, Vector2.zero);
            mesh.AddTriangle(i, i+1, i+2); mesh.AddTriangle(i, i+2, i+3);
        }
        void Circle(VertexHelper mesh, Vector2 center, Vector2 radius, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(Point(center.x, center.y), tint, Vector2.zero);
            const int sides = 24;
            for (int i=0; i<sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                mesh.AddVert(Point(center.x + Mathf.Cos(a)*radius.x, center.y + Mathf.Sin(a)*radius.y), tint, Vector2.zero);
            }
            for (int i=0; i<sides; i++) mesh.AddTriangle(start, start+1+i, start+1+(i+1)%sides);
        }
    }
}
