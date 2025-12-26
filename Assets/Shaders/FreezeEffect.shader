Shader "Custom/FreezeEffect"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        
        // Propriedades de congelamento
        _FreezeAmount ("Freeze Amount", Range(0, 1)) = 0
        _FreezeColor ("Freeze Color", Color) = (0.6, 0.8, 1, 1)
        _IceTexture ("Ice Texture", 2D) = "white" {}
        _IceNormal ("Ice Normal Map", 2D) = "bump" {}
        
        // Propriedades de cristais
        _CrystalScale ("Crystal Scale", Float) = 5
        _CrystalStrength ("Crystal Strength", Range(0, 1)) = 0.5
        _CrystalHeight ("Crystal Height", Range(0, 0.5)) = 0.1
        _CrystalSharpness ("Crystal Sharpness", Range(1, 10)) = 3
        
        // Propriedades de brilho
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _FreezeSmoothness ("Freeze Smoothness", Range(0, 1)) = 0.9
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _IceTexture;
        sampler2D _IceNormal;
        
        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
        };
        
        half _Glossiness;
        half _Metallic;
        half _FreezeSmoothness;
        fixed4 _Color;
        fixed4 _FreezeColor;
        half _FreezeAmount;
        float _CrystalScale;
        half _CrystalStrength;
        float _CrystalHeight;
        float _CrystalSharpness;
        
        // Função de ruído para cristais
        float noise(float3 p)
        {
            return frac(sin(dot(p, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
        }
        
        // Voronoi melhorado para cristais
        float2 voronoiWithEdge(float3 p)
        {
            float3 i = floor(p);
            float3 f = frac(p);
            
            float minDist1 = 10.0;
            float minDist2 = 10.0;
            
            for(int x = -1; x <= 1; x++)
            {
                for(int y = -1; y <= 1; y++)
                {
                    for(int z = -1; z <= 1; z++)
                    {
                        float3 neighbor = float3(x, y, z);
                        float3 randomPoint = float3(
                            noise(i + neighbor),
                            noise(i + neighbor + 1.5),
                            noise(i + neighbor + 3.7)
                        );
                        float3 diff = neighbor + randomPoint - f;
                        float dist = length(diff);
                        
                        if(dist < minDist1)
                        {
                            minDist2 = minDist1;
                            minDist1 = dist;
                        }
                        else if(dist < minDist2)
                        {
                            minDist2 = dist;
                        }
                    }
                }
            }
            
            return float2(minDist1, minDist2 - minDist1);
        }
        
        // Vertex shader para extrusão dos cristais
        void vert(inout appdata_full v)
        {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 worldNormal = UnityObjectToWorldNormal(v.normal);
            
            // Calcular padrão de cristais
            float3 crystalPos = worldPos * _CrystalScale;
            float2 voronoi = voronoiWithEdge(crystalPos);
            
            // Criar picos nos centros das células voronoi
            float crystalPeak = pow(1.0 - saturate(voronoi.x * 2.0), _CrystalSharpness);
            
            // Extrudir vértices ao longo da normal
            float extrusion = crystalPeak * _CrystalHeight * _FreezeAmount;
            v.vertex.xyz += v.normal * extrusion;
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Textura base
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            // Padrão de cristais usando Voronoi
            float3 worldPos = IN.worldPos * _CrystalScale;
            float2 voronoi = voronoiWithEdge(worldPos);
            
            // F1: distância ao ponto mais próximo
            float crystalBase = voronoi.x;
            // F2-F1: bordas das células (onde os cristais se encontram)
            float crystalEdges = voronoi.y;
            
            // Criar padrão de cristais com picos
            float crystalPattern = pow(1.0 - saturate(crystalBase * 2.0), _CrystalSharpness);
            
            // Intensificar as bordas dos cristais
            float edgeHighlight = saturate(1.0 - crystalEdges * 5.0);
            
            // Textura de gelo
            float2 iceUV = IN.uv_MainTex * 2.0;
            fixed4 iceColor = tex2D(_IceTexture, iceUV);
            
            // Combinar padrões
            float iceFactor = crystalPattern * _CrystalStrength + iceColor.r * 0.3;
            iceFactor = saturate(iceFactor);
            
            // Cor final misturando base com cor de gelo
            fixed3 finalColor = lerp(baseColor.rgb, _FreezeColor.rgb * (0.8 + iceFactor * 0.4), _FreezeAmount);
            
            // Normal map para gelo
            float3 iceNormal = UnpackNormal(tex2D(_IceNormal, iceUV));
            
            // Criar normais procedurais para os cristais
            float3 crystalNormal = float3(0, 0, 1);
            float gradient = length(frac(worldPos) - 0.5) * 2.0;
            crystalNormal.xy = normalize(frac(worldPos.xy) - 0.5) * gradient;
            crystalNormal = normalize(crystalNormal);
            
            // Misturar normais
            float3 finalNormal = lerp(iceNormal, crystalNormal, crystalPattern * _FreezeAmount * 0.5);
            o.Normal = lerp(float3(0, 0, 1), finalNormal, _FreezeAmount);
            
            // Saída
            o.Albedo = finalColor;
            o.Metallic = _Metallic;
            o.Smoothness = lerp(_Glossiness, _FreezeSmoothness, _FreezeAmount);
            o.Alpha = 1.0;
            
            // Brilho nos picos e bordas dos cristais
            float glow = crystalPattern * 0.4 + edgeHighlight * 0.3;
            o.Emission = _FreezeColor.rgb * glow * _FreezeAmount * 0.5;
            
            // Efeito de fresnel nas bordas
            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3.0);
            o.Emission += _FreezeColor.rgb * fresnel * _FreezeAmount * 0.2;
        }
        ENDCG
    }
    
    FallBack "Diffuse"
}