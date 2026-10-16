using UnityEngine;

namespace Dollwright.Graybox
{
    public sealed class GrayboxTrigger : MonoBehaviour
    {
        public enum Action { Message, Checkpoint, Exit }
        public Action action;
        [TextArea] public string message;
        public Vector2 checkpoint;
        bool visited;
        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<GrayboxPlayer2D>();
            if (player == null) return;
            if (action == Action.Exit) { player.run.SetExitAvailable(true); return; }
            if (visited) return;
            visited = true;
            if (action == Action.Checkpoint) player.run.SetCheckpoint(checkpoint, message);
            else player.run.ShowMessage(message, 5);
        }
        void OnTriggerExit2D(Collider2D other)
        {
            if (action != Action.Exit) return;
            var player = other.GetComponent<GrayboxPlayer2D>();
            if (player != null) player.run.SetExitAvailable(false);
        }
    }
}
