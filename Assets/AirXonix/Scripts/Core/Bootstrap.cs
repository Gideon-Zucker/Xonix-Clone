using UnityEngine;
using UnityEngine.Rendering;

namespace AirXonix
{
    /// <summary>
    /// Safety net: even if the scene lost its wiring, entering Play mode (or a
    /// build) still spins up a camera and the game. The editor scene created by
    /// SceneBuilder already contains a GameManager, so this becomes a no-op there.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (Object.FindObjectOfType<GameManager>() != null) return;

            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.05f, 0.12f);
            cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            cam.transform.position = new Vector3(0f, 40f, -40f);

            EnsureLighting();

            new GameObject("AirXonix").AddComponent<GameManager>();
        }

        static void EnsureLighting()
        {
            if (Object.FindObjectOfType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.shadows = LightShadows.Soft;
                light.intensity = 1.1f;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.18f, 0.22f);
        }
    }
}
