using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroMita.VR
{
    internal sealed class VrMenuStage : IDisposable
    {
        private readonly struct LightPlacement
        {
            public readonly Transform Light, Parent;
            public readonly Vector3 Position, Scale, WorldPosition;
            public readonly Quaternion Rotation, WorldRotation;
            public readonly int Sibling;

            public LightPlacement(Transform light)
            {
                Light = light;
                WorldPosition = light.position;
                WorldRotation = light.rotation;
                Parent = light.parent;
                Position = light.localPosition;
                Rotation = light.localRotation;
                Scale = light.localScale;
                Sibling = light.GetSiblingIndex();
            }

            public void Restore()
            {
                if (Light == null) return;
                Light.SetParent(Parent, false);
                Light.localPosition = Position;
                Light.localRotation = Rotation;
                Light.localScale = Scale;
                Light.SetSiblingIndex(Sibling);
            }
        }

        private readonly List<LightPlacement> lights = new();

        public static VrMenuStage Create(Camera camera)
        {
            MenuMita character = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<MenuMita>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene == camera.gameObject.scene && candidate.mitaAnim != null &&
                    candidate.mitaAnim.gameObject.activeInHierarchy)
                { character = candidate; break; }
            if (character == null) return null;
            VrDiagnostics.MenuLighting(camera, character);
            Vector3 origin = character.mitaAnim.transform.position;
            Vector3 direction = camera.transform.position - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = -camera.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            var stage = new VrMenuStage();
            foreach (var light in camera.GetComponentsInChildren<Light>(true))
            {
                stage.lights.Add(new LightPlacement(light.transform));

            }
            Vector3 position = origin + direction.normalized * 2.2f;
            position.y = camera.transform.position.y;
            Quaternion facing = Quaternion.LookRotation(-direction.normalized, Vector3.up);
            camera.transform.SetPositionAndRotation(position, facing);
            return stage;
        }

        public void HoldLights()
        {
            foreach (var placement in lights)
                if (placement.Light != null) placement.Light.SetPositionAndRotation(placement.WorldPosition, placement.WorldRotation);
        }

        public void Dispose()
        {
            foreach (var placement in lights) placement.Restore();
            lights.Clear();
        }
    }
}
