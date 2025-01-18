Shader "Custom/StencilOnly" {
    Properties{
        _Color("Color", Color) = (1,1,1,1)
    }
        SubShader{
            Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
            LOD 200

            ZWrite Off
            
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            
            ColorMask 0


            Pass{}
    }
        FallBack "Diffuse"
}