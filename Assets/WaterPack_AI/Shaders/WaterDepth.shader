Shader "FantasyScenery/MatteDeepWater" {
 Properties {_MainTex("Matte Depth",2D)="white"{} _JoinTex("Surface Texture",2D)="white"{} _Join("Surface Connection",Range(0,1))=0 _Flow("Phase",Float)=0 _Opacity("Opacity",Range(0,1))=.86}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};sampler2D _MainTex,_JoinTex;float _Join,_Flow,_Opacity;
 float mirror(float x){return 1-abs(frac(x*.5)*2-1);}
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {float p=_Flow*6.2831853;float shift=.055*sin(p)+.02*sin(p*2);float u=mirror(i.uv.x*2+shift);float v=mirror(i.uv.y*2);fixed4 deep=tex2D(_MainTex,float2(u,v));fixed4 join=tex2D(_JoinTex,float2(u,0));deep.rgb=lerp(deep.rgb,join.rgb,_Join*smoothstep(.82,1,i.uv.y));deep.a=_Opacity;return deep;}
 ENDCG
 }}}
