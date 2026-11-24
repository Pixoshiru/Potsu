using System;
using System.Collections.Generic;
using UnityEngine;

namespace Potsu.Timing
{
    /// <summary>
    /// Pendule : la temporalité d'une session.
    /// V1 : chaque frappe posée se répète à intervalle fixe, un nombre limité de fois, en s'usant à chaque répétition.
    ///
    /// Le Pendule ne joue rien lui-même : il annonce les échos. Il les annonce deux fois :
    /// - <see cref="EchoAhead"/> un peu en avance (<c>lookahead</c>), avec l'instant exact, pour que le son
    ///   soit programmé à l'échantillon près ;
    /// - <see cref="EchoNow"/> au moment même, pour ce qui se cale sur l'image (les ondes).
    /// </summary>
    public sealed class Pendulum : MonoBehaviour
    {
        [Tooltip("Intervalle entre deux répétitions, en secondes.")]
        [SerializeField, Min(0.05f)] float interval = 1f;

        [Tooltip("Nombre de répétitions d'une frappe, après quoi elle se tait.")]
        [SerializeField, Range(0, 32)] int repetitions = 6;

        [Tooltip("Avance avec laquelle les échos sont envoyés au son, en secondes.")]
        [SerializeField, Range(0.02f, 0.5f)] float lookahead = 0.1f;

        readonly List<Echo> ahead = new List<Echo>();
        readonly List<Echo> now = new List<Echo>();

        /// <summary>
        /// L'horloge du Pendule, en secondes. Par défaut le temps de Unity ; PotsuApp la branche sur l'horloge audio.
        /// À terme : l'horloge de la machine.
        /// </summary>
        public Func<double> Clock { get; set; } = () => Time.timeAsDouble;

        public event Action<Echo> EchoAhead;
        public event Action<Echo> EchoNow;

        public double CurrentTime => Clock();

        /// <summary>Le nombre d'échos encore à venir.</summary>
        public int PendingCount => now.Count;

        /// <summary>Pose une frappe : ses répétitions sont programmées à partir de <see cref="Strike.Time"/>.</summary>
        public void Register(Strike strike)
        {
            for (int r = 1; r <= repetitions; r++)
            {
                var echo = new Echo(strike, r, (float)r / repetitions, strike.Time + r * interval);
                ahead.Add(echo);
                now.Add(echo);
            }
        }

        /// <summary>Oublie toutes les frappes posées.</summary>
        public void Clear()
        {
            ahead.Clear();
            now.Clear();
        }

        void Update()
        {
            double t = Clock();
            Dispatch(ahead, t + lookahead, EchoAhead);
            Dispatch(now, t, EchoNow);
        }

        static void Dispatch(List<Echo> echoes, double until, Action<Echo> handler)
        {
            // Ordre de pose conservé ; retrait en place pour éviter les allocations.
            int kept = 0;
            for (int i = 0; i < echoes.Count; i++)
            {
                var echo = echoes[i];
                if (echo.Time <= until) handler?.Invoke(echo);
                else echoes[kept++] = echo;
            }
            echoes.RemoveRange(kept, echoes.Count - kept);
        }
    }
}
