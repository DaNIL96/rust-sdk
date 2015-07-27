#ifndef ENCODING_INCLUDED
#define ENCODING_INCLUDED

//-------------------------------------------------------------------------------------------------------------
// World-space normal encoding into 2 channels. Based on octahedron-normal vector encoding.

half2 oct_normal_encode( half3 n )
{
	half2 enc = n.xy * ( 1 / ( abs( n.x ) + abs( n.y ) + abs( n.z ) ) );
	enc = ( n.z >= 0 ) ? enc : ( ( 1 - abs( enc.yx ) ) * ( enc.xy >= 0 ? 1 : -1 ) );
	return enc * 0.5 + 0.5;
}

half3 oct_normal_decode( half2 enc )
{
	half3 n;
	n.xy = enc * 2 - 1;
	n.z = 1 - abs( n.x ) - abs( n.y );
	n.xy = ( n.z >= 0 ) ? n.xy : ( ( 1 - abs( n.yx ) ) * ( n.xy >= 0 ? 1 : -1 ) );
	return n;
}

#endif // ENCODING_INCLUDED
