using UnityEngine;

namespace Potsu.Timing
{
    /// <summary>Une frappe posée sur la surface : ses notes, sa position, sa charge, et l'instant où elle a sonné.</summary>
    public readonly struct Strike
    {
        public readonly int[] Notes;
        public readonly Vector2 ScreenPosition;
        public readonly float Charge;
        public readonly double Time;

        public Strike(int[] notes, Vector2 screenPosition, float charge, double time)
        {
            Notes = notes;
            ScreenPosition = screenPosition;
            Charge = charge;
            Time = time;
        }
    }

    /// <summary>Une répétition d'une frappe.</summary>
    public readonly struct Echo
    {
        public readonly Strike Strike;

        /// <summary>Numéro de la répétition : 1 pour la première.</summary>
        public readonly int Repetition;

        /// <summary>Usure de 0 (neuf) à 1 (dernière répétition).</summary>
        public readonly float Wear;

        /// <summary>Instant de l'écho, sur l'horloge du Pendule.</summary>
        public readonly double Time;

        public Echo(Strike strike, int repetition, float wear, double time)
        {
            Strike = strike;
            Repetition = repetition;
            Wear = wear;
            Time = time;
        }
    }
}
