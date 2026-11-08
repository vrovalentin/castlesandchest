// Outline of an empty slot, drawn from the part's own layer: a band just inside the edge of its picture.
// Used by CastleArt instead of a separate outline image per slot. Colour comes from Graphics.DrawTexture
// (0.5 is neutral there, hence the * 2).
Shader "Hidden/Game/SlotOutline"
{
    Properties
    {
        _MainTex ("Layer", 2D) = "white" {}
        _Width ("Band width in texels", Float) = 3
        _Color ("Outline colour", Color) = (0.275, 0.431, 0.667, 0.863)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Width;
            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * 2;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float a = tex2D(_MainTex, i.uv).a;
                float2 d = _MainTex_TexelSize.xy * _Width;
                float outside = 0;
                // any point a band's width away that is outside the picture puts this pixel on the band
                outside = max(outside, 1 - tex2D(_MainTex, i.uv + float2(d.x, 0)).a);
                outside = max(outside, 1 - tex2D(_MainTex, i.uv - float2(d.x, 0)).a);
                outside = max(outside, 1 - tex2D(_MainTex, i.uv + float2(0, d.y)).a);
                outside = max(outside, 1 - tex2D(_MainTex, i.uv - float2(0, d.y)).a);
                outside = max(outside, 1 - tex2D(_MainTex, i.uv + d * 0.7).a);
                outside = max(outside, 1 - tex2D(_MainTex, i.uv - d * 0.7).a);
                outside = max(outside, 1 - tex2D(_MainTex, i.uv + float2(d.x, -d.y) * 0.7).a);
                outside = max(outside, 1 - tex2D(_MainTex, i.uv + float2(-d.x, d.y) * 0.7).a);
                float band = step(0.5, a) * step(0.5, outside);
                fixed4 c = _Color * i.color;
                c.a *= band;
                return c;
            }
            ENDCG
        }
    }
}
