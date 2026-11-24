namespace Potsu.Composition
{
    /// <summary>Les sept modes de la gamme majeure (modes « grecs »).</summary>
    public enum Mode
    {
        Ionian,
        Dorian,
        Phrygian,
        Lydian,
        Mixolydian,
        Aeolian,
        Locrian,
    }

    public static class Modes
    {
        static readonly int[] Major = { 0, 2, 4, 5, 7, 9, 11 };
        static readonly string[] FrenchNames =
            { "ionien", "dorien", "phrygien", "lydien", "mixolydien", "éolien", "locrien" };

        public const int Count = 7;

        /// <summary>Intervalles du mode, obtenus par rotation de la gamme majeure.</summary>
        public static int[] Intervals(Mode mode)
        {
            int r = (int)mode;
            var result = new int[Major.Length];
            for (int i = 0; i < Major.Length; i++)
                result[i] = (Major[(i + r) % Major.Length] - Major[r] + 12) % 12;
            return result;
        }

        public static string FrenchName(Mode mode) => FrenchNames[(int)mode];

        public static Scale Create(int root, Mode mode) => new Scale(root, Intervals(mode), FrenchName(mode));
    }
}
