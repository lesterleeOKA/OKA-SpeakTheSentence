Shader "UI/RoundedRawImage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RadiusX ("Corner Radius X", Range(0,0.5)) = 0.2
        _RadiusY ("Corner Radius Y", Range(0,0.5)) = 0.2
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 localPos : TEXCOORD1;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _RadiusX;
            float _RadiusY;
            float4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.localPos = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.localPos;
                float rx = _RadiusX;
                float ry = _RadiusY;

                // Calculate the minimum distance to each edge
                float2 edgeDist = min(uv, 1.0 - uv);

                float alpha = 1.0;
                if (edgeDist.x < rx && edgeDist.y < ry)
                {
                    float2 cornerCenter = float2(
                        uv.x < 0.5 ? rx : 1.0 - rx,
                        uv.y < 0.5 ? ry : 1.0 - ry
                    );
                    float2 diff = uv - cornerCenter;
                    // Ellipse equation: (x/rx)^2 + (y/ry)^2 <= 1
                    if ((diff.x * diff.x) / (rx * rx) + (diff.y * diff.y) / (ry * ry) > 1.0)
                        alpha = 0.0;
                }

                half4 col = tex2D(_MainTex, i.uv) * _Color;
                col *= i.color; // <-- This line enables CanvasGroup fading!
                col.a *= alpha;
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "UI/Default"
}