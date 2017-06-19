Shader "Custom/InstancedSurfaceShader" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Color01("Color01", color) = (1,1,1,1)
		_Color02("Color02", color) = (1,1,1,1)
		_Color03("Color03", color) = (1,1,1,1)
		_Color04("Color04", color) = (1,1,1,1)
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model
		#pragma surface surf Standard addshadow fullforwardshadows
		#pragma instancing_options procedural:setup
		#include "Common/SimplexNoise3D.cginc"
		#include "Common/Math.cginc"

		sampler2D _MainTex;
		float4 _Color01;
		float4 _Color02;
		float4 _Color03;
		float4 _Color04;
		float _RotateAngle;

		float4x4 _LocalToWorld;
		float4x4 _WorldToLocal;

		float rand(float n) { return frac(sin(n) * 43758.5453123); }
		float noise(float p)
		{
			float fl = floor(p);
			float fc = frac(p);
			return lerp(rand(fl), rand(fl + 1.0), fc);
		}

		struct Input {
			float2 uv_MainTex;
		};

		struct appdata
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			uint vid : SV_VertexID;
			float3 color : TEXCOORD3;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			StructuredBuffer<float4> _posBuffer;
			StructuredBuffer<float3> _colorBuffer;
		#endif


		void rotate2D(inout float2 v, float r)
		{
			float s, c;
			sincos(r, s, c);
			v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
		}

//		void vert(inout appdata v)
//		{
//#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
//			v.vertex.xyz = float3(1, 1, 1);// _posBuffer[unity_InstanceID].xyz;
//#endif
//		}

		//void setup()
		//{
		//	unity_ObjectToWorld = _LocalToWorld;
		//	unity_WorldToObject = _WorldToLocal;
		//}

		void setup()
		{
		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			float4 data = _posBuffer[unity_InstanceID];
			data.xyz = RotateX(data.xyz, _RotateAngle);
			unity_ObjectToWorld._11_21_31_41 = float4(data.w, 0, 0, 0);
			unity_ObjectToWorld._12_22_32_42 = float4(0, data.w, 0, 0);
			unity_ObjectToWorld._13_23_33_43 = float4(0, 0, data.w, 0);
			unity_ObjectToWorld._14_24_34_44 = float4(data.xyz, 1);

			unity_WorldToObject = unity_ObjectToWorld;
			unity_WorldToObject._14_24_34 *= -1;
			unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
		#endif
		}

		half _Glossiness;
		half _Metallic;

		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			o.Albedo = _colorBuffer[noise(unity_InstanceID) * 100];// lerp(_BottomColor, _TopColor, noise(unity_InstanceID));// _Color;
		#endif
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
	ENDCG
	}
	FallBack "Diffuse"
}