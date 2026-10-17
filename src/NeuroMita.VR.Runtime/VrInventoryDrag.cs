using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Il2CppInterop.Runtime.InteropTypes;

namespace NeuroMita.VR
{
    internal static class VrInventoryDrag
    {
        private static InventoryFullUI owner;
        private static InventorySlotView source;
        private static Inventory inventory;
        private static InventorySlot originalSlot;
        private static ItemDefinition originalItem;
        private static int originalQuantity;
        private static Image icon;
        private static int sourceIndex;

        public static bool Dispatch<T>(GameObject target, BaseEventData data) where T : Il2CppObjectBase
        {
            bool dragEvent = typeof(T) == typeof(IBeginDragHandler) || typeof(T) == typeof(IDragHandler) ||
                typeof(T) == typeof(IEndDragHandler) || typeof(T) == typeof(IDropHandler);
            if (!dragEvent || !VrRuntime.IsActive) return false;
            var slot = target.GetComponent<InventorySlotView>();
            if (slot == null) return false;
            var pointer = data.Cast<PointerEventData>();
            if (typeof(T) == typeof(IBeginDragHandler)) Begin(slot, pointer);
            else if (typeof(T) == typeof(IDragHandler)) Update(pointer);
            else if (typeof(T) == typeof(IDropHandler)) Drop(slot);
            else Cancel();
            return true;
        }

        private static bool Valid()
        {
            if (owner == null || source == null || !owner.isActiveAndEnabled || !owner.isInventoryOpen ||
                inventory == null || owner._targetInventory == null || owner._targetInventory.Pointer != inventory.Pointer || VrRuntime.Rig?.HeadTracked != true ||
                !VrRuntime.Rig.PointerHandTracked) return false;
            var slots = inventory.GetSlots();
            return sourceIndex >= 0 && sourceIndex < slots.Count && slots[sourceIndex] != null &&
                slots[sourceIndex].Pointer == originalSlot.Pointer && originalSlot.item == originalItem &&
                originalItem != null && originalSlot.quantity == originalQuantity;
        }

        private static void Begin(InventorySlotView slot, PointerEventData pointer)
        {
            Cancel();
            var current = slot._owner;
            if (pointer.button != PointerEventData.InputButton.Left || current == null || !current.isInventoryOpen ||
                current._isAnimating || current._targetInventory == null) return;
            var slots = current._targetInventory.GetSlots();
            int index = slot._index;
            if (index < 0 || index >= slots.Count || slots[index]?.item == null) return;
            owner = current;
            source = slot;
            sourceIndex = index;
            inventory = current._targetInventory;
            originalSlot = slots[index];
            originalItem = originalSlot.item;
            originalQuantity = originalSlot.quantity;
            try
            {
                owner.CloseContextMenu();
                owner.SelectSlot(index, true);
                var canvas = owner.GetComponentInParent<Canvas>();
                if (canvas == null) { Cancel(); return; }
                var rect = VrModPanel.Rect("VR inventory dragged item", canvas.transform, new Vector2(72f, 72f), Vector2.zero);
                icon = rect.gameObject.AddComponent<Image>();
                icon.sprite = slot.GetCurrentItemSprite();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                source.SetDragSuppressed(true);
                Update(pointer);
            }
            catch { Cancel(); throw; }
        }

        public static void Update(PointerEventData pointer)
        {
            if (owner == null) { if (source != null || icon != null) Cancel(); return; }
            if (!Valid()) { Cancel(); return; }
            if (icon == null || pointer == null) return;
            var plane = icon.rectTransform.parent.TryCast<RectTransform>();
            var camera = owner.GetComponentInParent<Canvas>()?.worldCamera;
            if (plane != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(plane, pointer.position, camera, out var world))
                icon.rectTransform.position = world;
        }

        private static void Drop(InventorySlotView target)
        {
            if (!Valid() || target._owner != owner || owner._isAnimating) { Cancel(); return; }
            int index = target._index;
            var slots = inventory.GetSlots();
            if (index < 0 || index >= slots.Count || index == sourceIndex) { Cancel(); return; }
            var view = owner;
            try
            {
                inventory.SwapItems(sourceIndex, index);
                Cancel();
                view.RefreshUI();
                view.SelectSlot(index, true);
            }
            finally { Cancel(); }
        }

        public static void PointerDown(GameObject target)
        {
            foreach (var view in UnityEngine.Object.FindObjectsByType<InventoryFullUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var menu = view.slotContextMenu;
                if (!view.isInventoryOpen || menu == null || !menu.activeSelf) continue;
                if (target == null || (target != menu && !target.transform.IsChildOf(menu.transform))) view.CloseContextMenu();
            }
        }

        public static void Cancel()
        {
            var previous = source;
            var previousIcon = icon;
            owner = null;
            source = null;
            inventory = null;
            originalSlot = null;
            originalItem = null;
            icon = null;
            if (previousIcon != null) { previousIcon.gameObject.SetActive(false); UnityEngine.Object.Destroy(previousIcon.gameObject); }
            if (previous != null) previous.SetDragSuppressed(false);
        }
    }
}
