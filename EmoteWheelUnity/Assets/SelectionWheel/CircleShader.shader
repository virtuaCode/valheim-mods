Shader "Unlit/Circle"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _Inset("Inset", float) = 0
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed _Inset;


            // Quality level
            // 2 == high quality
            // 1 == medium quality
            // 0 == low quality
            #define QUALITY_LEVEL 1

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
                o.uv = v.texcoord - 0.5;
                o.color = v.color;
                return o;
            }



            fixed4 frag(v2f i) : SV_Target
            {
                float dist = length(i.uv);
                float dist2 = dist + _Inset * 0.5;

                #if QUALITY_LEVEL == 2
                    // length derivative, 1.5 pixel smoothstep edge
                    float pwidth = length(float2(ddx(dist), ddy(dist)));
                    float pwidth2 = length(float2(ddx(dist2), ddy(dist2)));
                    float alpha = smoothstep(0.5, 0.5 - pwidth * 1.5, dist) - smoothstep(0.5, 0.5 - pwidth2 * 1.5, dist2);
                #elif QUALITY_LEVEL == 1
                    // fwidth, 1.5 pixel smoothstep edge
                    float pwidth = fwidth(dist);
                    float pwidth2 = fwidth(dist2);
                    float alpha = smoothstep(0.5, 0.5 - pwidth * 1.5, dist) - smoothstep(0.5, 0.5 - pwidth2 * 1.5, dist2);
                #else // Low
                    // fwidth, 1 pixel linear edge
                    float pwidth = fwidth(dist);
                    float pwidth2 = fwidth(dist2);
                    float alpha = saturate((0.5 - dist) / pwidth) - saturate((0.5 - dist2) / pwidth2);

                #endif

                return fixed4(i.color.rgb, alpha * i.color.a);
        }
        ENDCG
    }
    }
}
