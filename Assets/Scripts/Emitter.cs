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

        if (Input.GetMouseButton(0))
        {
            startPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
        }
    }

    public GPUParticle[] Emit(float range = 0)
    {
        deltaTime += Time.deltaTime * count;

        int intDelta = (int)deltaTime;
        deltaTime -= intDelta;
        if (intDelta == 0) return null;

        var particles = new GPUParticle[intDelta];

        for (int i = 0; i < particles.Length; i++)
        {
            var circlePoint = Random.insideUnitCircle * range;
            particles[i].pos = startPosition + new Vector3(circlePoint.x, circlePoint.y, 0);
            //particles[i].accelation = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(0f, -0.2f));
            particles[i].accelation = new Vector3(0.1f,0,0);
            particles[i].lifeTime = lifeTime;
        }

        return particles;

    }
    public GPUParticle[] Emit(Vector3 point, Vector3 normal)
    {
        deltaTime += Time.deltaTime * count;

        int intDelta = (int)deltaTime;
        deltaTime -= intDelta;
        if (intDelta == 0) return null;

        var particles = new GPUParticle[intDelta];

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].pos = point;
            particles[i].velocity = normal.normalized * velocity;// (normal + new Vector3(Random.value, Random.value, Random.value) * 3f).normalized;
            particles[i].lifeTime = lifeTime;
        }

        return particles;

    }
}
