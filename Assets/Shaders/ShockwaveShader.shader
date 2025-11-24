Shader "Custom/ShockwaveParabola"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Radius ("Radius", Float) = 0
        _Thickness ("Thickness", Float) = 0.15
        _Origin ("Origin", Vector) = (0,0,0,0)

        _A ("A", Float) = 0.5
        _B ("B", Float) = 0
        _C ("C", Float) = 0

        _CurveThickness ("Curve Thickness", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Radius;
            float _Thickness;
            float4 _Origin;

            float _A;
            float _B;
            float _C;
            float _CurveThickness;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // Onda circular
            float ring(float dist, float radius, float thickness)
            {
                float edge = abs(dist - radius);
                float smoothing = thickness * 0.5;
                return 1 - smoothstep(smoothing, smoothing * 2, edge);
            }

            // Curva y=ax²+bx+c com expansão tipo onda
            float curveWave(float3 worldPos)
            {
                float x = worldPos.x - _Origin.x;
                float z = worldPos.z - _Origin.z;

                // valor correto da parábola
                float yCurve = _A*x*x + _B*x + _C;

                // "distância vertical" à parábola (define a linha)
                float dy = abs(z - yCurve);
                float lineMask = 1 - smoothstep(_CurveThickness, _CurveThickness*2, dy);

                // distância horizontal ao longo da parábola (define o avanço da onda)
                float tangentDist = abs(x);

                // linha só aparece quando a onda chega nela
                float waveMask = 1 - smoothstep(_Radius - _Thickness, _Radius + _Thickness, tangentDist);

                return waveMask * lineMask;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 xz = i.worldPos.xz;
                float2 oxz = _Origin.xz;

                float d = distance(xz, oxz);
                float circle = ring(d, _Radius, _Thickness);

                float parabola = curveWave(i.worldPos);

                float alpha = saturate(circle + parabola) * _Color.a;

                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
