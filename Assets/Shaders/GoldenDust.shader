Shader "Custom/GoldenDust"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        [HDR] _TintColor ("Tint Color", Color) = (2, 1.6, 0.5, 1) // Default Gold
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Blend SrcAlpha One
        ColorMask RGB
        Cull Off 
        Lighting Off 
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float _InvFade;
            CBUFFER_END

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _TintColor;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Procedural Soft Circle to prevent "Square Particles"
                float dist = length(input.uv - 0.5);
                float circle = saturate(1.0 - smoothstep(0.2, 0.5, dist) * 2.0); // Soft core, faded edge
                
                half4 col = 2.0f * input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                col.a *= circle; // Force circular alpha mask
                
                // Boost center brightness for "White Core" look
                float core = smoothstep(0.5, 0.0, dist); 
                col.rgb += core * 0.5; // Add extra white to center

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Particles/Additive"
}
