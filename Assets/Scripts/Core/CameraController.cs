using System.Collections;
using UnityEngine;
using DUT.Data;
using DUT.Core;

namespace DUT.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Refs")]
        public Transform pivot;

        [Header("Orbit")]
        public float orbitSpeed  = 180f;
        public float minPitch    = 10f;
        public float maxPitch    = 80f;

        [Header("Zoom")]
        public float minDist     = 25f;
        public float maxDist     = 1200f;

        [Header("Pan")]
        public float panSpeed    = 0.008f;

        [Header("Fly")]
        public float flyDuration = 0.8f;
        public float flyTargetDist = 120f;

        float _yaw, _pitch, _dist;
        bool  _flying;
        Coroutine _flyCo;

        // Overview defaults
        Vector3 _ovPivot;
        float   _ovDist = 420f, _ovPitch = 28f, _ovYaw = 45f;

        // Drag detection
        Vector2 _mouseDownPos;
        const float DragThreshold = 5f;

        Camera _cam;
        BuildingDataStore _store;

        void Start()
        {
            _cam   = GetComponent<Camera>();
            _store = Resources.Load<BuildingDataStore>("BuildingDataStore");
            if (_store == null) _store = FindFirstObjectByType<BuildingDataStore>();

            if (pivot == null)
            {
                var p = GameObject.Find("CameraPivot");
                pivot = p != null ? p.transform : new GameObject("CameraPivot").transform;
                pivot.position = new Vector3(370, 0, 320);
            }

            _ovPivot = pivot.position;
            _yaw = _ovYaw; _pitch = _ovPitch; _dist = _ovDist;
            ApplyTransform();
        }

        void LateUpdate()
        {
            if (_flying) return;
            HandleInput();
            ApplyTransform();
        }

        void HandleInput()
        {
            // ── Orbit: RMB ──
            if (Input.GetMouseButton(1))
            {
                _yaw   += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                _pitch -= Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
                _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            // ── Zoom: scroll ──
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                _dist = Mathf.Clamp(_dist - scroll * _dist * 0.4f, minDist, maxDist);

            // ── Track LMB press ──
            if (Input.GetMouseButtonDown(0))
                _mouseDownPos = Input.mousePosition;

            // ── Pan: MMB luôn, LMB chỉ khi kéo đủ xa ──
            float dragDist = Vector2.Distance(Input.mousePosition, _mouseDownPos);
            bool lmbHeld = Input.GetMouseButton(0);
            bool mmb     = Input.GetMouseButton(2);

            if (mmb || (lmbHeld && dragDist > DragThreshold && !OverSidebar()))
            {
                float s = _dist * panSpeed;
                pivot.position += transform.right * (-Input.GetAxis("Mouse X") * s)
                                + transform.up    * (-Input.GetAxis("Mouse Y") * s);
            }

            // ── Click: LMB release + di chuyển ít ──
            if (Input.GetMouseButtonUp(0) && dragDist < DragThreshold && !OverSidebar())
                TrySelect();

            // ── Keyboard ──
            float ks = _dist * 1.5f * Time.deltaTime;
            Vector3 fwd = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    pivot.position += fwd * ks;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  pivot.position -= fwd * ks;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  pivot.position -= transform.right * ks;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) pivot.position += transform.right * ks;
            if (Input.GetKeyDown(KeyCode.F))      ReturnToOverview();
            if (Input.GetKeyDown(KeyCode.Escape)) { _store?.ClearSelection(); ReturnToOverview(); }
        }

        void TrySelect()
        {
            if (_store == null) _store = FindFirstObjectByType<BuildingDataStore>();
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 2000f))
            {
                var bo = hit.collider.GetComponentInParent<BuildingObject>();
                if (bo != null)
                {
        _store?.SelectBuilding(bo.buildingId);
                    return;
                }
            }
            else _store?.ClearSelection();
        }

        void ApplyTransform()
        {
            var rot = Quaternion.Euler(_pitch, _yaw, 0);
            transform.position = pivot.position + rot * Vector3.back * _dist;
            transform.LookAt(pivot.position);
        }

        public void FlyTo(Vector3 pos, float dist = -1)
        {
            if (_flyCo != null) StopCoroutine(_flyCo);
            _flyCo = StartCoroutine(Co_Fly(pos, dist > 0 ? dist : flyTargetDist, -1, -1));
        }

        public void ReturnToOverview()
        {
            if (_flyCo != null) StopCoroutine(_flyCo);
            _flyCo = StartCoroutine(Co_Fly(_ovPivot, _ovDist, _ovPitch, _ovYaw));
        }

        IEnumerator Co_Fly(Vector3 tPivot, float tDist, float tPitch, float tYaw)
        {
            _flying = true;
            float sDist=_dist, sPitch=_pitch, sYaw=_yaw;
            Vector3 sPivot = pivot.position;
            float ePitch = tPitch >= 0 ? tPitch : Mathf.Clamp(_pitch, 25f, 50f);
            float eYaw   = tYaw   >= 0 ? tYaw   : _yaw;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / flyDuration;
                float s = Mathf.SmoothStep(0, 1, Mathf.Min(t, 1f));
                pivot.position = Vector3.Lerp(sPivot, tPivot, s);
                _dist  = Mathf.Lerp(sDist,  tDist,  s);
                _pitch = Mathf.Lerp(sPitch, ePitch, s);
                _yaw   = Mathf.LerpAngle(sYaw, eYaw, s);
                ApplyTransform();
                yield return null;
            }
            _dist=tDist; _pitch=ePitch; _yaw=eYaw; pivot.position=tPivot;
            ApplyTransform(); _flying=false;
        }

        // UI Toolkit: Input.mousePosition y=0 là bottom, y=Screen.height là top
        bool OverSidebar()
        {
            float x = Input.mousePosition.x;
            float y = Input.mousePosition.y;
            // Sidebar: 390px bên phải
            if (x > Screen.width - 390f) return true;
            // Topbar: 55px từ trên (y cao = gần top)
            if (y > Screen.height - 55f) return true;
            // Bottom stats: 90px từ dưới, chỉ bên trái
            if (y < 90f && x < 220f) return true;
            return false;
        }
    }
}