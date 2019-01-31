Shader "Vertex color unlit" {
Properties {
	_MainTex ("Texture", 2D) = "white" {}
}
 
Category {
	Tags {Queue=Transparent}
	Lighting Off
	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}
 
	ZWrite Off
	ZTest Always
	Cull back
	Blend SrcAlpha OneMinusSrcAlpha

	SubShader {
		Pass {
			SetTexture [_MainTex] {
				Combine texture * primary DOUBLE
			}
		}
	}
}
}