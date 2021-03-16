// Upgrade NOTE: upgraded instancing buffer 'WaterAmp' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "WaterAmp"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "black" {}
		_Resolution("Resolution", Range( 0 , 1)) = 1
		_Strips("Strips", Range( 0 , 1)) = 1
		_Color("Color", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" }
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf StandardCustom keepalpha exclude_path:deferred vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
		};

		struct SurfaceOutputStandardCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			half3 Transmission;
		};

		uniform sampler2D _MainTex;
		uniform half _Color;
		uniform half _Strips;

		UNITY_INSTANCING_BUFFER_START(WaterAmp)
			UNITY_DEFINE_INSTANCED_PROP(half4, _MainTex_ST)
#define _MainTex_ST_arr WaterAmp
			UNITY_DEFINE_INSTANCED_PROP(half, _Resolution)
#define _Resolution_arr WaterAmp
		UNITY_INSTANCING_BUFFER_END(WaterAmp)


		float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }

		float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }

		float snoise( float3 v )
		{
			const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
			float3 i = floor( v + dot( v, C.yyy ) );
			float3 x0 = v - i + dot( i, C.xxx );
			float3 g = step( x0.yzx, x0.xyz );
			float3 l = 1.0 - g;
			float3 i1 = min( g.xyz, l.zxy );
			float3 i2 = max( g.xyz, l.zxy );
			float3 x1 = x0 - i1 + C.xxx;
			float3 x2 = x0 - i2 + C.yyy;
			float3 x3 = x0 - 0.5;
			i = mod3D289( i);
			float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
			float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
			float4 x_ = floor( j / 7.0 );
			float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
			float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 h = 1.0 - abs( x ) - abs( y );
			float4 b0 = float4( x.xy, y.xy );
			float4 b1 = float4( x.zw, y.zw );
			float4 s0 = floor( b0 ) * 2.0 + 1.0;
			float4 s1 = floor( b1 ) * 2.0 + 1.0;
			float4 sh = -step( h, 0.0 );
			float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
			float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
			float3 g0 = float3( a0.xy, h.x );
			float3 g1 = float3( a0.zw, h.y );
			float3 g2 = float3( a1.xy, h.z );
			float3 g3 = float3( a1.zw, h.w );
			float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
			g0 *= norm.x;
			g1 *= norm.y;
			g2 *= norm.z;
			g3 *= norm.w;
			float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
			m = m* m;
			m = m* m;
			float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
			return 42.0 * dot( m, px);
		}


		float2 voronoihash58( float2 p )
		{
			
			p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
			return frac( sin( p ) *43758.5453);
		}


		float voronoi58( float2 v, float time, inout float2 id, inout float2 mr, float smoothness )
		{
			float2 n = floor( v );
			float2 f = frac( v );
			float F1 = 8.0;
			float F2 = 8.0; float2 mg = 0;
			for ( int j = -1; j <= 1; j++ )
			{
				for ( int i = -1; i <= 1; i++ )
			 	{
			 		float2 g = float2( i, j );
			 		float2 o = voronoihash58( n + g );
					o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
					float d = 0.5 * dot( r, r );
			 		if( d<F1 ) {
			 			F2 = F1;
			 			F1 = d; mg = g; mr = r; id = o;
			 		} else if( d<F2 ) {
			 			F2 = d;
			 		}
			 	}
			}
			return F1;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			half3 appendResult8 = (half3(ase_worldPos.x , ase_worldPos.z , _Time.y));
			half _Resolution_Instance = UNITY_ACCESS_INSTANCED_PROP(_Resolution_arr, _Resolution);
			half3 temp_output_55_0 = ( appendResult8 * _Resolution_Instance );
			half simplePerlin3D6 = snoise( temp_output_55_0*1.0 );
			simplePerlin3D6 = simplePerlin3D6*0.5 + 0.5;
			half simplePerlin3D16 = snoise( temp_output_55_0*3.0 );
			simplePerlin3D16 = simplePerlin3D16*0.5 + 0.5;
			half simplePerlin3D20 = snoise( temp_output_55_0*9.0 );
			simplePerlin3D20 = simplePerlin3D20*0.5 + 0.5;
			half temp_output_24_0 = ( ( simplePerlin3D6 * 0.5 ) + ( simplePerlin3D16 * 0.25 ) + ( simplePerlin3D20 * 0.125 ) );
			half3 appendResult54 = (half3(0.0 , ( temp_output_24_0 * 0.4763031 ) , 0.0));
			v.vertex.xyz += appendResult54;
			v.vertex.w = 1;
		}

		inline half4 LightingStandardCustom(SurfaceOutputStandardCustom s, half3 viewDir, UnityGI gi )
		{
			half3 transmission = max(0 , -dot(s.Normal, gi.light.dir)) * gi.light.color * s.Transmission;
			half4 d = half4(s.Albedo * transmission , 0);

			SurfaceOutputStandard r;
			r.Albedo = s.Albedo;
			r.Normal = s.Normal;
			r.Emission = s.Emission;
			r.Metallic = s.Metallic;
			r.Smoothness = s.Smoothness;
			r.Occlusion = s.Occlusion;
			r.Alpha = s.Alpha;
			return LightingStandard (r, viewDir, gi) + d;
		}

		inline void LightingStandardCustom_GI(SurfaceOutputStandardCustom s, UnityGIInput data, inout UnityGI gi )
		{
			#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
				gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
			#else
				UNITY_GLOSSY_ENV_FROM_SURFACE( g, s, data );
				gi = UnityGlobalIllumination( data, s.Occlusion, s.Normal, g );
			#endif
		}

		void surf( Input i , inout SurfaceOutputStandardCustom o )
		{
			half4 _MainTex_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(_MainTex_ST_arr, _MainTex_ST);
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST_Instance.xy + _MainTex_ST_Instance.zw;
			float3 ase_worldPos = i.worldPos;
			half3 appendResult8 = (half3(ase_worldPos.x , ase_worldPos.z , _Time.y));
			half _Resolution_Instance = UNITY_ACCESS_INSTANCED_PROP(_Resolution_arr, _Resolution);
			half3 temp_output_55_0 = ( appendResult8 * _Resolution_Instance );
			half simplePerlin3D6 = snoise( temp_output_55_0*1.0 );
			simplePerlin3D6 = simplePerlin3D6*0.5 + 0.5;
			half simplePerlin3D16 = snoise( temp_output_55_0*3.0 );
			simplePerlin3D16 = simplePerlin3D16*0.5 + 0.5;
			half simplePerlin3D20 = snoise( temp_output_55_0*9.0 );
			simplePerlin3D20 = simplePerlin3D20*0.5 + 0.5;
			half temp_output_24_0 = ( ( simplePerlin3D6 * 0.5 ) + ( simplePerlin3D16 * 0.25 ) + ( simplePerlin3D20 * 0.125 ) );
			half4 appendResult29 = (half4(temp_output_24_0 , temp_output_24_0 , temp_output_24_0 , temp_output_24_0));
			half4 temp_cast_0 = (6.0).xxxx;
			half time58 = _Time.y;
			float2 coords58 = appendResult8.xy * 0.26;
			float2 id58 = 0;
			float2 uv58 = 0;
			float voroi58 = voronoi58( coords58, time58, id58, uv58, 0 );
			half4 temp_output_47_0 = ( ( tex2D( _MainTex, uv_MainTex ) * _Color ) + pow( appendResult29 , temp_cast_0 ) + ( pow( voroi58 , 2.0 ) * _Strips ) );
			o.Albedo = temp_output_47_0.rgb;
			o.Metallic = temp_output_47_0.r;
			half temp_output_64_0 = ( 1.0 - temp_output_24_0 );
			o.Smoothness = temp_output_64_0;
			o.Occlusion = temp_output_47_0.r;
			o.Transmission = temp_output_47_0.rgb;
			o.Alpha = temp_output_64_0;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18800
0;0;1366;707;509.0134;665.4077;2.054029;True;True
Node;AmplifyShaderEditor.SimpleTimeNode;9;-1072.191,310.831;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;7;-1075.89,169.1806;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;8;-904.3143,193.586;Inherit;True;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;56;-951.1007,429.5579;Inherit;False;InstancedProperty;_Resolution;Resolution;2;0;Create;True;0;0;0;False;0;False;1;0.31;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-423.265,283.2961;Inherit;False;Constant;_Float2;Float 2;0;0;Create;True;0;0;0;False;0;False;3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-423.5484,67.93009;Inherit;False;Constant;_Float0;Float 0;0;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;-662.1007,240.5579;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-425.2963,514.8066;Inherit;False;Constant;_Float4;Float 4;0;0;Create;True;0;0;0;False;0;False;9;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;16;-386.75,183.989;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-297.6086,69.09519;Inherit;False;Constant;_Float1;Float 1;0;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-299.3566,515.9717;Inherit;False;Constant;_Float5;Float 5;0;0;Create;True;0;0;0;False;0;False;0.125;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-297.3253,284.4612;Inherit;False;Constant;_Float3;Float 3;0;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;6;-387.0333,-31.37704;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;20;-388.7813,415.4995;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-216.3419,180.0159;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-218.3732,419.7572;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-220.7407,-24.37574;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;24;-21.15338,181.5704;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode;58;-555.4215,-352.7454;Inherit;True;0;0;1;0;1;False;1;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0.26;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.PowerNode;65;-413.7392,-316.3134;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;27;447.7759,-120.2063;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;0;False;0;False;-1;None;d2c21b3117b24b749844675d09a72ace;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;49;588.7186,742.459;Inherit;False;Constant;_Float6;Float 6;2;0;Create;True;0;0;0;False;0;False;6;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;83.58746,498.7656;Inherit;False;Constant;_Float8;Float 8;2;0;Create;True;0;0;0;False;0;False;0.4763031;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;29;489.1555,499.6525;Inherit;True;COLOR;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;67;597.6055,-263.4846;Inherit;False;Property;_Color;Color;4;0;Create;True;0;0;0;False;0;False;0;0.08940139;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;62;-378.2757,-167.9241;Inherit;False;Property;_Strips;Strips;3;0;Create;True;0;0;0;False;0;False;1;0.27;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;48;765.9423,500.2473;Inherit;True;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;350.5875,360.7656;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;61;-222.7299,-296.9097;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;69;849.8538,-2.53833;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;64;300.6684,124.306;Inherit;True;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;47;1020.591,122.3285;Inherit;True;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;54;607.2189,339.8492;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;40;1313.93,31.46091;Half;False;True;-1;2;ASEMaterialInspector;0;0;Standard;WaterAmp;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;True;Transparent;;Geometry;ForwardOnly;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;1;False;-1;1;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;8;0;7;1
WireConnection;8;1;7;3
WireConnection;8;2;9;0
WireConnection;55;0;8;0
WireConnection;55;1;56;0
WireConnection;16;0;55;0
WireConnection;16;1;18;0
WireConnection;6;0;55;0
WireConnection;6;1;13;0
WireConnection;20;0;55;0
WireConnection;20;1;22;0
WireConnection;17;0;16;0
WireConnection;17;1;19;0
WireConnection;21;0;20;0
WireConnection;21;1;23;0
WireConnection;14;0;6;0
WireConnection;14;1;15;0
WireConnection;24;0;14;0
WireConnection;24;1;17;0
WireConnection;24;2;21;0
WireConnection;58;0;8;0
WireConnection;58;1;9;0
WireConnection;65;0;58;0
WireConnection;29;0;24;0
WireConnection;29;1;24;0
WireConnection;29;2;24;0
WireConnection;29;3;24;0
WireConnection;48;0;29;0
WireConnection;48;1;49;0
WireConnection;52;0;24;0
WireConnection;52;1;53;0
WireConnection;61;0;65;0
WireConnection;61;1;62;0
WireConnection;69;0;27;0
WireConnection;69;1;67;0
WireConnection;64;1;24;0
WireConnection;47;0;69;0
WireConnection;47;1;48;0
WireConnection;47;2;61;0
WireConnection;54;1;52;0
WireConnection;40;0;47;0
WireConnection;40;3;47;0
WireConnection;40;4;64;0
WireConnection;40;5;47;0
WireConnection;40;6;47;0
WireConnection;40;9;64;0
WireConnection;40;11;54;0
ASEEND*/
//CHKSM=C1F7EE486B71A8868D320F17E304974B4CADC90A