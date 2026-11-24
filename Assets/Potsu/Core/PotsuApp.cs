using Potsu.Composition;
using Potsu.Surface;
using Potsu.Synthesis;
using UnityEngine;

namespace Potsu.Core
{
    /// <summary>
    /// Point d'entrée : crée les modules et les relie entre eux.
    /// Surface (frappe) → Compositeur (quelles notes ?) → Synthétiseur (son) + Surface (onde).
    /// Les modules ne se connaissent pas : seul PotsuApp les assemble.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PotsuApp : MonoBehaviour
    {
        Composer composer;
        Synthesizer synthesizer;
        RippleSurface surface;

        // Démarre Potsu dans n'importe quelle scène, même vide, sans rien configurer à la main.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<PotsuApp>() != null) return;
            new GameObject("Potsu").AddComponent<PotsuApp>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;

            composer = GetOrAdd<Composer>();
            synthesizer = GetOrAdd<Synthesizer>();
            surface = GetOrAdd<RippleSurface>();

            surface.Struck += OnStruck;
        }

        void OnDestroy()
        {
            if (surface != null) surface.Struck -= OnStruck;
        }

        void OnStruck(Vector2 screenPosition, float charge)
        {
            int[] notes = composer.PickNotes(charge);
            synthesizer.PlayNotes(notes);
            surface.SpawnRipple(screenPosition, charge);
        }

        T GetOrAdd<T>() where T : Component
        {
            var component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
