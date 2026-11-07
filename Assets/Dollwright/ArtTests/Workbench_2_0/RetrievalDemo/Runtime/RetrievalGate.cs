using UnityEngine;
namespace Dollwright.RetrievalDemo
{
    public sealed class RetrievalGate : MonoBehaviour
    {
        public Vector2 aperture = new Vector2(.58f, 1.80f); // depth, height in metres
        public float centerHeight = 1.8f;
        public string hint;
        public Renderer marker;
    }
}
