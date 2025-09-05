using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cameras
{
    public class CameraTracker : MonoBehaviour
    {
        [Header("Available Actions")]
        [SerializeField] private bool orbit = true;
        [SerializeField] private bool zoom = true;

        [Header("Tracking Settings")]
        [SerializeField] private Vector3 baseFollowOffset = new Vector3(0f, 4f, -6f);

        [Header("Speed")]
        [SerializeField, Range(0f, 180f)] private float orbitSpeed = 90f;
        [SerializeField, Range(0f, 100f)] private float zoomSpeed = 10f;

        [Header("Zoom")]
        [SerializeField, Range(0f, 1f)] private float minZoom = 0.5f;
        [SerializeField, Range(1f, 5f)] private float maxZoom = 2f;

        [Header("Vertical Angle")]
        [SerializeField, Range(-85f, 85f)] private float minVerticalAngle = -30f;
        [SerializeField, Range(-85f, 85f)] private float maxVerticalAngle = 60f;

        [Header("Smooth Speed")]
        [SerializeField, Range(0f, 10f)] private float rotationSmoothSpeed = 5f;
        [SerializeField, Range(0f, 10f)] private float positionSmoothSpeed = 5f;

        private Transform _followTarget;
        private float _currentX = 0f;
        private float _currentY = 10f;
        private float _currentZoom = 1f;

        private Coroutine _followCoroutine;

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;

            if (_followCoroutine != null)
                StopCoroutine(_followCoroutine);

            _followCoroutine = StartCoroutine(FollowTargetCoroutine());
        }

        private IEnumerator FollowTargetCoroutine()
        {
            while (_followTarget)
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    if (orbit)
                    {
                        _currentX += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                        _currentY -= Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
                        _currentY = Mathf.Clamp(_currentY, minVerticalAngle, maxVerticalAngle);
                    }

                    if (zoom)
                    {
                        float zoomInput = Input.GetAxis("Mouse ScrollWheel");
                        _currentZoom = Mathf.Clamp(_currentZoom - zoomInput * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
                    }
                }

                Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0f);
                Vector3 desiredPosition = _followTarget.position + rotation * baseFollowOffset * _currentZoom;

                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSmoothSpeed * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * Time.deltaTime);

                yield return new WaitForFixedUpdate();
            }

            _followCoroutine = null;
        }
    }
}