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

    public GPUParticle[] Emit()
    {
        deltaTime += Time.deltaTime * count;

        int intDelta = (int)deltaTime;
        deltaTime -= intDelta;
        if (intDelta == 0) return null;

        var particles = new GPUParticle[intDelta];

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].pos = startPosition;
            //particles[i].velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
            particles[i].accelation = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(0f, -2f));
            particles[i].lifeTime = lifeTime;
        }

        return particles;

    }
}
