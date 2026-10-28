using UnityEngine;
namespace Dollwright.Graybox
{
    public sealed class Dream02Portal : MonoBehaviour
    {
        public string nextScene;
        public bool prototypeEnd;
        void OnTriggerEnter2D(Collider2D other)
        {
            var player=other.GetComponent<GrayboxPlayer2D>();
            if (player==null || player.run.Rewind.IsRewinding) return;
            if (prototypeEnd) FindObjectOfType<Dream02Guide>().ReachedEnd = true;
            else DreamTravel.MoveWithinDream(nextScene,player.run);
        }
    }
}
