Shader "Unlit/Triangle"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f i) : SV_Target
            {
                float dst = dot(abs(i.uv * fixed2(1, 0.5) - fixed2(0.5,0.5)), fixed(1));
                float aaf = fwidth(dst);
                float alpha = smoothstep(0.5, 0.5 - aaf * 1.5, dst);
                return fixed4(i.color.rgb, alpha * i.color.a);
            }
        ENDCG
        }
    }
}
