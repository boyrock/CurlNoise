Shader "Hidden/InstancingShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_EdgeColor("EdgeColor", Color) = (1,1,1,1)
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		Cull off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#include "Common/SimplexNoise2D.cginc"
			#include "Common/Color.cginc"

			StructuredBuffer<float3> _posBuffer;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 normal : NORMAL;
				uint instanceID : SV_InstanceID;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 pos : TEXCOORD1;
				uint instanceID : SV_InstanceID;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _EdgeColor;
			
			v2f vert (appdata v)
			{
				float3 pos = _posBuffer[v.instanceID];

				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex * 0.3 + pos);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.pos = pos;// UnityObjectToWorldNormal(v.normal);
				o.instanceID = v.instanceID;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 color = hsb2rgb(float3(snoise(i.pos.xy * 0.0009), snoise(i.pos.xy * 0.0007),0.6 ));
				return float4(color, 1);
			}
			ENDCG
		}
	}
}
