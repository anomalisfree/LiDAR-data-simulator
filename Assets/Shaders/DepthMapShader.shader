Shader "Custom/DepthMapShader"
{
    Properties
    {
        _DepthTex ("Depth Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _DepthTex;
            float4 _DepthTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _DepthTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = tex2D(_DepthTex, i.uv).r;
                return fixed4(depth, depth, depth, 1);
            }
            ENDCG
        }
    }
}
