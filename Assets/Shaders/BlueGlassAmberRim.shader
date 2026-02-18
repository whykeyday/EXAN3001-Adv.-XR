Shader "Custom/BlueGlassAmberRim"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.0, 0.5, 1.0, 0.3)
        [HDR] _RimColor ("Rim Color", Color) = (1.0, 0.6, 0.0, 1.0)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0.0, 5.0)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+1" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = GetWorldSpaceViewDir(input.positionWS);
                
                // Fresnel / Rim calculation
                // dot(N, V) determines angle. 1.0 when facing, 0.0 when perpendicular/rim.
                // We want rim to be 1.0 at edges.
                // N needs to be normalized. V is usually normalized by GetWorldSpaceViewDir but let's be safe.
                float3 normalWS = normalize(input.normalWS);
                
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float rim = 1.0 - NdotV;
                
                // Sharpen the rim
                rim = pow(rim, _RimPower);
                
                // Apply Rim Color and Intensity
                // Rim only adds to the localized area
                half3 rimEmission = _RimColor.rgb * rim * _RimIntensity;
                
                // Mix with Base Color
                // We want base color everywhere, plus rim at edges.
                // Since this is transparent, we might want to Add? Or Mix?
                // Let's take Base as background.
                half3 finalColor = _BaseColor.rgb + rimEmission;
                
                // Alpha Logic:
                // Base alpha is constant. Rim should make it more opaque?
                // Or standard glass where edges are clearer or more opaque?
                // Let's keep alpha as base alpha + rim alpha contribution (faintly)
                half finalAlpha = saturate(_BaseColor.a + (rim * 0.5));
                
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
