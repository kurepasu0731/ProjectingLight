Shader "Custom/RenderDepthEdge" {
    Properties {
        _MainTex ("Depth Texture", 2D) = "white" {}
        _Threshold ("Edge Threshold", Range(0.0001, 1)) = 0.01
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
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
                float tx = _MainTex_TexelSize.x * _Thick;
                float ty = _MainTex_TexelSize.y * _Thick;

                //※「DirectX9 シェーダープログラミングブック」を参考にしています。
                // 輪郭判定値の計算
                // (-1,-1) ┼─┼ (0, -1)
                //         │  │
                // (-1, 0) ┼─┼ (0,  0)
                // の4ピクセルの対角線同士でデプス値の差を取って2乗したものを加算する
                float col00 = Linear01Depth(tex2D(_MainTex, input.texcoord + half2(-tx, -ty)).r);
                float col10 = Linear01Depth(tex2D(_MainTex, input.texcoord + half2(  0, -ty)).r);
                float col01 = Linear01Depth(tex2D(_MainTex, input.texcoord + half2(-tx,   0)).r);
                float col11 = Linear01Depth(tex2D(_MainTex, input.texcoord + half2(  0,   0)).r);
                float val = (col00 - col11) * (col00 - col11) + (col10 - col01) * (col10 - col01);

                // 閾値以下ならクリップする
                if(val < _Threshold)
                {
                    clip(-1);
                }

                fixed4 col = _EdgeColor;
                return col;
            }
            ENDCG
        }
    } 
}
