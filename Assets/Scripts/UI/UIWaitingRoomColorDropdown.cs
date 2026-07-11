using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIWaitingRoomColorDropdown : Selectable, IPointerClickHandler, ISubmitHandler, ICancelHandler
    {
        [SerializeField] private RectTransform templateRoot;

        public event Action<bool> OpenStateChanged;

        private ScrollRect _scrollRect;
        private RectTransform _contentRect;
        private VerticalLayoutGroup _contentLayoutGroup;

        public bool isOpen => templateRoot != null && templateRoot.gameObject.activeSelf;

        protected override void Awake()
        {
            base.Awake();

            CacheTemplateComponents();

            if (templateRoot != null)
                templateRoot.gameObject.SetActive(false);
        }

        protected override void OnDisable()
        {
            Hide();
            base.OnDisable();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !IsInteractable())
                return;

            Toggle();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsInteractable())
                return;

            Toggle();
        }

        public void OnCancel(BaseEventData eventData)
        {
            Hide();
        }

        public void Toggle()
        {
            SetOpen(!isOpen);
        }

        public void RefreshVisibility()
        {
            if (!IsInteractable())
                Hide();
        }

        public void RefreshLayout()
        {
            CacheTemplateComponents();
            ResizeContentHeight();

            if (templateRoot == null || !templateRoot.gameObject.activeInHierarchy)
                return;

            Canvas.ForceUpdateCanvases();

            if (_contentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);

            LayoutRebuilder.ForceRebuildLayoutImmediate(templateRoot);

            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;
        }

        public void Hide()
        {
            SetOpen(false);
        }

        private void CacheTemplateComponents()
        {
            if (templateRoot == null)
                return;

            if (_scrollRect == null)
                _scrollRect = templateRoot.GetComponent<ScrollRect>();

            if (_scrollRect == null)
                return;

            _contentRect = _scrollRect.content;

            if (_contentRect != null && _contentLayoutGroup == null)
                _contentLayoutGroup = _contentRect.GetComponent<VerticalLayoutGroup>();
        }

        private void ResizeContentHeight()
        {
            if (_contentRect == null)
                return;

            var totalHeight = 0f;
            var activeChildCount = 0;

            for (var i = 0; i < _contentRect.childCount; i++)
            {
                var child = _contentRect.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                    continue;

                var childHeight = LayoutUtility.GetPreferredHeight(child);
                if (childHeight <= 0f)
                    childHeight = LayoutUtility.GetMinHeight(child);
                if (childHeight <= 0f)
                    childHeight = child.rect.height;

                totalHeight += childHeight;
                activeChildCount++;
            }

            if (_contentLayoutGroup != null)
            {
                totalHeight += _contentLayoutGroup.padding.top + _contentLayoutGroup.padding.bottom;

                if (activeChildCount > 1)
                    totalHeight += _contentLayoutGroup.spacing * (activeChildCount - 1);
            }

            _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, totalHeight));
        }

        private void SetOpen(bool shouldOpen)
        {
            if (templateRoot == null)
                return;

            if (shouldOpen && !IsInteractable())
                shouldOpen = false;

            var dropdownGameObject = templateRoot.gameObject;
            if (dropdownGameObject.activeSelf == shouldOpen)
                return;

            dropdownGameObject.SetActive(shouldOpen);

            if (shouldOpen)
                RefreshLayout();

            OpenStateChanged?.Invoke(shouldOpen);
        }
    }
}
