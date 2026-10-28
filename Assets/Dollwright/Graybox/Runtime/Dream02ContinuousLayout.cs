using UnityEngine;

namespace Dollwright.Graybox
{
    // Region coordinates for authoring and testing; all three regions share a single live run.
    public sealed class Dream02ContinuousLayout : MonoBehaviour
    {
        public Transform magicStage, sawSection, doubleSection;
        public float sawLocalLeft, doubleLocalLeft;
        public float stageRevealAhead=5.5f, sawRevealAhead=7, doubleRevealAhead=5.5f;
        public Vector2 SawOrigin => sawSection.position;
        public Vector2 DoubleOrigin => doubleSection.position;
        public float SawStartX => sawSection.position.x+sawLocalLeft;
        public float DoubleStartX => doubleSection.position.x+doubleLocalLeft;
        Dream02Guide guide;
        GrayboxFog2D fog;
        GrayboxRun run;
        void Awake()
        {
            run=GetComponent<GrayboxRun>();guide=GetComponent<Dream02Guide>();
            fog=FindObjectOfType<GrayboxFog2D>();
        }
        void Update()
        {
            float x=run.player.transform.position.x;
            int section=x>=DoubleStartX?4:x>=SawStartX?3:2;
            if(guide!=null)guide.section=section;
            if(fog!=null)fog.revealAhead=section==4?doubleRevealAhead:section==3?sawRevealAhead:stageRevealAhead;
        }
    }
}

