struct Node
{
	float3 position;
	float3 prevPosition;
	float3 acceleration;
	float3 velocity;
	float3 force;
	float mass;
	int group;
	int isStatic;
};

struct NodeLink
{
	int a;
	int b;
	float attractFactor;
};

struct Group
{
	int id;
	float3 center;
};