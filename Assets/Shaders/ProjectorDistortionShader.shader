Shader "Custom/ProjectorDistortion" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	
	// Shader code pasted into all further CGPROGRAM blocks
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};
	
	sampler2D _MainTex;
	
	float4 internalParam; //fx, fy, cx, cy
	float4 distortion; //k1, k2, p1, p2
	float4 resolution; // width, height, 0, 0
	
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord.xy;
		return o;
	} 
	
	half4 frag(v2f i) : SV_Target 
	{
		half2 realCoords = i.uv;
		//正規化座標系に戻す(UnityのUVは左上を原点とした0~1の座標)
		//float pw = 800 - internalParam.w; //主点座標も逆にする
		realCoords.x = (realCoords.x * resolution.x - internalParam.z) / internalParam.x;
		realCoords.y = (realCoords.y * resolution.y - internalParam.w) / internalParam.y;
		//realCoords.y = ((1 - realCoords.y) * 800 - pw) / internalParam.y;
		
		half2 distCoords;
		float r2 = realCoords.x * realCoords.x + realCoords.y * realCoords.y;
		distCoords.x = realCoords.x * (1 - distortion.x * r2 - distortion.y * r2*r2) - 2 * distortion.z * realCoords.x * realCoords.y - distortion.w * (r2 + 2 * realCoords.x * realCoords.x);
		distCoords.y = realCoords.y * (1 - distortion.x * r2 - distortion.y * r2*r2) - distortion.z * (r2 + 2 * realCoords.y * realCoords.y) - 2 * distortion.w * realCoords.x * realCoords.y;
		
		//uvに戻す
		distCoords.x = (distCoords.x * internalParam.x + internalParam.z) / resolution.x;
		distCoords.y = (distCoords.y * internalParam.y + internalParam.w) / resolution.y;
		//distCoords.y = 1 - ((distCoords.y * internalParam.y + pw) / 800);

		half4 color = tex2D (_MainTex, distCoords);	 
		
		return color;
	}

	ENDCG 
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
  
}

Fallback off
	
} // shader
