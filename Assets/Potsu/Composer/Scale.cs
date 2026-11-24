using System;

namespace Potsu.Composition
{
    /// <summary>
    /// Une gamme : une tonique (classe de hauteur 0-11, 0 = Do) et une liste d'intervalles
    /// en demi-tons depuis la tonique. Volontairement générique : une gamme n'est pas
    /// forcément un mode (pentatonique, gamme par tons, gamme inventée…).
    /// </summary>
    public sealed class Scale
    {
        static readonly string[] NoteNames =
            { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };

        public int Root { get; }
        public int[] Intervals { get; }
        public string Label { get; }

        public Scale(int root, int[] intervals, string label)
        {
            if (intervals == null || intervals.Length == 0)
                throw new ArgumentException("Une gamme doit contenir au moins un intervalle.", nameof(intervals));
            Root = ((root % 12) + 12) % 12;
            Intervals = intervals;
            Label = label;
        }

        public string Name => $"{NoteName(Root)} {Label}";

        public static string NoteName(int pitchClass) => NoteNames[((pitchClass % 12) + 12) % 12];

        public bool SameAs(Scale other)
        {
            if (other == null || other.Root != Root || other.Intervals.Length != Intervals.Length) return false;
            for (int i = 0; i < Intervals.Length; i++)
                if (other.Intervals[i] != Intervals[i]) return false;
            return true;
        }
    }
}
