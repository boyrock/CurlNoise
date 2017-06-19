Shader "Custom/InstancingUnlitShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_EdgeColor("EdgeColor", Color) = (1,1,1,1)
		_ZScale("ZScale", Range(1,10)) = 1
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
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"
			#include "Common/SimplexNoise2D.cginc"
			#include "Common/Color.cginc"
			#include "Common/Math.cginc"

			StructuredBuffer<float3> _posBuffer;
			StructuredBuffer<float3> _colorBuffer;

			float rand(float n) { return frac(sin(n) * 43758.5453123); }
			float noise(float p)
			{
				float fl = floor(p);
				float fc = frac(p);
				return lerp(rand(fl), rand(fl + 1.0), fc);
			}
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 normal : NORMAL;
				uint instanceID : SV_InstanceID;
			};
			
			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 ambient : TEXCOORD1;
				float3 diffuse : TEXCOORD2;
				float3 color : TEXCOORD3;
				uint instanceID : SV_InstanceID;
				SHADOW_COORDS(4)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _EdgeColor;
			float _ZScale;
			
			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				float3 pos = _posBuffer[instanceID];

				v2f o;
				v.vertex.z *= (noise(instanceID * pos.z) * 5);

				float rotateAngle = noise(instanceID) * 10 + _Time.x * 100;
				v.vertex.xyz = RotateZ(v.vertex.xyz, rotateAngle);
				v.normal = RotateZ(v.normal, rotateAngle);

				float3 localPosition = v.vertex.xyz * 10.2f;// *pos.w;
				float3 worldPosition = pos.xyz + localPosition;
				float3 worldNormal = v.normal;

				float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
				float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
				float3 diffuse = (ndotl * _LightColor0.rgb);
				float3 color = v.color;

				o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.ambient = ambient;
				o.diffuse = diffuse;
				o.color = color;
				o.instanceID = instanceID;
				TRANSFER_SHADOW(o)
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed shadow = SHADOW_ATTENUATION(i);
				//fixed4 albedo = tex2D(_MainTex, i.uv);
				fixed3 col = _colorBuffer[noise(i.instanceID) * 100];
				fixed4 albedo = float4(col, 1);

				float3 lighting = i.diffuse * shadow + i.ambient;
				fixed4 output = fixed4(hsb2rgb(float3(noise(i.instanceID),0.2,1)) * lighting, albedo.w);
				UNITY_APPLY_FOG(i.fogCoord, output);
				return output;

			
			}
			ENDCG
		}
	}
}
