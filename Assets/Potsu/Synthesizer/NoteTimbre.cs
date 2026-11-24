namespace Potsu.Synthesis
{
    /// <summary>Le caractère d'une note jouée : volume relatif, brillance (filtre passe-bas) et attaque.</summary>
    public struct NoteTimbre
    {
        /// <summary>Multiplicateur de volume (1 = volume normal).</summary>
        public float Gain;

        /// <summary>Fréquence de coupure du passe-bas, en Hz. 0 ou moins = pas de filtre.</summary>
        public float LowpassHz;

        /// <summary>Attaque, en secondes. 0 ou moins = attaque par défaut du synthé.</summary>
        public float Attack;

        /// <summary>Une note neuve : volume plein, aucun filtre, attaque par défaut.</summary>
        public static NoteTimbre Fresh => new NoteTimbre { Gain = 1f, LowpassHz = 0f, Attack = 0f };
    }
}
