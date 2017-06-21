using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Emitter : MonoBehaviour {

    float deltaTime;

    [SerializeField]
    int count;

    [SerializeField]
    int lifeTime;

    Vector3 startPosition;

    [SerializeField]
    float velocity;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

    }

    public GPUParticle[] Emit(EmitShapeType shape, Vector3 centerPos, float size)
    {
        deltaTime += Time.deltaTime * count;

        int intDelta = (int)deltaTime;
        deltaTime -= intDelta;
        if (intDelta == 0) return null;

        var particles = new GPUParticle[intDelta];

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].pos = centerPos + GetPoint(shape, size);
            particles[i].lifeTime = lifeTime;
        }

        return particles;
    }

    private Vector3 GetPoint(EmitShapeType shape, float size)
    {
        Vector3 pos = Vector3.one;

        switch (shape)
        {
            case EmitShapeType.Ring:
                float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
                var x = Mathf.Cos(angle) * size;
                var y = Mathf.Sin(angle) * size;
                pos = new Vector3(x, y, 0);

                break;
            case EmitShapeType.Circle:
                pos = Random.insideUnitCircle * size;
                break;
            default:
                break;
        }

        return pos;
    }

    public enum EmitShapeType
    {
        Ring,
        Circle
    }
}
