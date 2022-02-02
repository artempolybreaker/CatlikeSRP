#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4

// As the atlas isn't a regular texture let's define it via the TEXTURE2D_SHADOW macro instead to be clear,
// even though it doesn't make a difference for the platforms that we support. And we'll use a special SAMPLER_CMP macro
// to define the sampler state, as this does define a different way to sample shadow maps, because regular
// bilinear filtering doesn't make sense for depth data.
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
// In fact, there's only one appropriate way to sample the shadow map, so we can define an explicit sampler state
// instead of relying on the one Unity deduces for our render texture. Sampler states can be defined inline by creating
// one with specific words in its name.
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

#endif

