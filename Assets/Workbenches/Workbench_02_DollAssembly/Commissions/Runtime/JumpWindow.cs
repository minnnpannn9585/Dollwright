using UnityEngine;
namespace Dollwright.RetrievalDemo
{
    struct JumpWindow
    {
        float queued,grace;
        public bool Consume(bool pressed,bool grounded,float verticalSpeed)
        {
            float dt=Time.fixedDeltaTime;
            queued=pressed?.12f:Mathf.Max(0,queued-dt);
            grace=grounded && verticalSpeed<=.1f?.09f:Mathf.Max(0,grace-dt);
            if(queued<=0 || grace<=0)return false;
            queued=grace=0;
            return true;
        }
    }
}
