// based on http://danjohnmoran.com/Shaders/102/AnimatedDisplacement.shader

Shader "Image Effects/UnderwaterShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_DisplaceTex("Displacement Texture", 2D) = "white" {}
		_Magnitude("Magnitude", Range(0,0.1)) = 1
		_AnimSpeed("Wiggle Speed", Range(0,1)) = 1
		_Color("Color", Color) = (1,1,1,1)
	}
		SubShader
		{
			// No culling or depth
			Cull Off ZWrite Off ZTest Always

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					return o;
				}

				sampler2D _MainTex;
				sampler2D _DisplaceTex;
				float _Magnitude;
				float _AnimSpeed;
				fixed4 _Color;

				float4 frag(v2f i) : SV_Target
				{
					float2 animUV = float2(i.uv.x + _Time.x * _AnimSpeed, i.uv.y + _Time.x * _AnimSpeed);
					float2 disp = tex2D(_DisplaceTex, animUV).xy;
					disp = ((disp * 2) - 1) * _Magnitude;

					float4 col = tex2D(_MainTex, i.uv + disp) * _Color;
					return col;
				}
				ENDCG
			}
		}
}
