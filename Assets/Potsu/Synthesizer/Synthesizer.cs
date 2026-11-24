using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Potsu.Synthesis
{
    /// <summary>
    /// Synthétiseur : génère le son en temps réel, sans samples.
    /// Sinusoïdes polyphoniques, enveloppe attaque / decay exponentiel, passe-bas par voix.
    /// Les notes peuvent être programmées à un instant précis de l'horloge audio (<see cref="AudioTime"/>),
    /// à l'échantillon près : c'est ce qui permet au Pendule de répéter les notes sans dérive.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class Synthesizer : MonoBehaviour
    {
        [Header("Enveloppe")]
        [Tooltip("Attaque d'une note neuve, en secondes.")]
        [SerializeField, Range(0.001f, 0.2f)] float attack = 0.008f;

        [Tooltip("Decay, en secondes : temps pour descendre à -60 dB.")]
        [SerializeField, Range(0.1f, 5f)] float decay = 1f;

        [Header("Volume")]
        [Tooltip("Volume d'une note seule.")]
        [SerializeField, Range(0f, 1f)] float noteVolume = 0.3f;

        [Tooltip("Volume général, avant le limiteur doux.")]
        [SerializeField, Range(0f, 1f)] float masterVolume = 0.8f;

        [Header("Polyphonie")]
        [SerializeField, Range(1, 128)] int maxVoices = 48;

        [Header("Dégradation (0 = note neuve, 1 = complètement usée)")]
        [Tooltip("Volume relatif d'une note complètement usée.")]
        [SerializeField, Range(0f, 1f)] float wornGain = 0.12f;

        [Tooltip("Coupure du passe-bas pour une note à peine usée, en Hz.")]
        [SerializeField] float slightlyWornLowpassHz = 3000f;

        [Tooltip("Coupure du passe-bas pour une note complètement usée, en Hz.")]
        [SerializeField] float wornLowpassHz = 220f;

        [Tooltip("Attaque d'une note complètement usée, en secondes (plus lente = plus feutrée).")]
        [SerializeField, Range(0.001f, 0.3f)] float wornAttack = 0.05f;

        struct Voice
        {
            public bool Active;
            public int Delay;          // échantillons à attendre avant de commencer
            public double Phase;
            public double PhaseIncrement;
            public int Age;
            public int AttackSamples;
            public float Gain;
            public float FilterCoefficient; // 1 = pas de filtre
            public float FilterState1;
            public float FilterState2;
        }

        struct NoteRequest
        {
            public long StartSample;   // -1 = dès que possible
            public double Frequency;
            public float Gain;
            public float LowpassHz;
            public float Attack;
        }

        const double TwoPi = System.Math.PI * 2.0;
        const int MaxPending = 128;
        const int MaxScheduled = 512;

        readonly object pendingLock = new object();
        readonly NoteRequest[] pending = new NoteRequest[MaxPending];
        int pendingCount;

        // Utilisés uniquement par le thread audio.
        readonly NoteRequest[] scheduled = new NoteRequest[MaxScheduled];
        int scheduledCount;
        Voice[] voices;

        int sampleRate;
        long samplePosition; // échantillons déjà rendus depuis le démarrage

        volatile float attackSeconds;
        volatile float decaySeconds;
        volatile float master;

        /// <summary>
        /// L'horloge audio, en secondes depuis le démarrage du synthé. Elle avance par blocs
        /// et vaut toujours le début du prochain bloc qui sera rendu.
        /// </summary>
        public double AudioTime => Interlocked.Read(ref samplePosition) / (double)sampleRate;

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

        /// <summary>
        /// Le timbre d'une note usée. Les trois courbes sont exponentielles entre « neuf » et « usé » :
        /// chaque étape d'usure retire la même proportion de volume et de brillance.
        /// </summary>
        public NoteTimbre Worn(float amount01)
        {
            float a = Mathf.Clamp01(amount01);
            if (a <= 0f) return NoteTimbre.Fresh;
            return new NoteTimbre
            {
                Gain = Mathf.Pow(Mathf.Max(wornGain, 1e-4f), a),
                LowpassHz = slightlyWornLowpassHz * Mathf.Pow(wornLowpassHz / slightlyWornLowpassHz, a),
                Attack = attack * Mathf.Pow(wornAttack / attack, a),
            };
        }

        /// <summary>Joue des notes MIDI tout de suite, avec un timbre neuf.</summary>
        public void PlayNotes(IReadOnlyList<int> midiNotes) => PlayNotes(midiNotes, NoteTimbre.Fresh, -1.0);

        /// <summary>
        /// Joue des notes MIDI ensemble. <paramref name="atTime"/> est un instant de <see cref="AudioTime"/> ;
        /// négatif = dès que possible, passé = immédiatement. Le volume par note baisse avec la taille de l'accord.
        /// </summary>
        public void PlayNotes(IReadOnlyList<int> midiNotes, NoteTimbre timbre, double atTime)
        {
            if (midiNotes == null || midiNotes.Count == 0) return;
            float gain = noteVolume * timbre.Gain / Mathf.Sqrt(midiNotes.Count);
            long start = atTime < 0 ? -1 : (long)System.Math.Round(atTime * sampleRate);
            lock (pendingLock)
            {
                for (int i = 0; i < midiNotes.Count && pendingCount < MaxPending; i++)
                {
                    pending[pendingCount++] = new NoteRequest
                    {
                        StartSample = start,
                        Frequency = MidiToFrequency(midiNotes[i]),
                        Gain = gain,
                        LowpassHz = timbre.LowpassHz,
                        Attack = timbre.Attack,
                    };
                }
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (voices == null) return;

            int frames = data.Length / channels;
            long blockStart = Interlocked.Read(ref samplePosition);

            lock (pendingLock)
            {
                for (int i = 0; i < pendingCount && scheduledCount < MaxScheduled; i++)
                    scheduled[scheduledCount++] = pending[i];
                pendingCount = 0;
            }

            // Démarre les notes qui tombent dans ce bloc, avec le bon décalage.
            for (int i = scheduledCount - 1; i >= 0; i--)
            {
                long offset = scheduled[i].StartSample < 0 ? 0 : scheduled[i].StartSample - blockStart;
                if (offset >= frames) continue;
                StartVoice(scheduled[i], (int)System.Math.Max(0, offset));
                scheduled[i] = scheduled[--scheduledCount];
            }

            int decaySamples = Mathf.Max(1, (int)(decaySeconds * sampleRate));
            float k = 6.9077553f / decaySamples; // exp(-k * decaySamples) = 0.001 (-60 dB)
            float gainOut = master;

            for (int f = 0; f < frames; f++)
            {
                float sum = 0f;
                for (int v = 0; v < voices.Length; v++)
                {
                    ref Voice voice = ref voices[v];
                    if (!voice.Active) continue;
                    if (voice.Delay > 0) { voice.Delay--; continue; }

                    float envelope = voice.Age < voice.AttackSamples
                        ? (float)voice.Age / voice.AttackSamples
                        : Mathf.Exp(-k * (voice.Age - voice.AttackSamples));

                    float s = (float)System.Math.Sin(voice.Phase) * envelope * voice.Gain;

                    // Passe-bas à deux pôles : retire les aigus, donc atténue surtout les notes hautes.
                    float c = voice.FilterCoefficient;
                    voice.FilterState1 += c * (s - voice.FilterState1);
                    voice.FilterState2 += c * (voice.FilterState1 - voice.FilterState2);
                    sum += voice.FilterState2;

                    voice.Phase += voice.PhaseIncrement;
                    if (voice.Phase >= TwoPi) voice.Phase -= TwoPi;
                    if (++voice.Age >= voice.AttackSamples + decaySamples) voice.Active = false;
                }

                float sample = SoftClip(sum * gainOut);
                int o = f * channels;
                for (int ch = 0; ch < channels; ch++)
                    data[o + ch] = sample;
            }

            Interlocked.Add(ref samplePosition, frames);
        }

        void StartVoice(NoteRequest request, int delay)
        {
            // Une voix libre, sinon on vole la plus ancienne.
            int target = 0;
            int oldestAge = -1;
            for (int v = 0; v < voices.Length; v++)
            {
                if (!voices[v].Active) { target = v; break; }
                if (voices[v].Age > oldestAge) { oldestAge = voices[v].Age; target = v; }
            }

            float attackS = request.Attack > 0f ? request.Attack : attackSeconds;
            float coefficient = request.LowpassHz > 0f && request.LowpassHz < sampleRate * 0.45f
                ? 1f - Mathf.Exp(-2f * Mathf.PI * request.LowpassHz / sampleRate)
                : 1f;

            voices[target] = new Voice
            {
                Active = true,
                Delay = delay,
                Phase = 0.0,
                PhaseIncrement = TwoPi * request.Frequency / sampleRate,
                Age = 0,
                AttackSamples = Mathf.Max(1, (int)(attackS * sampleRate)),
                Gain = request.Gain,
                FilterCoefficient = coefficient,
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
