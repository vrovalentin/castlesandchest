// A built layer of a castle whose edge is cut where the next parts will go: near that cut the picture turns into
// courses of bricks in its own colour, and some bricks of the outermost course are missing, so the edge looks like
// unfinished masonry instead of a straight cut. Edges at the building's outline are left as they are.
// _Edge is a distance field over the whole canvas (CastleArt.UpdateCutEdges): 0 on a part that is not built yet,
// rising to 1 at _Reach (canvas fractions) away from it. _At is where this layer sits on the canvas (x, y from
// the top, w, h). Colour comes from Graphics.DrawTexture (0.5 is neutral there, hence the * 2).
Shader "Hidden/Game/CutMasonry"
{
    Properties
    {
        _MainTex ("Layer", 2D) = "white" {}
        _Edge ("Distance to unbuilt parts", 2D) = "white" {}
        _At ("Layer rect on the canvas", Vector) = (0, 0, 1, 1)
        _Brick ("Brick size, canvas fractions", Vector) = (0.038, 0.017, 0, 0)
        _Mortar ("Mortar width, canvas fractions", Float) = 0.0028
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
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Edge;
            float4 _At;
            float4 _Brick;
            float _Mortar;

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

            float hash (float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            // canvas point (y from the top) <-> this layer's uv
            float2 ToCanvas (float2 uv) { return float2(_At.x + uv.x * _At.z, _At.y + (1 - uv.y) * _At.w); }
            float2 ToUv (float2 c) { return float2((c.x - _At.x) / _At.z, 1 - (c.y - _At.y) / _At.w); }
            float EdgeAt (float2 c) { return tex2Dlod(_Edge, float4(c.x, 1 - c.y, 0, 0)).r; }
            float4 LayerAt (float2 c) { return tex2Dlod(_MainTex, float4(ToUv(c), 0, 0)); }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 px = tex2D(_MainTex, i.uv);
                float2 c = ToCanvas(i.uv);
                float d = EdgeAt(c);
                if (d >= 0.999 || px.a < 0.01) return px * i.color;

                // Which brick this point is in: courses across the canvas, every other one shifted by half a brick.
                float2 b = _Brick.xy;
                float row = floor(c.y / b.y);
                float shift = fmod(abs(row), 2) * 0.5 * b.x;
                float col = floor((c.x + shift) / b.x);
                float2 cell = float2(col, row);
                float2 centre = float2((col + 0.5) * b.x - shift, (row + 0.5) * b.y);
                float dc = EdgeAt(centre);
                float rnd = hash(cell);
                // Only bricks well inside the band; the rest of the picture stays as drawn.
                if (dc > 0.78 + 0.15 * rnd) return px * i.color;
                // Some bricks of the outer course are not laid yet.
                if (dc < 0.35 && rnd < 0.5) return fixed4(0, 0, 0, 0);

                // The brick's colour: the layer's own colour around the brick's middle, without the detail.
                float4 acc = 0;
                acc += LayerAt(centre);
                acc += LayerAt(centre + float2(b.x * 0.3, 0));
                acc += LayerAt(centre - float2(b.x * 0.3, 0));
                acc += LayerAt(centre + float2(0, b.y * 0.6));
                acc += LayerAt(centre - float2(0, b.y * 0.6));
                acc += px;
                float3 base = acc.a > 0.05 ? acc.rgb / acc.a : px.rgb;
                base = lerp(base, dot(base, float3(0.3, 0.59, 0.11)).xxx, 0.15);

                // Inside the brick: a lighter top, a darker bottom, darker mortar joints around it.
                float2 local = (c - (centre - b * 0.5)) / b;      // 0..1 across the brick, y down
                float2 m = _Mortar / b;
                float face = 0.8 + 0.17 * rnd;
                face *= local.y < 0.28 ? 1.1 : local.y > 0.78 ? 0.86 : 1.0;
                float3 col3 = base * face;
                bool joint = local.x < m.x || local.x > 1 - m.x || local.y < m.y || local.y > 1 - m.y;
                if (joint) col3 = base * 0.6;
                // A dark line right at the cut, so the edge reads as an edge.
                if (d < 0.1) col3 = base * 0.45;
                return fixed4(col3, px.a) * i.color;
            }
            ENDCG
        }
    }
}
