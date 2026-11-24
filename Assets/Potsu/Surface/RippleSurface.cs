using System;
using UnityEngine;

namespace Potsu.Surface
{
    /// <summary>
    /// Surface : l'écran de jeu. Affiche les ondes (shader Potsu/Ripple sur un quad plein écran)
    /// et transforme les appuis en « frappes » : position + charge (0 à 1).
    /// </summary>
    public sealed class RippleSurface : MonoBehaviour
    {
        public const int MaxRipples = 64; // doit correspondre à MAX_RIPPLES dans PotsuRipple.shader

        [Tooltip("Durée de maintien pour une charge complète, en secondes.")]
        [SerializeField, Min(0.05f)] float fullChargeSeconds = 1.5f;

        [Tooltip("Amplitude d'une onde pour un clic bref et pour une charge complète.")]
        [SerializeField] Vector2 amplitudeRange = new Vector2(0.6f, 1.6f);

        public enum Style
        {
            Liquid,
            OneBit,
        }

        [Header("Rendu")]
        [Tooltip("Liquide : reflets sur fond noir. 1-bit : encre noire sur blanc, pixels agrandis, tramage.")]
        [SerializeField] Style style = Style.OneBit;

        [Tooltip("Taille d'un pixel en style 1-bit, en pixels écran (4 sur un écran Retina ≈ pixels doublés).")]
        [SerializeField, Range(1, 16)] int pixelSize = 4;

        static readonly int OneBitId = Shader.PropertyToID("_OneBit");
        static readonly int PixelSizeId = Shader.PropertyToID("_PixelSize");
        static readonly int ResolutionId = Shader.PropertyToID("_Resolution");
        static readonly int RipplesId = Shader.PropertyToID("_Ripples");
        static readonly int RippleCountId = Shader.PropertyToID("_RippleCount");
        static readonly int NowId = Shader.PropertyToID("_Now");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");

        readonly Vector4[] ripples = new Vector4[MaxRipples];
        readonly PointerTracker tracker = new PointerTracker();
        int nextRipple;
        int rippleCount;

        Camera view;
        Transform quad;
        Material material;

        /// <summary>Une frappe sur la surface : position écran (pixels), charge 0-1. Émise au relâchement.</summary>
        public event Action<Vector2, float> Struck;

        public Material Material => material;

        void Awake()
        {
            SetupCamera();
            SetupQuad();
            tracker.Released += (id, position, held) => Struck?.Invoke(position, Mathf.Clamp01(held / fullChargeSeconds));
        }

        void Update()
        {
            tracker.Poll(Time.unscaledTime);
            FitQuadToView();
            material.SetFloat(NowId, Time.time);
            material.SetFloat(AspectId, Aspect);
            material.SetFloat(OneBitId, style == Style.OneBit ? 1f : 0f);
            material.SetFloat(PixelSizeId, pixelSize);
            material.SetVector(ResolutionId, new Vector4(Screen.width, Screen.height, 0f, 0f));
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        static float Aspect => Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;

        /// <summary>
        /// Lance une onde à une position écran (pixels), avec une charge 0-1.
        /// <paramref name="strength"/> multiplie l'amplitude (1 = onde neuve, moins pour un écho).
        /// </summary>
        public void SpawnRipple(Vector2 screenPosition, float charge01, float strength = 1f)
        {
            // Coordonnées de surface : centre de l'écran = (0, 0), hauteur de l'écran = 1.
            var p = new Vector2(
                (screenPosition.x / Mathf.Max(1, Screen.width) - 0.5f) * Aspect,
                screenPosition.y / Mathf.Max(1, Screen.height) - 0.5f);

            float amplitude = Mathf.Lerp(amplitudeRange.x, amplitudeRange.y, Mathf.Clamp01(charge01)) * strength;
            ripples[nextRipple] = new Vector4(p.x, p.y, Time.time, amplitude);
            nextRipple = (nextRipple + 1) % MaxRipples;
            rippleCount = Mathf.Min(rippleCount + 1, MaxRipples);

            material.SetVectorArray(RipplesId, ripples);
            material.SetFloat(RippleCountId, rippleCount);
        }

        void SetupCamera()
        {
            view = Camera.main;
            if (view == null)
            {
                var go = new GameObject("Potsu Camera");
                go.tag = "MainCamera";
                view = go.AddComponent<Camera>();
            }

            view.orthographic = true;
            view.orthographicSize = 1f;
            view.nearClipPlane = 0.01f;
            view.farClipPlane = 10f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Color.black;

            if (FindAnyObjectByType<AudioListener>() == null)
                view.gameObject.AddComponent<AudioListener>();
        }

        void SetupQuad()
        {
            var shader = Shader.Find("Potsu/Ripple");
            if (shader == null)
            {
                Debug.LogError("[Potsu] Shader Potsu/Ripple introuvable.");
                enabled = false;
                return;
            }

            material = new Material(shader) { name = "Potsu Ripple (instance)" };
            material.SetVectorArray(RipplesId, ripples); // fixe la taille du tableau une fois pour toutes

            var go = new GameObject("Potsu Surface");
            go.AddComponent<MeshFilter>().sharedMesh = BuildQuadMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            quad = go.transform;
            FitQuadToView();
        }

        static Mesh BuildQuadMesh()
        {
            var mesh = new Mesh { name = "Potsu Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f, 1f));
            return mesh;
        }

        void FitQuadToView()
        {
            if (quad == null || view == null) return;
            float height = view.orthographicSize * 2f;
            var t = view.transform;
            quad.SetPositionAndRotation(t.position + t.forward * 1f, t.rotation);
            quad.localScale = new Vector3(height * view.aspect, height, 1f);
        }
    }
}
