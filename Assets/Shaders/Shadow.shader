Shader "Custom/RenderShadow" {
    Properties {
        _MainTex ("Depth Texture", 2D) = "white" {}
        _Threshold ("Threshold", Range(0, 1)) = 0.5
        _EdgeColor ("Back Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0)
        _Thick("Thick", Range(0.1, 5)) = 1
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        ZTest Off
        ZWrite Off
        Lighting Off
        AlphaTest Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4  _MainTex_ST;
            float _Threshold;
            float4 _EdgeColor;
            float4 _ShadowColor;
            float _Thick;
            float4  _MainTex_TexelSize; // 1テクセルを正規化した値

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        
		        return o;
            }

            fixed4 frag(v2f input) : Color
            {
                fixed4 col = tex2D(_MainTex, input.texcoord);

				// Depth を求める
				//float depth = SAMPLE_DEPTH_TEXTURE(_MainTex, input.texcoord);

				// Depth を 0 ~ 1 にする（1 が Far Clip と思われる...）
				//depth = Linear01Depth(depth);

                if(tex2D(_MainTex, input.texcoord).r <  _Threshold) col = _ShadowColor;
                return col;
            }
            ENDCG
        }
    } 
}