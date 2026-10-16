using UnityEngine;

namespace Dollwright.Graybox
{
    public sealed class GrayboxFog2D : MonoBehaviour
    {
        public Transform player;
        public float revealAhead = 8f;
        public float curtainWidth = 200f;
        public bool revealCircusLandmark;
        public bool revealPushLessons = true;
        public float FurthestRevealed { get; private set; }
        void Start() { FurthestRevealed = player.position.x + revealAhead; }
        void LateUpdate()
        {
            FurthestRevealed = Mathf.Max(FurthestRevealed, player.position.x + revealAhead);
            // Show the destination before asking the player to commit to pushing.
            if (revealPushLessons && player.position.x >= 10f)
                FurthestRevealed = Mathf.Max(FurthestRevealed, 25.5f);
            if (revealCircusLandmark && player.position.x >= 49f)
                FurthestRevealed = Mathf.Max(FurthestRevealed, 73f);
            if (revealPushLessons && player.position.x >= 114f)
                FurthestRevealed = Mathf.Max(FurthestRevealed, 136f);
            transform.position = new Vector3(FurthestRevealed + curtainWidth / 2, 4, 0);
        }
    }
}
