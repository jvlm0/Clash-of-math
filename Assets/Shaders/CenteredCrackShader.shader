// == == == == == == == == == == SHADER COM FENDA PROCEDURAL NO CENTRO == == == == == == == == == ==
// Gera fenda diretamente no shader, sem necessidade de textura externa
// Salve como : Assets / Shaders / CenteredCrackShader.shader

Shader "Custom/CenteredCrackShader"
{
    Properties
    {
        [Header(Ground)]
        _MainTex ("Ground Texture", 2D) = "white" {}
        _NormalMap ("Ground Normal", 2D) = "bump" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        [Header(Crack Position)]
        _CrackCenter ("Crack Center (X, Y)", Vector) = (0.5, 0.5, 0, 0)
        _CrackAngle ("Crack Angle", Range(0, 360)) = 0

        [Header(Crack Shape)]
        _CrackLength ("Crack Length", Range(0, 2)) = 0.6
        _CrackWidth ("Crack Width", Range(0, 0.5)) = 0.05
        _CrackDepth ("Crack Depth", Range(0, 3)) = 1.0
        _Roughness ("Edge Roughness", Range(0, 1)) = 0.3

        [Header(Branch Settings)]
        _BranchCount ("Branch Count", Range(0, 6)) = 2
        _BranchAngle ("Branch Angle", Range(0, 90)) = 45
        _BranchLength ("Branch Length", Range(0, 1)) = 0.5

        [Header(Crack Appearance)]
        _CrackColor ("Crack Color", Color) = (0.05, 0.05, 0.05, 1)
        _EdgeColor ("Edge Color", Color) = (0.3, 0.25, 0.2, 1)
        _CrackRoughness ("Crack Roughness", Range(0, 1)) = 0.9

        [Header(Animation)]
        _FormationProgress ("Formation Progress", Range(0, 1)) = 1.0
        _AnimationSeed ("Animation Seed", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.5

        sampler2D _MainTex;
        sampler2D _NormalMap;
        //float4 _MainTex_ST;

        float2 _CrackCenter;
        float _CrackAngle;
        float _CrackLength;
        float _CrackWidth;
        float _CrackDepth;
        float _Roughness;

        int _BranchCount;
        float _BranchAngle;
        float _BranchLength;

        float4 _CrackColor;
        float4 _EdgeColor;
        float _Smoothness;
        float _CrackRoughness;

        float _FormationProgress;
        float _AnimationSeed;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float crackMask;
        };

        // Função de ruído para variação
        float noise(float2 p)
        {
            return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453 * _AnimationSeed);
        }

        // Rotação 2D
        float2 rotate2D(float2 p, float angle)
        {
            float rad = angle * 0.01745329; // graus para radianos
            float s = sin(rad);
            float c = cos(rad);
            return float2(
            p.x * c - p.y * s,
            p.x * s + p.y * c
            );
        }

        // Calcula distância até uma linha (fenda)
        float lineDistance(float2 p, float2 start, float2 end, float width)
        {
            float2 pa = p - start;
            float2 ba = end - start;
            float h = saturate(dot(pa, ba) / dot(ba, ba));
            float dist = length(pa - ba * h);

            // Adiciona rugosidade
            float noiseVal = noise(p * 20.0 + h * 10.0);
            dist += (noiseVal - 0.5) * _Roughness * width;

            return dist;
        }

        // Função principal que calcula a máscara da fenda
        float calculateCrackMask(float2 uv)
        {
            // Translada para o centro
            float2 p = uv - _CrackCenter;

            // Rotaciona
            p = rotate2D(p, _CrackAngle);

            float mask = 0.0;
            float maxLength = _CrackLength * _FormationProgress;

            // Fenda principal
            float2 crackStart = float2(- maxLength * 0.5, 0);
            float2 crackEnd = float2(maxLength * 0.5, 0);
            float mainDist = lineDistance(p, crackStart, crackEnd, _CrackWidth);
            mask = 1.0 - smoothstep(_CrackWidth * 0.5, _CrackWidth * 1.5, mainDist);

            // Adiciona ramificações
            if (_BranchCount > 0 && _FormationProgress > 0.3)
            {
                float branchProgress = saturate((_FormationProgress - 0.3) / 0.7);

                for (int i = 1; i <= _BranchCount; i ++)
                {
                    // Posição ao longo da fenda principal
                    float branchPos = (float(i) / float(_BranchCount + 1)) * maxLength - maxLength * 0.5;

                    // Alterna entre esquerda e direita
                    float side = (i % 2 == 0) ? 1.0 : - 1.0;

                    // Calcula ângulo da ramificação
                    float angle = _BranchAngle * side;
                    float2 branchDir = rotate2D(float2(1, 0), angle);

                    float2 branchStart = float2(branchPos, 0);
                    float2 branchEnd = branchStart + branchDir * _BranchLength * _CrackLength * branchProgress;

                    float branchDist = lineDistance(p, branchStart, branchEnd, _CrackWidth * 0.7);
                    float branchMask = 1.0 - smoothstep(_CrackWidth * 0.35, _CrackWidth * 1.0, branchDist);

                    mask = max(mask, branchMask);
                }
            }

            return mask;
        }

        // Vertex shader - deforma geometria
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float2 uv = v.texcoord.xy;
            float mask = calculateCrackMask(uv);

            // Displacement com falloff suave
            float displacement = pow(mask, 1.5) * _CrackDepth;
            v.vertex.xyz -= v.normal * displacement;

            o.crackMask = mask;
        }

        // Fragment shader
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Textura base
            float4 groundColor = tex2D(_MainTex, IN.uv_MainTex);
            float3 groundNormal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));

            // Recalcula máscara para efeitos de superfície
            float crackMask = IN.crackMask;

            // Gradientes
            float edgeMask = smoothstep(0.3, 0.6, crackMask);
            float innerMask = smoothstep(0.6, 0.9, crackMask);

            // Mistura de cores
            float3 finalColor = groundColor.rgb;
            finalColor = lerp(finalColor, _EdgeColor.rgb, edgeMask * 0.9);
            finalColor = lerp(finalColor, _CrackColor.rgb, innerMask);

            // Adiciona variação
            float detail = noise(IN.uv_MainTex * 30.0);
            finalColor *= lerp(1.0, detail * 0.5 + 0.75, crackMask * 0.3);

            o.Albedo = finalColor;

            // Normal perturbation
            float3 crackNormal = float3(
            (noise(IN.uv_MainTex * 25.0) - 0.5) * 2.0,
            (noise(IN.uv_MainTex * 25.0 + 0.5) - 0.5) * 2.0,
            1.0
            );
            o.Normal = lerp(groundNormal, normalize(crackNormal), crackMask * 0.7);

            o.Smoothness = lerp(_Smoothness, 1.0 - _CrackRoughness, crackMask);
            o.Occlusion = lerp(1.0, 0.3, innerMask);
        }
        ENDCG
    }

    FallBack "Standard"
}