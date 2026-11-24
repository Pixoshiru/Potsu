using System.Collections.Generic;
using UnityEngine;

namespace Potsu.Synthesis
{
    /// <summary>
    /// Synthétiseur : génère le son en temps réel, sans samples.
    /// V1 : sinusoïde pure, enveloppe attaque courte / decay exponentiel, polyphonie à voix fixes.
    /// Le calcul se fait dans OnAudioFilterRead (thread audio) ; les notes y arrivent par une file protégée.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class Synthesizer : MonoBehaviour
    {
        [Header("Enveloppe")]
        [Tooltip("Attaque, en secondes.")]
        [SerializeField, Range(0.001f, 0.2f)] float attack = 0.008f;

        [Tooltip("Decay, en secondes : temps pour descendre à -60 dB.")]
        [SerializeField, Range(0.1f, 5f)] float decay = 1f;

        [Header("Volume")]
        [Tooltip("Volume d'une note seule.")]
        [SerializeField, Range(0f, 1f)] float noteVolume = 0.3f;

        [Tooltip("Volume général, avant le limiteur doux.")]
        [SerializeField, Range(0f, 1f)] float masterVolume = 0.8f;

        [Header("Polyphonie")]
        [SerializeField, Range(1, 64)] int maxVoices = 32;

        struct Voice
        {
            public bool Active;
            public double Phase;
            public double PhaseIncrement;
            public int Age;
            public float Gain;
        }

        struct NoteRequest
        {
            public double Frequency;
            public float Gain;
        }

        const double TwoPi = System.Math.PI * 2.0;
        const int MaxPending = 64;

        readonly object pendingLock = new object();
        readonly NoteRequest[] pending = new NoteRequest[MaxPending];
        int pendingCount;

        Voice[] voices;
        int sampleRate;

        // Copies des paramètres pour le thread audio (on ne lit pas les champs sérialisés depuis là-bas).
        volatile float attackSeconds;
        volatile float decaySeconds;
        volatile float master;

        void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            voices = new Voice[maxVoices];
            CopyParameters();

            // Une source qui joue un clip silencieux en boucle : c'est elle qui déclenche OnAudioFilterRead.
            var source = GetComponent<AudioSource>();
            source.clip = AudioClip.Create("Potsu Silence", sampleRate, 1, sampleRate, false);
            source.loop = true;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.Play();
        }

        void Update() => CopyParameters();

        void OnValidate() => CopyParameters();

        void CopyParameters()
        {
            attackSeconds = attack;
            decaySeconds = decay;
            master = masterVolume;
        }

        public static double MidiToFrequency(int midi) => 440.0 * System.Math.Pow(2.0, (midi - 69) / 12.0);

        /// <summary>Joue une ou plusieurs notes MIDI en même temps. Le volume par note baisse avec la taille de l'accord.</summary>
        public void PlayNotes(IReadOnlyList<int> midiNotes)
        {
            if (midiNotes == null || midiNotes.Count == 0) return;
            float gain = noteVolume / Mathf.Sqrt(midiNotes.Count);
            lock (pendingLock)
            {
                for (int i = 0; i < midiNotes.Count && pendingCount < MaxPending; i++)
                    pending[pendingCount++] = new NoteRequest { Frequency = MidiToFrequency(midiNotes[i]), Gain = gain };
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (voices == null) return;

            lock (pendingLock)
            {
                for (int i = 0; i < pendingCount; i++)
                    StartVoice(pending[i]);
                pendingCount = 0;
            }

            int attackSamples = Mathf.Max(1, (int)(attackSeconds * sampleRate));
            int decaySamples = Mathf.Max(1, (int)(decaySeconds * sampleRate));
            int lifeSamples = attackSamples + decaySamples;
            // exp(-k * decaySamples) = 0.001 (-60 dB)
            float k = 6.9077553f / decaySamples;
            float gainOut = master;

            int frames = data.Length / channels;
            for (int f = 0; f < frames; f++)
            {
                float sum = 0f;
                for (int v = 0; v < voices.Length; v++)
                {
                    ref Voice voice = ref voices[v];
                    if (!voice.Active) continue;

                    float envelope = voice.Age < attackSamples
                        ? (float)voice.Age / attackSamples
                        : Mathf.Exp(-k * (voice.Age - attackSamples));

                    sum += (float)System.Math.Sin(voice.Phase) * envelope * voice.Gain;

                    voice.Phase += voice.PhaseIncrement;
                    if (voice.Phase >= TwoPi) voice.Phase -= TwoPi;
                    if (++voice.Age >= lifeSamples) voice.Active = false;
                }

                float sample = SoftClip(sum * gainOut);
                int offset = f * channels;
                for (int c = 0; c < channels; c++)
                    data[offset + c] = sample;
            }
        }

        void StartVoice(NoteRequest request)
        {
            // Une voix libre, sinon on vole la plus ancienne.
            int target = 0;
            int oldestAge = -1;
            for (int v = 0; v < voices.Length; v++)
            {
                if (!voices[v].Active) { target = v; oldestAge = int.MaxValue; break; }
                if (voices[v].Age > oldestAge) { oldestAge = voices[v].Age; target = v; }
            }

            voices[target] = new Voice
            {
                Active = true,
                Phase = 0.0,
                PhaseIncrement = TwoPi * request.Frequency / sampleRate,
                Age = 0,
                Gain = request.Gain,
            };
        }

        /// <summary>Limiteur doux (approximation rationnelle de tanh) : les accords denses ne saturent pas brutalement.</summary>
        static float SoftClip(float x)
        {
            if (x > 3f) return 1f;
            if (x < -3f) return -1f;
            float x2 = x * x;
            return x * (27f + x2) / (27f + 9f * x2);
        }
    }
}
