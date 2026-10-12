Shader "AnimalCrossing/LiquidGlass"
{
    Properties
    {
        [MainTexture] _BaseMap("Palette Atlas Texture", 2D) = "white" {}
        [MainColor]   _BaseColor("Liquid / Glass Color Tint", Color) = (0.35, 0.72, 0.88, 0.65)

        [KeywordEnum(Glass, Water)] _Mode("Material Mode", Float) = 1.0 // 0 = Glass, 1 = Water
        _BaseAlpha("Base Transparency", Range(0.0, 1.0)) = 0.55
        _FresnelAlpha("Fresnel Edge Alpha", Range(0.0, 1.0)) = 0.88
        _FresnelPower("Fresnel Power", Range(0.5, 8.0)) = 3.2

        _Smoothness("Smoothness / Glossiness", Range(0.0, 1.0)) = 0.95
        _SpecularColor("Specular Highlight Color", Color) = (1.0, 1.0, 1.0, 1.0)

        // Water Wave & Ripple Settings
        _WaveSpeed("Wave Motion Speed", Float) = 1.5
        _WaveFrequency("Wave Spatial Frequency", Float) = 3.5
        _WaveAmplitude("Wave Surface Amplitude", Float) = 0.015
        _RippleScale("Ripple Noise Scale", Float) = 6.0
        _RippleDistortion("Ripple Normal Perturbation", Range(0.0, 0.5)) = 0.12

        // Shoreline / Basin Depth Foam
        _FoamColor("Shore / Rim Foam Color", Color) = (0.92, 0.96, 1.0, 0.85)
        _FoamDistance("Depth Foam Distance", Float) = 0.06

        [HideInInspector] _Cull("__cull", Float) = 0.0 // Two-sided for glass vessels & fluids
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex ForwardPassVertex
            #pragma fragment ForwardPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Mode;
                half _BaseAlpha;
                half _FresnelAlpha;
                half _FresnelPower;
                half _Smoothness;
                half4 _SpecularColor;
                half _WaveSpeed;
                half _WaveFrequency;
                half _WaveAmplitude;
                half _RippleScale;
                half _RippleDistortion;
                half4 _FoamColor;
                half _FoamDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                half3 normalWS      : TEXCOORD2;
                half fogFactor      : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ForwardPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // Water mode surface wave bobbing (bypassed in Glass mode)
                float wave = sin(positionWS.x * _WaveFrequency + _Time.y * _WaveSpeed) *
                             cos(positionWS.z * _WaveFrequency * 1.25 + _Time.y * _WaveSpeed * 0.85);
                positionWS.y += wave * (_WaveAmplitude * _Mode);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 ForwardPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 albedo = texColor.rgb;

                float3 baseNormalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Procedural ripple normal perturbation (scaled by _Mode)
                float r1 = sin((input.positionWS.x + input.positionWS.z) * _RippleScale + _Time.y * _WaveSpeed);
                float r2 = cos((input.positionWS.x - input.positionWS.z) * _RippleScale * 1.3 - _Time.y * _WaveSpeed * 0.9);
                float3 normalWS = normalize(baseNormalWS + float3(r1, 0, r2) * (_RippleDistortion * _Mode));

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                // Ambient light
                half3 ambient = inputData.bakedGI * albedo * 0.6;

                // Main Light
                Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
                half3 lightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);

                // Diffuse transmission / reflection
                half halfLambert = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                half3 diffuse = albedo * lightColor * halfLambert;

                // Crisp Specular Blinn-Phong Highlight
                half3 halfVec = SafeNormalize(float3(mainLight.direction) + float3(viewDirWS));
                half NdotH = saturate(dot(normalWS, halfVec));
                half specPower = lerp(32.0, 256.0, _Smoothness);
                half spec = pow(NdotH, specPower);
                half3 specular = _SpecularColor.rgb * spec * lightColor;

                // Additional Realtime Lights specular glint
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
                    half3 addLightColor = addLight.color * (addLight.distanceAttenuation * addLight.shadowAttenuation);
                    half3 addHalfVec = SafeNormalize(float3(addLight.direction) + float3(viewDirWS));
                    half addNdotH = saturate(dot(normalWS, addHalfVec));
                    half addSpec = pow(addNdotH, specPower);
                    specular += _SpecularColor.rgb * addSpec * addLightColor;
                LIGHT_LOOP_END
                #endif

                // Fresnel Transparency
                half NdotV = saturate(dot(normalWS, viewDirWS));
                half fresnel = pow(1.0 - NdotV, _FresnelPower);
                half alpha = lerp(_BaseAlpha, _FresnelAlpha, fresnel);

                // Depth Fade & Shoreline Foam (in Water Mode)
                #if defined(_REQUIRE_DEPTH_TEXTURE) || true
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float sceneRawDepth = SampleSceneDepth(screenUV);
                float sceneLinearDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float surfaceLinearDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDifference = max(0.0, sceneLinearDepth - surfaceLinearDepth);

                float foam = saturate(1.0 - (depthDifference / max(0.001, _FoamDistance))) * _Mode;
                albedo = lerp(albedo, _FoamColor.rgb, foam * _FoamColor.a);
                alpha = lerp(alpha, 1.0, foam);
                #endif

                half3 finalColor = ambient + diffuse + specular;
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
