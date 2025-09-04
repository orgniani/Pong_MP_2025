using UnityEngine;

namespace Players
{
    [RequireComponent(typeof(Collider2D))]
    public class Player : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private bool useWASD = true;

        private float _moveInput;
        private float _halfHeight;
        private float _halfWidth;

        private SpriteRenderer _parentRenderer;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            _halfHeight = col.bounds.extents.y;
            _halfWidth = col.bounds.extents.x;

            _parentRenderer = transform.parent.GetComponent<SpriteRenderer>();
            if (_parentRenderer == null)
                Debug.LogError("[Player] Parent does not have a SpriteRenderer!");
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

            if (_parentRenderer != null)
                ClampToParentBounds();
        }

        private void ClampToParentBounds()
        {
            Bounds bounds = _parentRenderer.bounds;
            Vector3 pos = transform.position;

            float minX = bounds.min.x + _halfWidth;
            float maxX = bounds.max.x - _halfWidth;
            float minY = bounds.min.y + _halfHeight;
            float maxY = bounds.max.y - _halfHeight;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            transform.position = pos;
        }
    }
}
