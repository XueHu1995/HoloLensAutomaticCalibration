Shader "Debugging"
{
	Properties
	{
		_Color("Color", Color) = (0.000000,1.000000,1.000000,1.000000)
	}

		SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vertex_shader
			#pragma fragment pixel_shader
			#pragma target 4.5
			float4 _Color;
			float _Number;
			RWStructuredBuffer<float4x4> buffer : register(u1);

			void vertex_shader(inout float4 vertex:POSITION,inout float2 uv : TEXCOORD0)
			{
				vertex = UnityObjectToClipPos(vertex);
			}

			float4 pixel_shader(float4 vertex:POSITION,float2 uv : TEXCOORD0) : SV_TARGET
			{
				/*float4x4 p = float4x4(vertex,0,0,0,0,0,0,0,0,0,0,0,0);
				buffer[_Number] = p;*/
				return _Color;
			}
			ENDCG
		}
	}
}