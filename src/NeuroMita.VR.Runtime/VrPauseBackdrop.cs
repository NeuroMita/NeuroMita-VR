using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    public sealed partial class VrUiPresenter
    {
        private Canvas pauseBackdrop;
        private System.IDisposable pauseBackdropExclusion;

        private void RefreshPauseBackdrop(Canvas pause)
        {
            if (pause == null)
            {
                if (pauseBackdrop != null) pauseBackdrop.gameObject.SetActive(false);
                return;
            }
            if (pauseBackdrop == null)
            {
                var root = VrObject.Create("VR pause backdrop", typeof(RectTransform), typeof(Canvas), typeof(Image));
                root.transform.SetParent(transform, false);
                pauseBackdrop = root.GetComponent<Canvas>();
                pauseBackdrop.renderMode = RenderMode.WorldSpace;
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);
                var image = root.GetComponent<Image>();
                image.color = new Color(0.015f, 0.01f, 0.025f, 0.55f);
                image.raycastTarget = false;
                pauseBackdropExclusion?.Dispose();
                pauseBackdropExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(pauseBackdrop));
            }
            pauseBackdrop.gameObject.SetActive(true);
            pauseBackdrop.worldCamera = rig?.Eye;
            pauseBackdrop.sortingOrder = pause.sortingOrder - 1;
            pauseBackdrop.transform.SetPositionAndRotation(pause.transform.position + pause.transform.forward * 0.025f,
                pause.transform.rotation);
        }

        private void ReleasePauseBackdrop()
        {
            pauseBackdropExclusion?.Dispose();
            pauseBackdropExclusion = null;
            if (pauseBackdrop != null) Destroy(pauseBackdrop.gameObject);
            pauseBackdrop = null;
        }
    }
}
