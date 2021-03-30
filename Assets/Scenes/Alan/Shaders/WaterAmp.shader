// Upgrade NOTE: upgraded instancing buffer 'WaterAmp' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "WaterAmp"
{
	Properties
	{
		_Resolution("Resolution", Range( 0 , 1)) = 1
		_Strips("Strips", Range( 0 , 1)) = 1
		_WaveHeight("WaveHeight", Range( -10 , 10)) = 1
		_Depth("Depth", Range( 0 , 10)) = 0
		_DepthOffset("Depth Offset", Range( -10 , 10)) = 0
		_Speed("Speed", Range( 0 , 5)) = 0
		_Color("Color", Range( 0 , 1)) = 0
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard alpha:fade keepalpha noshadow vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float4 screenPos;
		};

		uniform half _WaveHeight;
		uniform half _Speed;
		uniform half _Color;
		uniform half _Strips;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform half _DepthOffset;
		uniform half _Depth;

		UNITY_INSTANCING_BUFFER_START(WaterAmp)
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
			half _Resolution_Instance = UNITY_ACCESS_INSTANCED_PROP(_Resolution_arr, _Resolution);
			half3 break116 = ( ase_worldPos * _Resolution_Instance );
			half mulTime9 = _Time.y * _Speed;
			half3 appendResult115 = (half3(break116.x , break116.z , mulTime9));
			half simplePerlin3D6 = snoise( appendResult115*1.0 );
			simplePerlin3D6 = simplePerlin3D6*0.5 + 0.5;
			half simplePerlin3D16 = snoise( appendResult115*2.0 );
			simplePerlin3D16 = simplePerlin3D16*0.5 + 0.5;
			half simplePerlin3D20 = snoise( appendResult115*4.0 );
			simplePerlin3D20 = simplePerlin3D20*0.5 + 0.5;
			half temp_output_24_0 = ( ( simplePerlin3D6 * 0.5 ) + ( simplePerlin3D16 * 0.25 ) + ( simplePerlin3D20 * 0.125 ) );
			half3 appendResult54 = (half3(0.0 , ( _WaveHeight * temp_output_24_0 ) , 0.0));
			v.vertex.xyz += appendResult54;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = half3(0,0,1);
			half3 appendResult113 = (half3(_Color , _Color , _Color));
			half mulTime9 = _Time.y * _Speed;
			half time58 = mulTime9;
			float3 ase_worldPos = i.worldPos;
			half _Resolution_Instance = UNITY_ACCESS_INSTANCED_PROP(_Resolution_arr, _Resolution);
			half3 break116 = ( ase_worldPos * _Resolution_Instance );
			half2 appendResult117 = (half2(break116.x , break116.z));
			float2 coords58 = appendResult117 * 1.0;
			float2 id58 = 0;
			float2 uv58 = 0;
			float voroi58 = voronoi58( coords58, time58, id58, uv58, 0 );
			half temp_output_61_0 = ( pow( voroi58 , 2.0 ) * _Strips );
			half3 appendResult115 = (half3(break116.x , break116.z , mulTime9));
			half simplePerlin3D6 = snoise( appendResult115*1.0 );
			simplePerlin3D6 = simplePerlin3D6*0.5 + 0.5;
			half simplePerlin3D16 = snoise( appendResult115*2.0 );
			simplePerlin3D16 = simplePerlin3D16*0.5 + 0.5;
			half simplePerlin3D20 = snoise( appendResult115*4.0 );
			simplePerlin3D20 = simplePerlin3D20*0.5 + 0.5;
			half temp_output_24_0 = ( ( simplePerlin3D6 * 0.5 ) + ( simplePerlin3D16 * 0.25 ) + ( simplePerlin3D20 * 0.125 ) );
			half3 appendResult29 = (half3(temp_output_24_0 , temp_output_24_0 , temp_output_24_0));
			half3 temp_cast_0 = (6.0).xxx;
			o.Albedo = ( appendResult113 + temp_output_61_0 + pow( appendResult29 , temp_cast_0 ) );
			o.Metallic = temp_output_61_0;
			o.Smoothness = temp_output_61_0;
			o.Occlusion = temp_output_61_0;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			half eyeDepth88 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			half clampResult91 = clamp( ( ( eyeDepth88 + exp( _DepthOffset ) ) / exp( _Depth ) ) , 0.0 , 1.0 );
			o.Alpha = clampResult91;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18800
83;365;1366;525;-341.6551;215.1216;1.087875;True;True
Node;AmplifyShaderEditor.WorldPosInputsNode;7;-1003.242,-38.71817;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;56;-1279.724,95.26422;Inherit;False;InstancedProperty;_Resolution;Resolution;0;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;-828.6112,10.60978;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;101;-1281.374,-36.73155;Inherit;False;Property;_Speed;Speed;5;0;Create;True;0;0;0;False;0;False;0;0.2;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;116;-702.6436,13.37416;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleTimeNode;9;-1000.543,100.9322;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;115;-575.7228,12.78136;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-413.3608,69.17387;Inherit;False;Constant;_Float0;Float 0;0;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-425.2963,514.8066;Inherit;False;Constant;_Float4;Float 4;0;0;Create;True;0;0;0;False;0;False;4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-423.265,283.2961;Inherit;False;Constant;_Float2;Float 2;0;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;16;-385.2498,185.4893;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-299.3566,515.9717;Inherit;False;Constant;_Float5;Float 5;0;0;Create;True;0;0;0;False;0;False;0.125;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-297.3253,284.4612;Inherit;False;Constant;_Float3;Float 3;0;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;6;-390.0339,-25.37585;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-297.6086,69.09519;Inherit;False;Constant;_Float1;Float 1;0;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;20;-388.7813,415.4995;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-220.7407,-24.37574;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-213.3413,187.5173;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-218.3732,419.7572;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;90;183.5727,417.0447;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;94;407.5766,551.4713;Inherit;False;Property;_DepthOffset;Depth Offset;4;0;Create;True;0;0;0;False;0;False;0;2.12;-10;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;117;-570.0923,-79.11121;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScreenDepthNode;88;573.588,418.0446;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;83;409.3665,618.9095;Inherit;False;Property;_Depth;Depth;3;0;Create;True;0;0;0;False;0;False;0;3.5;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;24;8.841512,156.3596;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode;58;-387.693,-343.1846;Inherit;True;0;0;1;0;1;False;1;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.ExpOpNode;95;661.7609,552.6533;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;93;777.9997,481.6205;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ExpOpNode;87;662.8588,620.1296;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;62;-227.4843,-121.7874;Inherit;False;Property;_Strips;Strips;1;0;Create;True;0;0;0;False;0;False;1;0.83;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;49;627.8321,-159.5972;Inherit;False;Constant;_Float6;Float 6;2;0;Create;True;0;0;0;False;0;False;6;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;112;789.8644,-185.5643;Inherit;False;Property;_Color;Color;6;0;Create;True;0;0;0;False;0;False;0;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;64.08746,85.3656;Inherit;False;Property;_WaveHeight;WaveHeight;2;0;Create;True;0;0;0;False;0;False;1;1;-10;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;65;-189.1548,-342.4529;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;29;587.5814,-81.22301;Inherit;True;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;92;883.0057,483.3687;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;317.2874,85.56552;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;113;934.2275,-202.5669;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;61;46.89474,-343.5396;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;48;793.3723,-80.62812;Inherit;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;54;320.1064,175.3605;Inherit;True;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;110;1052.13,-200.9173;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;108;1024.635,-76.80886;Inherit;False;Constant;_Vector1;Vector 1;7;0;Create;True;0;0;0;False;0;False;0,0,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ClampOpNode;91;990.6708,482.1838;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;40;1222.525,-80.39318;Half;False;True;-1;2;ASEMaterialInspector;0;0;Standard;WaterAmp;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;1;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;1;False;-1;1;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;2;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;55;0;7;0
WireConnection;55;1;56;0
WireConnection;116;0;55;0
WireConnection;9;0;101;0
WireConnection;115;0;116;0
WireConnection;115;1;116;2
WireConnection;115;2;9;0
WireConnection;16;0;115;0
WireConnection;16;1;18;0
WireConnection;6;0;115;0
WireConnection;6;1;13;0
WireConnection;20;0;115;0
WireConnection;20;1;22;0
WireConnection;14;0;6;0
WireConnection;14;1;15;0
WireConnection;17;0;16;0
WireConnection;17;1;19;0
WireConnection;21;0;20;0
WireConnection;21;1;23;0
WireConnection;117;0;116;0
WireConnection;117;1;116;2
WireConnection;88;0;90;0
WireConnection;24;0;14;0
WireConnection;24;1;17;0
WireConnection;24;2;21;0
WireConnection;58;0;117;0
WireConnection;58;1;9;0
WireConnection;95;0;94;0
WireConnection;93;0;88;0
WireConnection;93;1;95;0
WireConnection;87;0;83;0
WireConnection;65;0;58;0
WireConnection;29;0;24;0
WireConnection;29;1;24;0
WireConnection;29;2;24;0
WireConnection;92;0;93;0
WireConnection;92;1;87;0
WireConnection;52;0;53;0
WireConnection;52;1;24;0
WireConnection;113;0;112;0
WireConnection;113;1;112;0
WireConnection;113;2;112;0
WireConnection;61;0;65;0
WireConnection;61;1;62;0
WireConnection;48;0;29;0
WireConnection;48;1;49;0
WireConnection;54;1;52;0
WireConnection;110;0;113;0
WireConnection;110;1;61;0
WireConnection;110;2;48;0
WireConnection;91;0;92;0
WireConnection;40;0;110;0
WireConnection;40;1;108;0
WireConnection;40;3;61;0
WireConnection;40;4;61;0
WireConnection;40;5;61;0
WireConnection;40;9;91;0
WireConnection;40;11;54;0
ASEEND*/
//CHKSM=8DCE044468587C64E68E3FEF8BE397FFC4375923