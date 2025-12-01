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

            float distanceToFunction(float2 point)
            {
                float x = point.x;
                float z = point.y;
                
                // Amostra múltiplos pontos ao redor para encontrar o mais próximo
                float minDist = 999999.0;
                int samples = 16; // Mais amostras = mais preciso
                
                // Define range de busca baseado na posição
                float searchRange = max(2.0, abs(x) * 0.5);
                
                for(int i = 0; i < samples; i++)
                {
                    float t = float(i) / float(samples - 1);
                    float testX = x + (t - 0.5) * searchRange;
                    
                    float funcY = sampleFunction(testX);
                    float dist = length(float2(testX - x, funcY - z));
                    minDist = min(minDist, dist);
                }
                
                return minDist;
            }

            float functionWave(float3 worldPos)
            {
                float x = worldPos.x - _Origin.x;
                float z = worldPos.z - _Origin.z;
                
                // Distância radial do centro
                float radialDist = length(float2(x, z));
                
                // Máscara circular - só desenha na banda da shockwave
                float bandWidth = _Thickness * 2.0;
                float waveMask = 1.0 - smoothstep(0.0, bandWidth, abs(radialDist - _Radius));
                
                if(waveMask < 0.01) return 0.0;
                
                // Calcula distância do ponto atual para a curva
                float distToCurve = distanceToFunction(float2(x, z));
                
                // Espessura adaptativa - mais grossa perto da origem onde a função varia mais
                float adaptiveThickness = _CurveThickness * (1.0 + 3.0 / max(1.0, abs(x) + 1.0));
                
                // Máscara da curva
                float curveMask = 1.0 - smoothstep(adaptiveThickness * 0.5, adaptiveThickness * 1.5, distToCurve);
                
                return waveMask * curveMask;
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