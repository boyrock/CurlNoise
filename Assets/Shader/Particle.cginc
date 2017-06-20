struct GPUParticle
{
	int id;
	float3 pos;
	float3 velocity;
	float3 accelation;
	float lifeTime;
};

struct ParticleGlobal
{
	int numActiveParticle;
	int numNewParticle;
	int numMaxParticle;
};