using UnityEngine;

namespace FirstView
{
    /// <summary>
    /// Manages first-person camera transitions between predefined focus points.
    /// Uses SmoothDamp for buttery-smooth movement with idle breathing.
    /// </summary>
    public class FocusCameraRig : MonoBehaviour
    {
        [System.Serializable]
        public class FocusTarget
        {
            [Tooltip("Unique identifier for this focus point")]
            public string id;
            [Tooltip("Transform anchor defining camera position and rotation")]
            public Transform anchor;
            [Tooltip("Field of view when focused here")]
            [Range(20f, 90f)] public float fov = 50f;
            [Tooltip("Smooth time for transition (seconds)")]
            [Range(0.1f, 2f)] public float smoothTime = 0.5f;
        }

        [Header("Focus Targets")]
        [SerializeField] private FocusTarget[] targets;

        [Header("Idle Motion")]
        [SerializeField] private float breathAmplitude = 0.0008f;
        [SerializeField] private float breathSpeed = 0.8f;

        private Camera cam;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float targetFov;
        private Vector3 posVelocity;
        private float fovVelocity;
        private bool isTransitioning;
        private float breathPhase;

        public string CurrentFocusId { get; private set; }
        public int CurrentFocusIndex { get; private set; }
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = GetComponent<Camera>();
        }

        public void Initialize(string defaultFocusId)
        {
            CurrentFocusIndex = FindTargetIndex(defaultFocusId);
            FocusToImmediate(defaultFocusId);
        }

        public void FocusTo(string id)
        {
            FocusTarget target = FindTarget(id);
            if (target == null || target.anchor == null)
            {
                Debug.LogWarning("[FocusCameraRig] Target not found: " + id);
                return;
            }

            CurrentFocusId = id;
            CurrentFocusIndex = FindTargetIndex(id);
            targetPosition = target.anchor.position;
            targetRotation = target.anchor.rotation;
            targetFov = target.fov;
            isTransitioning = true;
            posVelocity = Vector3.zero;
            fovVelocity = 0f;
        }

        public void FocusNext()
        {
            if (targets == null || targets.Length == 0) return;
            int next = (CurrentFocusIndex + 1) % targets.Length;
            FocusTo(targets[next].id);
        }

        public void FocusPrev()
        {
            if (targets == null || targets.Length == 0) return;
            int prev = (CurrentFocusIndex - 1 + targets.Length) % targets.Length;
            FocusTo(targets[prev].id);
        }

        public void FocusToImmediate(string id)
        {
            FocusTarget target = FindTarget(id);
            if (target == null || target.anchor == null) return;

            CurrentFocusId = id;
            targetPosition = target.anchor.position;
            targetRotation = target.anchor.rotation;
            targetFov = target.fov;

            transform.SetPositionAndRotation(targetPosition, targetRotation);
            if (cam != null) cam.fieldOfView = targetFov;

            isTransitioning = false;
            posVelocity = Vector3.zero;
            fovVelocity = 0f;
            breathPhase = Random.Range(0f, 10f);
        }

        private void LateUpdate()
        {
            if (isTransitioning)
            {
                float smooth = GetCurrentSmoothTime();

                Vector3 newPos = Vector3.SmoothDamp(
                    transform.position, targetPosition, ref posVelocity, smooth);
                Quaternion newRot = Quaternion.Slerp(
                    transform.rotation, targetRotation, Time.deltaTime * (1f / smooth) * 2.5f);

                transform.SetPositionAndRotation(newPos, newRot);

                if (cam != null)
                {
                    cam.fieldOfView = Mathf.SmoothDamp(
                        cam.fieldOfView, targetFov, ref fovVelocity, smooth);
                }

                if (Vector3.SqrMagnitude(transform.position - targetPosition) < 0.000001f
                    && Quaternion.Angle(transform.rotation, targetRotation) < 0.01f)
                {
                    transform.SetPositionAndRotation(targetPosition, targetRotation);
                    isTransitioning = false;
                }
            }
            else
            {
                transform.position = targetPosition + CalculateBreath();
            }
        }

        private Vector3 CalculateBreath()
        {
            float bx = Mathf.Sin((Time.time + breathPhase) * breathSpeed * 0.7f) * breathAmplitude * 0.5f;
            float by = Mathf.Sin((Time.time + breathPhase) * breathSpeed) * breathAmplitude;
            return transform.right * bx + transform.up * by;
        }

        private FocusTarget FindTarget(string id)
        {
            if (targets == null) return null;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].id == id) return targets[i];
            }
            return null;
        }

        private int FindTargetIndex(string id)
        {
            if (targets == null) return 0;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].id == id) return i;
            }
            return 0;
        }

        private float GetCurrentSmoothTime()
        {
            FocusTarget t = FindTarget(CurrentFocusId);
            return t != null ? t.smoothTime : 0.5f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (targets == null) return;
            foreach (var t in targets)
            {
                if (t.anchor == null) continue;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(t.anchor.position, 0.05f);
                Gizmos.DrawRay(t.anchor.position, t.anchor.forward * 0.3f);
            }
        }
#endif
    }
}
