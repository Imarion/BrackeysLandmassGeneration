﻿Shader "Custom/Terrain"
{
	Properties
	{
		testTexture("Texture", 2D) = "white" {}
		testScale("Scale", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxLayerCount = 8;
		const static float epsilon = 1E-4;

		float minHeight;
		float maxHeight;

		int layerCount;
		float3 baseColours[maxLayerCount];
		float baseStartHeights[maxLayerCount];
		float baseBlends[maxLayerCount];
		float baseColourStrengths[maxLayerCount];
		float baseTextureScales[maxLayerCount];

		sampler2D testTexture;
		float testScale;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input
        {
			float3 worldPos;
			float3 worldNormal;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

		float inverseLerp(float a, float b, float value) {
			return saturate((value - a) / (b - a)); // saturate clamps the value between 0 & 1
		}

		float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPosition = worldPos / scale;

			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPosition.y, scaledWorldPosition.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPosition.x, scaledWorldPosition.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPosition.x, scaledWorldPosition.y, textureIndex)) * blendAxes.z;

			return xProjection + yProjection + zProjection;

		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z; // ensure sum is not > 1

			for (int i = 0; i < layerCount; i++)
			{
				float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i] / 2, heightPercent - baseStartHeights[i]); // -epsilon to avoid /0 in InverseLerp

				float3 baseColour = baseColours[i] * baseColourStrengths[i];
				float3 textureColour = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColourStrengths[i]);

				o.Albedo = o.Albedo * (1 - drawStrength) + (baseColour + textureColour) * drawStrength; // keep current albedo if drawStrength = 0
				//o.Albedo = float3(1, 1, 0);
			}
			
			//o.Albedo = float3(1, 1, 0);

			
        }
        ENDCG
    }
    FallBack "Diffuse"
}
