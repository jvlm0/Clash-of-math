Shader "Custom/ShockwaveShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Radius ("Radius", Float) = 0
        _Thickness ("Thickness", Float) = 0.15
        _Origin ("Origin", Vector) = (0,0,0,0)
        _CurveThickness ("Curve Thickness", Float) = 0.1
        
        _FunctionLUT ("Function LUT", 2D) = "white" {}
        _LUTRange ("LUT Range", Float) = 50
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
            float _CurveThickness;
            
            sampler2D _FunctionLUT;
            float _LUTRange;

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

            float ring(float dist, float radius, float thickness)
            {
                float edge = abs(dist - radius);
                float smoothing = thickness * 0.5;
                return 1 - smoothstep(smoothing, smoothing * 2, edge);
            }

            float sampleFunction(float x)
            {
                float t = (x + _LUTRange * 0.5) / _LUTRange;
                t = saturate(t);
                return tex2D(_FunctionLUT, float2(t, 0.5)).r;
            }

            float functionWave(float3 worldPos)
            {
                float x = worldPos.x - _Origin.x;
                float z = worldPos.z - _Origin.z;
                
                // Distância radial do centro
                float radialDist = length(float2(x, z));
                
                // Só desenha se estiver próximo ao raio atual (banda circular)
                float waveMask = 1 - smoothstep(_Radius - _Thickness, _Radius + _Thickness, abs(radialDist - _Radius));
                
                // Se não está na banda da onda, retorna 0
                if(waveMask < 0.01) return 0;
                
                // Valor da função no ponto X
                float yFunction = sampleFunction(x);
                
                // Distância vertical à função
                float dy = abs(z - yFunction);
                float lineMask = 1 - smoothstep(_CurveThickness, _CurveThickness*2, dy);

                return waveMask * lineMask;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 xz = i.worldPos.xz;
                float2 oxz = _Origin.xz;

                float d = distance(xz, oxz);
                float circle = ring(d, _Radius, _Thickness);

                float curve = functionWave(i.worldPos);

                float alpha = saturate(circle + curve) * _Color.a;

                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}