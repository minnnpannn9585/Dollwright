using System.Collections.Generic;
using UnityEngine;
namespace Dollwright.Graybox
{
    [RequireComponent(typeof(GrayboxPlayer2D))]
    public sealed class DreamPhaseAbility : MonoBehaviour
    {
        public float duration = 1.3f;
        public bool IsPhasing => ignored.Count > 0;
        public int Activations { get; private set; }
        public int CurtainCrossings { get; private set; }
        float until, nextAllowed;
        Collider2D shape;
        GrayboxPlayer2D player;
        DreamPhaseBarrier[] barriers;
        readonly List<Collider2D> ignored = new List<Collider2D>();
        SpriteRenderer[] visuals;
        Color[] colors;
        bool previousPhase;
        void Awake()
        {
            player = GetComponent<GrayboxPlayer2D>();
            shape = GetComponent<Collider2D>();
            barriers = FindObjectsOfType<DreamPhaseBarrier>();
            RefreshVisuals();
        }
        public void RefreshVisuals()
        {
            visuals = GetComponentsInChildren<SpriteRenderer>();
            colors = new Color[visuals.Length];
            for (int i=0;i<visuals.Length;i++) colors[i]=visuals[i].color;
        }
        public bool Activate()
        {
            if (Time.time < nextAllowed || IsPhasing || player.run.IsRecovering || player.run.Completed) return false;
            until = Time.time + duration;
            nextAllowed = until + .15f;
            Activations++;
            foreach (var barrier in barriers)
            {
                if (barrier == null) continue;
                var collider = barrier.GetComponent<Collider2D>();
                Physics2D.IgnoreCollision(shape, collider, true);
                ignored.Add(collider);
            }
            return true;
        }
        void Update()
        {
            if (!player.InputOverride && Input.GetKeyDown(KeyCode.E)) Activate();
            if (Time.time >= until)
            {
                for (int i=ignored.Count-1;i>=0;i--)
                {
                    var barrier = ignored[i];
                    // Restore only after the capsule leaves the wall, never inside a solid barrier.
                    if (barrier != null && shape.Distance(barrier).isOverlapped) continue;
                    if (barrier != null) Physics2D.IgnoreCollision(shape, barrier, false);
                    ignored.RemoveAt(i);
                }
            }
            if (previousPhase != IsPhasing)
            {
                for(int i=0;i<visuals.Length;i++) visuals[i].color = IsPhasing ? new Color(.65f,.88f,1,.55f) : colors[i];
                previousPhase=IsPhasing;
            }
        }
        public void Cancel()
        {
            foreach(var barrier in ignored)if(barrier!=null&&shape!=null)Physics2D.IgnoreCollision(shape,barrier,false);
            ignored.Clear();until=nextAllowed=0;previousPhase=false;
            if(visuals!=null)for(int i=0;i<visuals.Length;i++)visuals[i].color=colors[i];
        }
        void OnDisable()
        {
            foreach(var barrier in ignored) if (barrier != null && shape != null) Physics2D.IgnoreCollision(shape,barrier,false);
            ignored.Clear();
        }
    }
}
