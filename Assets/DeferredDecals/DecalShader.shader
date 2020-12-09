Shader "Custom/GDecalShader"
{
	Properties
	{
		_NormalTolerance("Normal Tolerance", Range(-1,1)) = 0.3
		_AlphaClip("Alpha Clip", Range(0,1)) = 0.001

		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Normal map", 2D) = "bump"{}
		_NormalScale("Normal Scale", float) = 1
		_NormalPow("Normal Influence", Range(0,1)) = 1

		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_GlossinessPow("Smoothness Influence", Range(0,1)) = 1

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
			//	Blend SrcAlpha OneMinusSrcAlpha

			//Blend SrcAlpha OneMinusSrcAlpha

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
		//	float4 _EmissionCol;
			float _Glossiness;
			float _GlossinessPow, _NormalPow;

			sampler2D _MainTex;
			sampler2D _BumpMap;
		//	sampler2D _EmissionTex;
			
			sampler2D_float _CameraDepthTexture;
			sampler2D _NormalsCopy;
			sampler2D _DiffuseCopy;
			sampler2D _SpecCopy;
			float4 _TileProperties;
			float _NormalScale;
			float _AlphaClip;

			void frag(v2f i, out half4 diffuse : COLOR0, out half4 outNormal : COLOR2, 
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

				clip(float3(0.5,0.5,0.5) - abs(opos.xyz));

				i.uv = opos.xz + 0.5;

				half3 normal = tex2D(_NormalsCopy, uv).rgb;
				fixed3 wnormal = normal.rgb * 2.0 - 1.0;
				wnormal = normalize(wnormal);

				clip(dot(wnormal, i.orientation) - _NormalTolerance);

				float4 col = tex2D(_MainTex, i.uv) * _Color;
				float alpha = col.a;

				clip(alpha - _AlphaClip);

				col.a = 1;

				float4 diff = tex2D(_DiffuseCopy, uv);
				diffuse = 0;
				diffuse.rgb = lerp(diff.rgb, col.rgb, saturate(alpha));
				diffuse.a = diff.a;

				half scale = _NormalScale * alpha;
				fixed4 nor = fixed4(UnpackScaleNormal(tex2D(_BumpMap, i.uv), scale), 1);
				half3x3 norMat = half3x3(i.orientationX, i.orientationZ, i.orientation);
				nor.rgb = mul(nor, norMat);
				nor.rgb = BlendNormals(nor.rgb, normal);
				nor = fixed4(nor.rgb * 0.5 + 0.5, 1);

				nor.rgb = lerp(normal.rgb, nor.rgb, alpha * _NormalPow);
			//	nor.rgb = normalize(nor.rgb);
			//	nor.rgb = saturate(nor.rgb);
				nor.a = 1;
				outNormal = nor;

				float4 originalSmoothness = tex2D(_SpecCopy, uv);
				originalSmoothness.a = lerp(originalSmoothness.a, _Glossiness, alpha * _GlossinessPow);

				outSpec = originalSmoothness;

		/*		float4 originalEmission = tex2D(_EmissionCopy, uv);
				float4 emission = tex2D(_EmissionTex, i.uv) + _EmissionCol;
				#if UNITY_HDR_ON
					emission.rgb = exp2(-emission.rgb);
				#endif
				
				outEmission = lerp(originalEmission, emission, alpha);
				outEmission.a = originalEmission.a;
*/
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
