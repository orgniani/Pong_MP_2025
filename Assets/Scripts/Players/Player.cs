using Helpers;
using UnityEngine;

namespace Players
{
    [RequireComponent(typeof(Collider2D))]
    public class Player : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private bool useWASD = true;

        [Header("Play Area")]
        [SerializeField] private Collider2D playArea;

        private float _moveInput;

        private float _halfHeight;
        private float _halfWidth;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();

            _halfHeight = col.bounds.extents.y;
            _halfWidth = col.bounds.extents.x;
        }

        private void Update()
        {
            if (useWASD)
            {
                if (Input.GetKey(KeyCode.W))
                    _moveInput = 1f;
                else if (Input.GetKey(KeyCode.S))
                    _moveInput = -1f;
                else
                    _moveInput = 0f;
            }
            else
            {
                if (Input.GetKey(KeyCode.UpArrow))
                    _moveInput = 1f;
                else if (Input.GetKey(KeyCode.DownArrow))
                    _moveInput = -1f;
                else
                    _moveInput = 0f;
            }

            transform.Translate(-Vector3.right * _moveInput * speed * Time.deltaTime);
            transform.position = PlayAreaHelper.ClampToBounds(transform.position, playArea, _halfWidth, _halfHeight);
        }
    }
}
