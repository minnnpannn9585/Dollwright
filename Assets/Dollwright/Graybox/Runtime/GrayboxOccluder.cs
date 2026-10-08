using UnityEngine;

namespace Dollwright.Graybox
{
    public sealed class GrayboxOccluder : MonoBehaviour
    {
        public Transform player;
        public float revealDistance = 3f;
        SpriteRenderer view;
        void Awake() { view = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            var color = view.color;
            float alpha = Mathf.Abs(player.position.x - transform.position.x) < revealDistance ? 0.12f : 1f;
            color.a = Mathf.MoveTowards(color.a, alpha, Time.deltaTime * 3);
            view.color = color;
        }
    }
}
