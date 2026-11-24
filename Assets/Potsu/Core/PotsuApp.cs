using Potsu.Composition;
using Potsu.Surface;
using Potsu.Synthesis;
using Potsu.Timing;
using UnityEngine;

namespace Potsu.Core
{
    /// <summary>
    /// Point d'entrée : crée les modules et les relie entre eux.
    /// Surface (frappe) → Compositeur (quelles notes ?) → Synthétiseur (son) + Surface (onde) + Pendule (répétitions).
    /// Les modules ne se connaissent pas : seul PotsuApp les assemble.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PotsuApp : MonoBehaviour
    {
        [Tooltip("Force relative de l'onde de la dernière répétition.")]
        [SerializeField, Range(0f, 1f)] float wornRippleStrength = 0.2f;

        Composer composer;
        Synthesizer synthesizer;
        RippleSurface surface;
        Pendulum pendulum;

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
            pendulum = GetOrAdd<Pendulum>();

            // Le Pendule suit l'horloge audio : les répétitions tombent pile, sans dérive.
            pendulum.Clock = () => synthesizer.AudioTime;

            surface.Struck += OnStruck;
            pendulum.EchoAhead += OnEchoAhead;
            pendulum.EchoNow += OnEchoNow;
        }

        void OnDestroy()
        {
            if (surface != null) surface.Struck -= OnStruck;
            if (pendulum != null)
            {
                pendulum.EchoAhead -= OnEchoAhead;
                pendulum.EchoNow -= OnEchoNow;
            }
        }

        void OnStruck(Vector2 screenPosition, float charge)
        {
            int[] notes = composer.PickNotes(charge);
            double time = synthesizer.AudioTime;

            synthesizer.PlayNotes(notes, NoteTimbre.Fresh, time);
            surface.SpawnRipple(screenPosition, charge);
            pendulum.Register(new Strike(notes, screenPosition, charge, time));
        }

        void OnEchoAhead(Echo echo)
        {
            synthesizer.PlayNotes(echo.Strike.Notes, synthesizer.Worn(echo.Wear), echo.Time);
        }

        void OnEchoNow(Echo echo)
        {
            float strength = Mathf.Pow(Mathf.Max(wornRippleStrength, 1e-3f), echo.Wear);
            surface.SpawnRipple(echo.Strike.ScreenPosition, echo.Strike.Charge, strength);
        }

        T GetOrAdd<T>() where T : Component
        {
            var component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
