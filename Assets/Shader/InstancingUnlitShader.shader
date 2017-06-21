// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/InstancingUnlitShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_EdgeColor("EdgeColor", Color) = (1,1,1,1)
		_Scale("Scale", Range(0,10)) = 1
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		//Cull off
		Blend One OneMinusSrcColor
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
			#include "Common/Quaternion.cginc"
			#include "Particle.cginc"

			StructuredBuffer<GPUParticle> _particleBuffer;
			StructuredBuffer<ParticleGlobal> _globalBuffer;
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
			float _Scale;
			
			v2f vert (appdata_full v, uint instanceID : SV_InstanceID, uint vid : SV_VertexID)
			{
				float3 pos = _particleBuffer[instanceID].pos;
				float lifeTime = _particleBuffer[instanceID].lifeTime;

				v2f o;
				//v.vertex.x *= 3;
				//v.vertex.xy = 1 * 2.0 - float2(1.0, 1.0);
				//v.vertex.zw = float2(0.0, 1.0);

				float rotateAngle = noise(instanceID) * 10 + _Time.x * 100;
				if (lifeTime <= 0)
				{
					v.vertex.xyz = 0;
				}
				else 
				{
					v.vertex.xyz = Rotate(v.vertex.xyz, rotateAngle);
					//v.vertex.xyz = GetRandomRotation(v.texcoord).xyz;
	/*				v.vertex.xyz = Rotate(v.vertex.xyz, rotateAngle);
					v.vertex.xyz = RotateY(v.vertex.xyz, rotateAngle * 0.5f);
					v.vertex.xyz = RotateZ(v.vertex.xyz, rotateAngle * 0.25f);*/
				}

				v.normal = RotateZ(v.normal, rotateAngle);

				float3 localPosition = v.vertex.xyz * (0.01 + noise(instanceID * 2) * _Scale);// *pos.w;
				float3 worldPosition = pos.xyz + localPosition;
				float3 worldNormal = v.normal;

				float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
				float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
				float3 diffuse = (ndotl * _LightColor0.rgb);
				float3 color = v.color;

				o.pos = UnityObjectToClipPos(float4(worldPosition, 1.0f));
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.ambient = ambient;
				o.diffuse = diffuse;

				float f = vid;
				//o.color = hsb2rgb(float3(noise(instanceID), 0.2, 1));
				o.color = _colorBuffer[noise(instanceID) * 100] * 1;
				o.instanceID = instanceID;
				TRANSFER_SHADOW(o)
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float lifeTime = _particleBuffer[i.instanceID].lifeTime;

				fixed shadow = SHADOW_ATTENUATION(i);
				fixed3 col = i.color;
				fixed4 albedo = float4(col, 1);
				//fixed4 albedo = tex2D(_MainTex, i.uv);

				float3 lighting = i.diffuse * shadow + i.ambient;
				fixed4 output = fixed4(albedo.rgb * lighting, clamp(lifeTime,0,1));
				//UNITY_APPLY_FOG(i.fogCoord, output);
				return output;
			}

			ENDCG
		}
	}
}
