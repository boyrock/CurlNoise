using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Instancing : MonoBehaviour {

    int count;

    [SerializeField]
    [Range(0,1000)]
    int column;
    [SerializeField]
    [Range(0, 1000)]
    int row;

    [SerializeField]
    Mesh mesh;

    [SerializeField]
    Material mat;
    //Material mat
    //{
    //    get
    //    {
    //        if (_mat == null)
    //            _mat = new Material(instancedShader);
    //        return _mat;
    //    }
    //}

    [SerializeField]
    Bounds bounds;

    ComputeBuffer argBuf;
    ComputeBuffer particleBuffer;
    ComputeBuffer newParticleBuffer;
    ComputeBuffer particleGlobalDataBuffer;

    ComputeBuffer posBuf;
    ComputeBuffer colorBuf;

    ParticleGlobal[] globalData;
    GPUParticle[] particles;

    Vector3[] colors;

    [SerializeField]
    ComputeShader kernel;

    uint[] argData;

    Shader instancedShader;

    MaterialPropertyBlock block;
    public Gradient gradientColor;

    Emitter emitter;

    private void Awake()
    {
        emitter = this.GetComponent<Emitter>();
    }

    // Use this for initialization
    void Start ()
    {
        count = column * row;
        block = new MaterialPropertyBlock();
        block.SetFloat("_UniqueID", Random.value);

        instancedShader = Shader.Find("Hidden/InstancedSurfaceShader");

        InitArgumentsBuffer();

        globalData = new ParticleGlobal[1];
        globalData[0] = new ParticleGlobal();

        InitPosition();

        InitParticleBuffer();

        InitColorBuffer();
    }

    private void InitColorBuffer()
    {
        var vector3_stride = Marshal.SizeOf(typeof(Vector3));
        colorBuf = new ComputeBuffer(count, vector3_stride);
        UpdateColorBuffer();
    }

    private void UpdateColorBuffer()
    {
        colors = new Vector3[100];
        for (int i = 0; i < 100; i++)
        {
            var col = gradientColor.Evaluate(i / 100f);
            colors[i] = new Vector3(col.r, col.g, col.b);
        }

        colorBuf.SetData(colors);
    }

    private void InitParticleBuffer()
    {
        particleGlobalDataBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(ParticleGlobal)));
        particleBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(GPUParticle)));
        newParticleBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(GPUParticle)));

        particleBuffer.SetData(particles);
    }

    private void InitPosition()
    {
        particles = new GPUParticle[count];

        for (int i = 0; i < count; i++)
        {
            float size = Random.Range(2f, 6f);
            var pos = Random.insideUnitSphere * 30f;
            particles[i].id = i;
            particles[i].pos = new Vector4(pos.x, pos.y, pos.z, size);
            particles[i].lifeTime = 0;
        }
    }

    private void InitArgumentsBuffer()
    {
        argData = new uint[5] { mesh.GetIndexCount(0), (uint)count, 0, 0, 0 };
        argBuf = new ComputeBuffer(1, argData.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argBuf.SetData(argData);
    }

    [Range(0,1000)]
    int time;

    // Update is called once per frame
    void Update () {

        if (Input.GetMouseButton(0))
        {
            var newParticles = emitter.Emit();
            AddParticles(newParticles);
        }

        UpdateColorBuffer();

        kernel.SetFloat("_deltaTime", Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.R))
        {
            kernel.SetFloat("_seed", Random.value * 1000);

            kernel.SetBuffer(0, "_posBuffer", posBuf);
            kernel.Dispatch(0, count / 8 + (count % 8), 1, 1);
        }

        kernel.SetBuffer(kernel.FindKernel("UpdatePosition"), "_particleBuffer", particleBuffer);
        kernel.Dispatch(kernel.FindKernel("UpdatePosition"), count / 8 + (count % 8), 1, 1);

        mat.SetBuffer("_particleBuffer", particleBuffer);

        mat.SetBuffer("_colorBuffer", colorBuf);
        mat.SetFloat("_RotateAngle", Mathf.Deg2Rad * rotateAngle);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, bounds, argBuf, 0, block);
	}

    private void AddParticles(GPUParticle[] newParticles)
    {
        if (newParticles == null || newParticles.Length == 0)
            return;

        int newParticleNum = newParticles.Length;
        newParticleBuffer.SetData(newParticles);

        globalData[0].numNewParticle = newParticleNum;
        globalData[0].numMaxParticle = count;
        particleGlobalDataBuffer.SetData(globalData);

        kernel.SetBuffer(kernel.FindKernel("AddParticles"), "_particleBuffer", particleBuffer);
        kernel.SetBuffer(kernel.FindKernel("AddParticles"), "_newParticleBuffer", newParticleBuffer);
        kernel.SetBuffer(kernel.FindKernel("AddParticles"), "_globalBuffer", particleGlobalDataBuffer);
        kernel.Dispatch(kernel.FindKernel("AddParticles"), newParticles.Length / 8 + 1, 1, 1);


        //ParticleGlobal[] data = new ParticleGlobal[1];
        particleGlobalDataBuffer.GetData(globalData);
        //Debug.Log("numActiveParticle length : " + globalData[0].numActiveParticle);

        newParticles = null;
    }

    [SerializeField]
    [Range(0,360)]
    float rotateAngle;

    void OnDisable()
    {
        if (argBuf != null) argBuf.Release();
        argBuf = null;

        if (colorBuf != null) colorBuf.Release();
        colorBuf = null;

        if (particleBuffer != null) particleBuffer.Release();
        particleBuffer = null;

        if (newParticleBuffer != null) newParticleBuffer.Release();
        newParticleBuffer = null;

        if (particleGlobalDataBuffer != null) particleGlobalDataBuffer.Release();
        particleGlobalDataBuffer = null;
    }
}
public struct GPUParticle
{
    public int id;
    public Vector3 pos;
    public Vector3 velocity;
    public Vector3 accelation;
    public float lifeTime;
}

public struct ParticleGlobal
{
    public int numActiveParticle;
    public int numNewParticle;
    public int numMaxParticle;
}

