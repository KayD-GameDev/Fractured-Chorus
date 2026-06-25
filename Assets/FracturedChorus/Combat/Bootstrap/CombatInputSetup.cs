using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace FracturedChorus.Combat.Bootstrap
{
    /// <summary>
    /// Project uses Input System package only — replace legacy StandaloneInputModule on EventSystem.
    /// </summary>
    public static class CombatInputSetup
    {
        public static void Configure(Camera mainCamera = null)
        {
            EnsureEventSystem();
            EnsureCameraRaycaster(mainCamera ?? Camera.main);
        }

        public static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            ApplyInputModule(eventSystem.gameObject, destroyImmediate: false);
        }

        public static void EnsureCameraRaycaster(Camera camera, bool destroyImmediate = false)
        {
            if (camera == null)
            {
                return;
            }

            var legacy3D = camera.GetComponent<PhysicsRaycaster>();
            if (legacy3D != null)
            {
                if (destroyImmediate)
                {
                    Object.DestroyImmediate(legacy3D);
                }
                else
                {
                    Object.Destroy(legacy3D);
                }
            }

            if (camera.GetComponent<Physics2DRaycaster>() == null)
            {
                camera.gameObject.AddComponent<Physics2DRaycaster>();
            }

            var raycaster2D = camera.GetComponent<Physics2DRaycaster>();
            if (raycaster2D != null && raycaster2D.maxRayIntersections <= 0)
            {
                raycaster2D.maxRayIntersections = 32;
            }
        }

        public static void ApplyInputModule(GameObject eventSystemObject, bool destroyImmediate)
        {
#if ENABLE_INPUT_SYSTEM
            RemoveModule<StandaloneInputModule>(eventSystemObject, destroyImmediate);

            if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private static void RemoveModule<T>(GameObject target, bool destroyImmediate) where T : Component
        {
            var module = target.GetComponent<T>();
            if (module == null)
            {
                return;
            }

            if (destroyImmediate)
            {
                Object.DestroyImmediate(module);
            }
            else
            {
                Object.Destroy(module);
            }
        }
    }
}
