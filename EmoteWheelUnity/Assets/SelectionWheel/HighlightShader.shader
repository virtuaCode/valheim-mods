Shader "Unlit/HighlightArc"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _Inset("Inset", Range(0, 1)) = 0.498
        _Degree("Degree", Range(0, 180)) = 0
        _Border("Border", Range(0, 0.05)) = 0.01
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
            fixed _Degree;
            fixed _Border;


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

            float2 rotateUV(float2 uv, float2 pivot, float rotation) {
                float sine = sin(rotation);
                float cosine = cos(rotation);

                uv -= pivot;
                uv.x = uv.x * cosine - uv.y * sine;
                uv.y = uv.x * sine + uv.y * cosine;
                uv += pivot;

                return uv;
            }

            float arc(float2 uv, float deg) {
                float dist = length(uv);
                float dist2 = dist + _Inset / 2;

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

            float leftMask = rotateUV(uv + 0.5, float2(0.5, 0.5), radians(180 - deg * 0.5));
            float rightMask = rotateUV(uv + 0.5, float2(0.5, 0.5), radians(deg * 0.5));
            float lwidth = fwidth(leftMask);
            float rwidth = fwidth(rightMask);
            leftMask = smoothstep(0.5, 0.5 - lwidth * 1.5, leftMask);
            rightMask = smoothstep(0.5, 0.5 - rwidth * 1.5, rightMask);

            return leftMask * rightMask * alpha;
        }

        float border(float2 uv, float deg) {

            float leftMask = rotateUV(uv + float2(0.5 - _Border, 0.5), float2(0.5, 0.5) - float2(_Border, 0), -radians(deg * 0.5));
            float rightMask = rotateUV(uv * float2(-1,1) + float2(0.5 - _Border, 0.5), float2(0.5, 0.5) - float2(_Border, 0), -radians(deg * 0.5));
            float lwidth = fwidth(leftMask);
            float rwidth = fwidth(rightMask);
            leftMask = smoothstep(0.5, 0.5 - lwidth * 1.5, leftMask);
            rightMask = smoothstep(0.5, 0.5 - rwidth * 1.5, rightMask);
            return max(rightMask, leftMask);
        }

        fixed4 frag(v2f i) : SV_Target
        {

            float arc1 = arc(i.uv, _Degree);
            float border1 = border(i.uv, _Degree);

            return fixed4(i.color.rgb, arc1 * 0.5 + (arc1 * border1) * 0.5);
            //return fixed4(i.color.rgb, border1);
        }
    ENDCG
        }
    }
}
