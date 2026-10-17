using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    internal sealed class VrMenuBackdrop : IDisposable
    {
        private readonly List<(Image Source, bool Enabled)> sources = new();
        private bool disposed;

        public VrMenuBackdrop(Canvas canvas, Transform parent)
        {
            foreach (var source in canvas.GetComponentsInChildren<Image>(true))
            {
                sources.Add((source, source.enabled));
                source.enabled = false;
            }
        }

        public void Refresh()
        {
            if (disposed) return;
            foreach (var source in sources)
                if (source.Source != null) source.Source.enabled = false;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var source in sources)
                if (source.Source != null) source.Source.enabled = source.Enabled;
            sources.Clear();
        }
    }
}
