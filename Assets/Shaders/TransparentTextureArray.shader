Shader "Custom/Transparent Texture Array"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2DArray) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_ZOffset("Z Buffer Offset", Float) = 0
	}
		SubShader
	{

		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		ZWrite Off
		ZTest LEqual
		LOD 200
		Offset[_ZOffset],[_ZOffset]

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types. Sets vertex function to vert. Allow alpha.
		#pragma surface surf Standard fullforwardshadows vertex:vert alpha:blend

		// Use shader model 3.5 target, texture array support
		#pragma target 3.5
		#pragma require 2darray

		UNITY_DECLARE_TEX2DARRAY(_MainTex);

		struct Input
		{
			float2 uv_MainTex;
			float arrayIndex; // cannot start with “uv”
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void vert(inout appdata_full v, out Input o)
		{
			o.uv_MainTex = v.texcoord.xy;
			o.arrayIndex = v.texcoord.z;
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.uv_MainTex, IN.arrayIndex)) * _Color;
			o.Albedo = c.rgb;

			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}

	FallBack "Diffuse"
}