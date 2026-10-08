using UnityEngine;

namespace Dollwright.Graybox
{
    public sealed class GrayboxFog2D : MonoBehaviour
    {
        public Transform player;
        public float revealAhead = 8f;
        public float curtainWidth = 200f;
        public float FurthestRevealed { get; private set; }
        void Start() { FurthestRevealed = player.position.x + revealAhead; }
        void LateUpdate()
        {
            FurthestRevealed = Mathf.Max(FurthestRevealed, player.position.x + revealAhead);
            transform.position = new Vector3(FurthestRevealed + curtainWidth / 2, 4, 0);
        }
    }
}
