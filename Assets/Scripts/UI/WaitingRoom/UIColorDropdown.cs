using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIColorDropdown : Selectable, IPointerClickHandler, ISubmitHandler, ICancelHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform templateRoot;
        [SerializeField] private RectTransform contentParent;
        [SerializeField] private Image selectedColorImage;

        [Header("Prefabs")]
        [SerializeField] private UIColorItem colorItemPrefab;

        private ScrollRect _scrollRect;
        private RectTransform _contentRect;
        private VerticalLayoutGroup _contentLayoutGroup;
        private UIViewData.ColorOptionViewData[] _colorOptions = Array.Empty<UIViewData.ColorOptionViewData>();
        private Action<int> _selectionRequested;
        private int _selectedColorId = -1;
        private Color _selectedDisplayColor = Color.white;

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

        public void BindOptions(UIViewData.ColorOptionViewData[] colorOptions, int selectedColorId, Color selectedDisplayColor, Action<int> selectionRequested)
        {
            _colorOptions = colorOptions ?? Array.Empty<UIViewData.ColorOptionViewData>();
            _selectedColorId = selectedColorId;
            _selectedDisplayColor = selectedDisplayColor;
            _selectionRequested = selectionRequested;

            RefreshSelectedColorVisual();
            RebuildItems();
            RefreshLayout();
        }

        public void RefreshVisibility()
        {
            if (!IsInteractable())
                Hide();
        }

        public void RefreshLayout()
        {
            CacheTemplateComponents();
            BindItems();
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

            if (contentParent == null)
                contentParent = _scrollRect.content;

            _contentRect = contentParent;

            if (_contentRect != null && _contentLayoutGroup == null)
                _contentLayoutGroup = _contentRect.GetComponent<VerticalLayoutGroup>();
        }

        private void RebuildItems()
        {
            CacheTemplateComponents();

            if (_contentRect == null || colorItemPrefab == null)
                return;

            var existingItems = new List<UIColorItem>(_contentRect.childCount);
            for (var i = 0; i < _contentRect.childCount; i++)
            {
                var childItem = _contentRect.GetChild(i).GetComponent<UIColorItem>();
                if (childItem != null)
                    existingItems.Add(childItem);
            }

            for (var i = existingItems.Count; i < _colorOptions.Length; i++)
            {
                var item = Instantiate(colorItemPrefab, _contentRect);
                item.name = colorItemPrefab.name;
                existingItems.Add(item);
            }

            for (var i = existingItems.Count - 1; i >= _colorOptions.Length; i--)
                Destroy(existingItems[i].gameObject);

            BindItems();
        }

        private void BindItems()
        {
            if (_contentRect == null)
                return;

            var itemIndex = 0;
            for (var i = 0; i < _contentRect.childCount && itemIndex < _colorOptions.Length; i++)
            {
                var child = _contentRect.GetChild(i);
                var colorItem = child.GetComponent<UIColorItem>();
                if (colorItem == null)
                    continue;

                colorItem.Bind(_colorOptions[itemIndex], _colorOptions[itemIndex].ColorId == _selectedColorId, _selectionRequested);
                itemIndex++;
            }
        }

        private void RefreshSelectedColorVisual()
        {
            if (selectedColorImage != null)
                selectedColorImage.color = _selectedDisplayColor;
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
                RebuildItems();

            if (shouldOpen)
                RefreshLayout();
        }
    }
}
