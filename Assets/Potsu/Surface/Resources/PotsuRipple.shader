// Potsu — ondes sur une surface liquide, entièrement procédurales.
// Chaque onde est un paquet d'ondes circulaire qui s'étend, s'atténue et s'additionne aux autres.
// On calcule une hauteur, on en déduit une normale, et on éclaire cette normale : c'est ce qui donne
// l'aspect « liquide » (reflets sur les pentes) plutôt que de simples cercles dessinés.
Shader "Potsu/Ripple"
{
    Properties
    {
        _Speed ("Vitesse (hauteurs d'écran / s)", Float) = 0.3
        _Wavelength ("Longueur d'onde", Float) = 0.045
        _PacketWidth ("Largeur du paquet", Float) = 0.05
        _Damping ("Amortissement", Float) = 0.7
        _Lifetime ("Durée de vie (s)", Float) = 5
        _Slope ("Relief", Float) = 0.012
        _Intensity ("Intensité des reflets", Float) = 1
        _BaseColor ("Couleur de fond", Color) = (0, 0, 0, 1)
        _LightColor ("Couleur des reflets", Color) = (0.75, 0.85, 1, 1)
        _LightDir ("Direction de la lumière", Vector) = (-0.5, 0.7, 0.6, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_RIPPLES 32
            #define TWO_PI 6.2831853

            // x, y : centre (centre de l'écran = 0, hauteur de l'écran = 1) ; z : instant de départ ; w : amplitude
            float4 _Ripples[MAX_RIPPLES];
            float _RippleCount;
            float _Now;
            float _Aspect;

            float _Speed, _Wavelength, _PacketWidth, _Damping, _Lifetime, _Slope, _Intensity;
            float4 _BaseColor, _LightColor, _LightDir;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Height (float2 p)
            {
                float h = 0;
                int count = (int)_RippleCount;
                for (int i = 0; i < MAX_RIPPLES; i++)
                {
                    if (i >= count) break;
                    float4 r = _Ripples[i];
                    float t = _Now - r.z;
                    if (t < 0 || t > _Lifetime) continue;

                    float d = distance(p, r.xy);
                    float x = d - _Speed * t;                                   // distance au front d'onde
                    float packet = exp(-(x * x) / (2.0 * _PacketWidth * _PacketWidth));
                    float fade = exp(-_Damping * t) * saturate(1.0 - t / _Lifetime);
                    float spread = 1.0 / (1.0 + 5.0 * d);                        // l'énergie se dilue en s'étalant
                    h += r.w * sin(x * TWO_PI / _Wavelength) * packet * fade * spread;
                }
                return h;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1.0);

                const float e = 0.0015;
                float h = Height(p);
                float2 slope = float2(Height(p + float2(e, 0)) - h, Height(p + float2(0, e)) - h) / e;

                float3 n = normalize(float3(-slope * _Slope, 1.0));
                float3 l = normalize(_LightDir.xyz);

                // Éclairage relatif à une surface plate : une eau immobile reste noire.
                float diffuse = max(0.0, dot(n, l) - l.z);
                float specular = pow(saturate(dot(reflect(-l, n), float3(0, 0, 1))), 48.0);

                float3 color = _BaseColor.rgb + _LightColor.rgb * (diffuse * 1.5 + specular) * _Intensity;
                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}
