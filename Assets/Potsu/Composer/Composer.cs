using System;
using System.Collections.Generic;
using UnityEngine;

namespace Potsu.Composition
{
    /// <summary>
    /// Compositeur : tient la gamme du moment et choisit les notes jouées.
    /// Il ne sait rien du son ni de l'image : il renvoie des numéros de notes MIDI.
    /// </summary>
    public sealed class Composer : MonoBehaviour
    {
        [Tooltip("Durée d'une gamme, en secondes. (Ce minutage ira plus tard dans le Pendule.)")]
        [SerializeField] float scaleDuration = 30f;

        [Tooltip("Octave de la note la plus grave (3 = Do3 = MIDI 48).")]
        [SerializeField] int lowestOctave = 3;

        [Tooltip("Tessiture, en octaves.")]
        [SerializeField, Min(1)] int octaveSpan = 2;

        [Tooltip("Nombre maximal de notes d'un accord, atteint avec une charge complète.")]
        [SerializeField, Range(1, 8)] int maxChordNotes = 4;

        [SerializeField] bool logScaleChanges = true;

        readonly System.Random random = new System.Random();
        readonly List<int> pool = new List<int>();
        float scaleTimer;

        /// <summary>Le set de gammes utilisé. Remplaçable à tout moment.</summary>
        public IScaleGenerator ScaleGenerator { get; set; } = new RandomModeGenerator();

        public Scale CurrentScale { get; private set; }

        /// <summary>Les notes jouables (MIDI) dans la gamme et la tessiture actuelles, de la plus grave à la plus aiguë.</summary>
        public IReadOnlyList<int> NotePool => pool;

        public event Action<Scale> ScaleChanged;

        void Awake() => ChangeScale();

        void Update()
        {
            scaleTimer += Time.deltaTime;
            if (scaleTimer >= scaleDuration)
                ChangeScale();
        }

        public void ChangeScale()
        {
            scaleTimer = 0f;
            CurrentScale = ScaleGenerator.Next(CurrentScale, random);
            RebuildPool();
            if (logScaleChanges) Debug.Log($"[Potsu] Gamme : {CurrentScale.Name}");
            ScaleChanged?.Invoke(CurrentScale);
        }

        /// <summary>Nombre de notes pour une charge donnée (0 = clic bref, 1 = maintien complet).</summary>
        public int NoteCountForCharge(float charge01)
        {
            int count = 1 + Mathf.FloorToInt(Mathf.Clamp01(charge01) * maxChordNotes);
            return Mathf.Clamp(count, 1, maxChordNotes);
        }

        /// <summary>
        /// Choisit les notes d'un coup. Une seule note au hasard pour un clic bref ;
        /// sinon un accord empilé en tierces dans la gamme (un degré sur deux).
        /// </summary>
        public int[] PickNotes(float charge01)
        {
            int count = NoteCountForCharge(charge01);
            if (count == 1)
                return new[] { pool[random.Next(pool.Count)] };

            const int step = 2; // tierces diatoniques
            count = Mathf.Min(count, 1 + (pool.Count - 1) / step);
            int span = (count - 1) * step;
            int start = random.Next(pool.Count - span);
            var notes = new int[count];
            for (int i = 0; i < count; i++)
                notes[i] = pool[start + i * step];
            return notes;
        }

        void RebuildPool()
        {
            pool.Clear();
            int baseNote = 12 * (lowestOctave + 1) + CurrentScale.Root;
            for (int octave = 0; octave < octaveSpan; octave++)
                foreach (int interval in CurrentScale.Intervals)
                    pool.Add(baseNote + octave * 12 + interval);
            pool.Add(baseNote + octaveSpan * 12); // la tonique du haut ferme la tessiture
        }
    }
}
