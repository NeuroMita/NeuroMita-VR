using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

namespace NeuroMitaVRmod;

[BepInPlugin("vinerx.neuromita.vr", "NeuroMita VR", "0.1.0")]
public sealed class Plugin : BasePlugin
{
    private Type runtime;
    private bool started;

    public override void Load()
    {
        if (!Config.Bind("General", "Enabled", true, "Enable the external VR runtime on startup").Value) return;
        try
        {
            var host = Assembly.Load("Assembly-CSharp");
            var bridge = host.GetType("NeuroMita.Modding.ExternalControlBridge", false);
            if (bridge == null || bridge.GetMethod("OpenSession") == null)
                throw new InvalidOperationException("This game build does not contain the external control API. Apply the API commits and rebuild the host first.");
            foreach (string name in new[] { "Unity.XR.Management", "Unity.XR.OpenXR", "Unity.InputSystem" })
            {
                try { Assembly.Load(name); }
                catch (Exception exception) { throw new InvalidOperationException("Host XR prerequisite missing: " + name, exception); }
            }
            string path = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "NeuroMita.VR.Runtime.dll");
            if (!File.Exists(path)) throw new FileNotFoundException("External IL2CPP runtime module is missing. The bootstrap alone cannot provide VR.", path);
            var module = Assembly.LoadFrom(path);
            runtime = module.GetType("NeuroMita.VR.VrRuntime", true);
            if (!Equals(runtime.GetProperty("RuntimeBackend")?.GetValue(null), "IL2CPP"))
                throw new InvalidOperationException("Runtime module was not compiled against IL2CPP interop assemblies.");
            ConfigureSettings(module);
            foreach (var type in module.GetTypes().Where(IsUnityBehaviour))
                ClassInjector.RegisterTypeInIl2Cpp(type);
            runtime.GetMethod("StartVr", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
            started = true;
            Log.LogInfo("External VR runtime requested; OpenXR status is reported by the runtime. Headset readiness is not implied.");
        }
        catch (Exception exception)
        {
            Log.LogError("VR module did not start; desktop remains available. " + exception);
        }
    }

    private static bool IsUnityBehaviour(Type type)
    {
        if (type.IsAbstract) return false;
        for (var parent = type.BaseType; parent != null; parent = parent.BaseType)
            if (parent.FullName == "UnityEngine.MonoBehaviour") return true;
        return false;
    }

    private void ConfigureSettings(Assembly module)
    {
        var type = module.GetType("NeuroMita.VR.VrSettings", true)!;
        object settings = type.GetProperty("Current")!.GetValue(null)!;
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            if (property.PropertyType == typeof(int))
            {
                var entry = Config.Bind("VR", property.Name, (int)property.GetValue(settings)!, "VR runtime preference");
                property.SetValue(settings, entry.Value);
                entry.SettingChanged += (_, _) => property.SetValue(settings, entry.Value);
            }
            else if (property.PropertyType == typeof(bool))
            {
                var entry = Config.Bind("VR", property.Name, (bool)property.GetValue(settings)!, "VR runtime preference");
                property.SetValue(settings, entry.Value);
                entry.SettingChanged += (_, _) => property.SetValue(settings, entry.Value);
            }
        }
    }

    public override bool Unload()
    {
        if (started)
        {
            runtime?.GetMethod("StopVr", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
            started = false;
        }
        return true;
    }
}
