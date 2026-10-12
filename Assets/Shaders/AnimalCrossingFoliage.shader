Shader "AnimalCrossing/Foliage"
{
    Properties
    {
        [MainTexture] _BaseMap("Palette Atlas Texture", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color Tint", Color) = (1, 1, 1, 1)

        _RampThreshold("Ramp Threshold", Range(0.0, 1.0)) = 0.45
        _RampSmoothness("Ramp Smoothness", Range(0.01, 1.0)) = 0.25
        _ShadowColor("Shadow Tint", Color) = (0.58, 0.70, 0.62, 1.0)

        _RimColor("Rim Color", Color) = (0.92, 1.0, 0.82, 1.0)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 2.5
        _RimIntensity("Rim Intensity", Range(0.0, 2.0)) = 0.45

        _CentroidOffsetOS("Centroid Offset (OS)", Vector) = (0, 1.5, 0, 0)
        _NormalSoftening("Centroid Normal Softening", Range(0.0, 1.0)) = 0.85

        _WindSpeed("Wind Speed", Float) = 1.8
        _WindFrequency("Wind Frequency", Vector) = (0.4, 0.3, 0.4, 0.0)
        _WindAmplitude("Wind Amplitude", Float) = 0.06
        _WindDirection("Wind Direction", Vector) = (1.0, 0.0, 0.5, 0.0)
        _WindHeightFactor("Wind Height Anchor Factor", Float) = 0.35

        _TransmissionColor("Backlight Transmission Color", Color) = (0.85, 0.95, 0.45, 1.0)
        _TransmissionStrength("Transmission Strength", Range(0.0, 2.0)) = 0.6
        _TransmissionPower("Transmission Power", Range(0.5, 8.0)) = 2.0

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Cull("__cull", Float) = 0.0 // Default two-sided for foliage/leaves
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex ForwardPassVertex
            #pragma fragment ForwardPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _RimColor;
                half _RampThreshold;
                half _RampSmoothness;
                half _RimPower;
                half _RimIntensity;
                float4 _CentroidOffsetOS;
                half _NormalSoftening;
                half _WindSpeed;
                float4 _WindFrequency;
                half _WindAmplitude;
                float4 _WindDirection;
                half _WindHeightFactor;
                half4 _TransmissionColor;
                half _TransmissionStrength;
                half _TransmissionPower;
                half _Cutoff;
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

            float3 ApplyFoliageWind(float3 positionOS, float3 positionWS)
            {
                float heightWeight = saturate(positionOS.y * _WindHeightFactor);
                float windPhase = _Time.y * _WindSpeed + dot(positionWS, _WindFrequency.xyz);
                float sway = sin(windPhase) * _WindAmplitude;
                sway += sin(windPhase * 2.3 + positionOS.y * 3.5) * (_WindAmplitude * 0.25);
                return positionWS + normalize(_WindDirection.xyz) * (sway * heightWeight);
            }

            float3 ApplyCentroidNormalSoftening(float3 positionOS, float3 normalOS)
            {
                float3 dirFromCentroid = positionOS - _CentroidOffsetOS.xyz;
                float distSq = dot(dirFromCentroid, dirFromCentroid);
                float3 sphericalNormalOS = distSq > 0.0001 ? normalize(dirFromCentroid) : float3(0, 1, 0);
                return normalize(lerp(normalOS, sphericalNormalOS, _NormalSoftening));
            }

            Varyings ForwardPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyFoliageWind(input.positionOS.xyz, positionWS);

                float3 softenedNormalOS = ApplyCentroidNormalSoftening(input.positionOS.xyz, input.normalOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(softenedNormalOS);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half3 ComputeStylizedLight(Light light, float3 normalWS, float3 viewDirWS, half3 albedo)
            {
                half3 lightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                half halfLambert = dot(normalWS, light.direction) * 0.5 + 0.5;
                half ramp = smoothstep(_RampThreshold - _RampSmoothness, _RampThreshold + _RampSmoothness, halfLambert);
                half3 diffuseRamp = lerp(_ShadowColor.rgb, half3(1.0, 1.0, 1.0), ramp);
                half3 directDiffuse = albedo * lightColor * diffuseRamp;

                half NdotV = saturate(dot(normalWS, viewDirWS));
                half rimFactor = pow(1.0 - NdotV, _RimPower);
                half rimLightFacing = saturate(dot(normalWS, light.direction) * 0.5 + 0.5);
                half3 rim = _RimColor.rgb * (rimFactor * _RimIntensity * rimLightFacing) * lightColor;

                return directDiffuse + rim;
            }

            half4 ForwardPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 albedo = texColor.rgb;

                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half3 ambient = inputData.bakedGI * albedo * _ShadowColor.rgb;

                // Main Directional Light
                Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
                half3 directLight = ComputeStylizedLight(mainLight, normalWS, viewDirWS, albedo);

                // Backlight Transmission (Subsurface / leaf translucency)
                half transDot = saturate(dot(-mainLight.direction, viewDirWS));
                half transmissionFactor = pow(transDot, _TransmissionPower) * _TransmissionStrength;
                half3 transmission = mainLight.color * (transmissionFactor * _TransmissionColor.rgb * albedo);

                // Additional Realtime Lights
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
                    directLight += ComputeStylizedLight(addLight, normalWS, viewDirWS, albedo);
                LIGHT_LOOP_END
                #endif

                half3 finalColor = ambient + directLight + transmission;
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _RimColor;
                half _RampThreshold;
                half _RampSmoothness;
                half _RimPower;
                half _RimIntensity;
                float4 _CentroidOffsetOS;
                half _NormalSoftening;
                half _WindSpeed;
                float4 _WindFrequency;
                half _WindAmplitude;
                float4 _WindDirection;
                half _WindHeightFactor;
                half4 _TransmissionColor;
                half _TransmissionStrength;
                half _TransmissionPower;
                half _Cutoff;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 ApplyFoliageWind(float3 positionOS, float3 positionWS)
            {
                float heightWeight = saturate(positionOS.y * _WindHeightFactor);
                float windPhase = _Time.y * _WindSpeed + dot(positionWS, _WindFrequency.xyz);
                float sway = sin(windPhase) * _WindAmplitude;
                sway += sin(windPhase * 2.3 + positionOS.y * 3.5) * (_WindAmplitude * 0.25);
                return positionWS + normalize(_WindDirection.xyz) * (sway * heightWeight);
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyFoliageWind(input.positionOS.xyz, positionWS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);
                output.positionCS = positionCS;

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _RimColor;
                half _RampThreshold;
                half _RampSmoothness;
                half _RimPower;
                half _RimIntensity;
                float4 _CentroidOffsetOS;
                half _NormalSoftening;
                half _WindSpeed;
                float4 _WindFrequency;
                half _WindAmplitude;
                float4 _WindDirection;
                half _WindHeightFactor;
                half4 _TransmissionColor;
                half _TransmissionStrength;
                half _TransmissionPower;
                half _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 ApplyFoliageWind(float3 positionOS, float3 positionWS)
            {
                float heightWeight = saturate(positionOS.y * _WindHeightFactor);
                float windPhase = _Time.y * _WindSpeed + dot(positionWS, _WindFrequency.xyz);
                float sway = sin(windPhase) * _WindAmplitude;
                sway += sin(windPhase * 2.3 + positionOS.y * 3.5) * (_WindAmplitude * 0.25);
                return positionWS + normalize(_WindDirection.xyz) * (sway * heightWeight);
            }

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyFoliageWind(input.positionOS.xyz, positionWS);
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            half DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _RimColor;
                half _RampThreshold;
                half _RampSmoothness;
                half _RimPower;
                half _RimIntensity;
                float4 _CentroidOffsetOS;
                half _NormalSoftening;
                half _WindSpeed;
                float4 _WindFrequency;
                half _WindAmplitude;
                float4 _WindDirection;
                half _WindHeightFactor;
                half4 _TransmissionColor;
                half _TransmissionStrength;
                half _TransmissionPower;
                half _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                half3 normalWS      : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 ApplyFoliageWind(float3 positionOS, float3 positionWS)
            {
                float heightWeight = saturate(positionOS.y * _WindHeightFactor);
                float windPhase = _Time.y * _WindSpeed + dot(positionWS, _WindFrequency.xyz);
                float sway = sin(windPhase) * _WindAmplitude;
                sway += sin(windPhase * 2.3 + positionOS.y * 3.5) * (_WindAmplitude * 0.25);
                return positionWS + normalize(_WindDirection.xyz) * (sway * heightWeight);
            }

            float3 ApplyCentroidNormalSoftening(float3 positionOS, float3 normalOS)
            {
                float3 dirFromCentroid = positionOS - _CentroidOffsetOS.xyz;
                float distSq = dot(dirFromCentroid, dirFromCentroid);
                float3 sphericalNormalOS = distSq > 0.0001 ? normalize(dirFromCentroid) : float3(0, 1, 0);
                return normalize(lerp(normalOS, sphericalNormalOS, _NormalSoftening));
            }

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyFoliageWind(input.positionOS.xyz, positionWS);
                output.positionCS = TransformWorldToHClip(positionWS);

                float3 softenedNormalOS = ApplyCentroidNormalSoftening(input.positionOS.xyz, input.normalOS);
                output.normalWS = TransformObjectToWorldNormal(softenedNormalOS);

                return output;
            }

            void DepthNormalsFragment(
                Varyings input,
                out half4 outNormalWS : SV_Target0
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                outNormalWS = half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
