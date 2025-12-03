Shader "Custom/AdvancedGroundCrack"
{
    Properties
    {
        [Header(Ground Textures)]
        _MainTex ("Ground Albedo", 2D) = "white" {}
        _NormalMap ("Ground Normal", 2D) = "bump" {}
        _Smoothness ("Ground Smoothness", Range(0,1)) = 0.5
        
        [Header(Crack Settings)]
        _CrackMask ("Crack Mask (R=depth, G=edge)", 2D) = "black" {}
        _CrackDepth ("Crack Depth", Range(0, 5)) = 1.0
        _CrackWidth ("Crack Width", Range(0.5, 2)) = 1.0
        
        [Header(Crack Appearance)]
        _CrackColor ("Crack Inner Color", Color) = (0.05, 0.05, 0.05, 1)
        _EdgeColor ("Crack Edge Color", Color) = (0.3, 0.25, 0.2, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.1
        _CrackRoughness ("Crack Roughness", Range(0,1)) = 0.9
        
        [Header(Detail)]
        _DetailNoise ("Detail Noise", 2D) = "gray" {}
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.3
        
        [Header(Emission Glow)]
        [Toggle] _UseEmission ("Use Emission", Float) = 0
        _EmissionColor ("Emission Color", Color) = (0, 0.5, 1, 1)
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.5
        
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _CrackMask;
        sampler2D _DetailNoise;
        
        float _Smoothness;
        float _CrackDepth;
        float _CrackWidth;
        float4 _CrackColor;
        float4 _EdgeColor;
        float _EdgeWidth;
        float _CrackRoughness;
        float _DetailStrength;
        
        float _UseEmission;
        float4 _EmissionColor;
        float _EmissionStrength;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_CrackMask;
            float3 worldPos;
            float crackValue;
        };
        
        // Função de ruído procedural para variação
        float noise(float2 uv)
        {
            return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
        }
        
        // Vertex shader - deforma a geometria
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Amostra a máscara de fenda
            float4 crackData = tex2Dlod(_CrackMask, float4(v.texcoord.xy, 0, 0));
            float crackMask = crackData.r * _CrackWidth;
            
            // Adiciona ruído para variação
            float2 noiseUV = v.texcoord.xy * 10.0;
            float detailNoise = tex2Dlod(_DetailNoise, float4(noiseUV, 0, 0)).r;
            detailNoise = (detailNoise - 0.5) * _DetailStrength;
            
            // Calcula deslocamento com falloff suave
            float displacement = pow(crackMask, 1.5) * _CrackDepth;
            displacement += detailNoise * crackMask;
            
            // Desloca vertices ao longo da normal
            v.vertex.xyz -= v.normal * displacement;
            
            // Passa valor para o fragment shader
            o.crackValue = crackMask;
        }
        
        // Fragment shader - define aparência da superfície
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Amostra texturas base
            float4 groundColor = tex2D(_MainTex, IN.uv_MainTex);
            float3 groundNormal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
            
            // Pega dados da fenda
            float4 crackData = tex2D(_CrackMask, IN.uv_CrackMask);
            float crackMask = saturate(crackData.r * _CrackWidth);
            float edgeMask = crackData.g; // Canal verde para bordas
            
            // Adiciona variação procedural
            float2 detailUV = IN.uv_MainTex * 15.0;
            float detail = tex2D(_DetailNoise, detailUV).r;
            
            // Cria gradiente de borda
            float edgeGradient = smoothstep(0.3, 0.7, crackMask);
            float innerCrack = smoothstep(0.6, 1.0, crackMask);
            
            // Mistura cores: terreno → borda → interior da fenda
            float3 finalColor = groundColor.rgb;
            finalColor = lerp(finalColor, _EdgeColor.rgb, edgeGradient * 0.8);
            finalColor = lerp(finalColor, _CrackColor.rgb, innerCrack);
            
            // Adiciona variação de detalhe
            finalColor *= lerp(1.0, detail, crackMask * 0.3);
            
            // Define propriedades da superfície
            o.Albedo = finalColor;
            
            // Normal mapping - mais rugoso na fenda
            float3 crackNormal = float3(
                (detail - 0.5) * 2.0,
                (noise(IN.uv_MainTex * 20) - 0.5) * 2.0,
                1.0
            );
            o.Normal = lerp(groundNormal, normalize(crackNormal), crackMask * 0.7);
            
            // Suavidade - fenda é mais rugosa
            o.Smoothness = lerp(_Smoothness, 1.0 - _CrackRoughness, crackMask);
            
            // Oclusão ambiental na fenda
            o.Occlusion = lerp(1.0, 0.3, innerCrack);
            
            // Emissão opcional (para efeito de lava/energia)
            if (_UseEmission > 0.5)
            {
                float emissionMask = pow(innerCrack, 2.0) * (0.5 + detail * 0.5);
                o.Emission = _EmissionColor.rgb * emissionMask * _EmissionStrength;
            }
        }
        ENDCG
    }
    
    FallBack "Standard"
}