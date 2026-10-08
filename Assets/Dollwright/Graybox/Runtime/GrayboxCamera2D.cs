using UnityEngine;

namespace Dollwright.Graybox
{
    public sealed class GrayboxCamera2D : MonoBehaviour
    {
        public Transform target;
        public float worldMinX = -3f, worldMaxX = 159f;
        public float lookAhead = 1.8f, followSpeed = 6f, centerY = 3.25f;
        Camera view;
        void Awake() { view = GetComponent<Camera>(); }
        void LateUpdate()
        {
            if (target == null) return;
            float halfWidth = view.orthographicSize * view.aspect;
            float x = Mathf.Clamp(target.position.x + lookAhead,
                worldMinX + halfWidth, worldMaxX - halfWidth);
            float blend = 1 - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = new Vector3(Mathf.Lerp(transform.position.x, x, blend), centerY, -10);
        }
    }
}
