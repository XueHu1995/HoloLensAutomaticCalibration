
Shader "Custom/TestShader"
{
	Properties
	{
		_Color("Color", Color) = (0.0, 1.0, 1.0, 1.0)
	}

		SubShader
	{

		Pass
	{

		Cull Back

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"

		sampler2D _MainTex;
		// float4x4 MATRIX_MVP;
		float4 _Color;


		struct appdata
		{
			float4 vertex : POSITION;
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
		};

		v2f vert(appdata v)
		{
			v2f o;

			o.pos = UnityObjectToClipPos(v.vertex);
			// o.pos.y = -o.pos.y;

			return o;
		}


		float4 frag(v2f i) : SV_Target
		{

			return _Color;
		}

		ENDCG

		}
	}
}