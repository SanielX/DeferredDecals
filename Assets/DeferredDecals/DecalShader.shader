Shader "Custom/GDecalShader"
{
	Properties
	{
		_NormalTolerance("Normal Tolerance", Range(-1,1)) = 0.3
		_AlphaClip("Alpha Clip", Range(0,1)) = 0.001

		_Color("Color", Color) = (1,1,1,1)
		[NoScaleOffset]
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset]
		_BumpMap("Normal map", 2D) = "bump"{}
		_NormalScale("Normal Scale", float) = 1
		_NormalPow("Normal Influence", Range(0,1)) = 1

		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0
		_GlossinessPow("Specular Influence", Range(0,1)) = 1

		//	_EmissionTex("Emission Texture", 2D) = "black"{}

		//		[HDR]
		//		_EmissionCol("Emission Color", color) = (0,0,0,0)
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "LightMode" = "Deferred" "Queue"="Geometry+100"}
		Cull off
		ZWrite off
		ZTest Always
		LOD 200

		Pass
		{
			Fog { Mode Off } // no fog in g-buffers pass
			ZWrite Off
			Cull off
			ZTest Always

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers nomrt

			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"

			struct v2f
			{
				float4 pos : SV_POSITION;
				half2 uv : TEXCOORD0;
				float4 screenUV : TEXCOORD1;
				float3 ray : TEXCOORD2;
				half3 orientation : TEXCOORD3;
				half3 orientationX : TEXCOORD4;
				half3 orientationZ : TEXCOORD5;
			};

			v2f vert(float3 v : POSITION)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(float4(v,1));
				o.uv = v.xz + 0.5;
				o.screenUV = ComputeScreenPos(o.pos);
				o.ray = mul(UNITY_MATRIX_MV, float4(v,1)).xyz * float3(-1,-1,1);
				o.orientation = normalize(mul((float3x3)unity_ObjectToWorld, float3(0,1,0)));
				o.orientationX =  normalize(mul((float3x3)unity_ObjectToWorld, float3(1,0,0)));
				o.orientationZ =  normalize(mul((float3x3)unity_ObjectToWorld, float3(0,0,1)));
				return o;
			}

			half _NormalTolerance;
			float4 _Color;
			half _Glossiness;
			half _Metallic;
			float _GlossinessPow, _NormalPow;

			sampler2D _MainTex;
			sampler2D _BumpMap;
			
			sampler2D_float _CameraDepthTexture;
			sampler2D _NormalsCopy;
			sampler2D _DiffuseCopy;
			sampler2D _SpecCopy;
			float4 _TileProperties;
			float _NormalScale;
			float _AlphaClip;

			void frag(v2f i, out half4 outDiffuse : COLOR0, out half4 outNormal : COLOR2, 
			out half4 outSpec : COLOR1/*, out half4 outEmission : COLOR3*/)
			{
				i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
				float2 uv = i.screenUV.xy / i.screenUV.w;

				// read depth and reconstruct world position
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
				depth = Linear01Depth(depth);
				float4 vpos = float4(i.ray * depth,1);
				float3 wpos = mul(unity_CameraToWorld, vpos).xyz;
				float3 opos = mul(unity_WorldToObject, float4(wpos,1)).xyz;

				clip(float3(0.5,0.5,0.5) - abs(opos.xyz));   // First clip when out of box bounds

				i.uv = opos.xz + 0.5;

				half4 originalNormal = tex2D(_NormalsCopy, uv);
				fixed3 wnormal = originalNormal.rgb * 2.0 - 1.0;
				wnormal = normalize(wnormal);

				clip(dot(wnormal, i.orientation) - _NormalTolerance);

				float4 albedo = tex2D(_MainTex, i.uv);
				float alpha = albedo.a;

				clip(alpha - _AlphaClip);   // Just alpha clip
				albedo *= _Color;
				alpha *= _Color.a;

				float3 specularTint;
				float oneMinusReflectivity;

				albedo.rgb = DiffuseAndSpecularFromMetallic(albedo.rgb, _Metallic, /*out*/ specularTint, /*out*/ oneMinusReflectivity);
				albedo.rgb *= oneMinusReflectivity; 
				
				float4 specular = tex2D(_SpecCopy, uv);
				specular.rgb = lerp(specular, specularTint, alpha * _GlossinessPow);
				specular.a = lerp(specular.a, _Glossiness, alpha * _GlossinessPow);

				float4 originalDiffuse = tex2D(_DiffuseCopy, uv);
				outDiffuse.rgb = lerp(originalDiffuse.rgb, albedo.rgb, saturate(alpha));
				outDiffuse.a = originalDiffuse.a;

				half scale = _NormalScale * alpha;
				fixed3 nor = UnpackScaleNormal(tex2D(_BumpMap, i.uv), scale);
				half3x3 norMat = half3x3(i.orientationX, i.orientationZ, i.orientation);
				nor.rgb = mul(nor, norMat);
				nor.rgb = BlendNormals(nor.rgb, 0); // This doesn't allow to completely overwrite originalNormal
				nor = fixed4(nor.rgb * 0.5 + 0.5, 1);

				nor.rgb = lerp(originalNormal.rgb, nor.rgb, alpha * _NormalPow);

				outNormal = fixed4(nor, originalNormal.a);
				outSpec = specular;

				/*		float4 originalEmission = tex2D(_EmissionCopy, uv);
				float4 emission = tex2D(_EmissionTex, i.uv) + _EmissionCol;
				#if UNITY_HDR_ON
					emission.rgb = exp2(-emission.rgb);
				#endif
				
				outEmission = originalEmission + (emission * alpha); 
				outEmission.a = originalEmission.a;
				*/
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
