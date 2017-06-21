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
    Mesh[] meshs;
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

    Vector3[] vertices;
    int[] triangles;
    Vector3[] normals;

    private void Awake()
    {
        var mf = this.GetComponent<MeshFilter>();
        if(mf != null)
        {
            vertices = this.GetComponent<MeshFilter>().mesh.vertices;
            triangles = this.GetComponent<MeshFilter>().mesh.triangles;
            normals = this.GetComponent<MeshFilter>().mesh.normals;
        }

        emitter = this.GetComponent<Emitter>();
    }

    // Use this for initialization
    void Start ()
    {
        count = column * row;
        block = new MaterialPropertyBlock();
        block.SetFloat("_UniqueID", Random.value);

        instancedShader = Shader.Find("Hidden/InstancedSurfaceShader");

        mesh = meshs[0];

        InitArgumentsBuffer();
        UpdateArgumentsData(0);


        globalData = new ParticleGlobal[1];
        globalData[0] = new ParticleGlobal();

        InitPosition();

        InitParticleBuffer();

        InitColorBuffer();

        //StartCoroutine(Emit());

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
        argBuf = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
    }

    private void UpdateArgumentsData(int meshIndex)
    {
        mesh = meshs[meshIndex];
        argData = new uint[5] { mesh.GetIndexCount(0), (uint)count, 0, 0, 0 };
        argBuf.SetData(argData);
    }

    Vector3 GetPosition(int surfaceIdx, Vector3 uv)
    {
        Vector3 p1 = vertices[triangles[(surfaceIdx * 3)]];
        Vector3 p2 = vertices[triangles[(surfaceIdx * 3) + 1]];
        Vector3 p3 = vertices[triangles[(surfaceIdx * 3) + 2]];

        float u = uv.x;// data[idx].xy.x;
        float v = uv.y;// data[idx].xy.y;

        if (u + v >= 1)
        {
            u = 1 - u;
            v = 1 - v;
        }

        float a = 1 - u - v;
        float b = u;
        float c = v;

        Vector3 pointOnMesh = a * p1 + b * p2 + c * p3;

        return pointOnMesh;
    }

    Vector3 GetNormal(int idx)
    {
        Vector3 p1 = vertices[triangles[(idx * 3)]];
        Vector3 p2 = vertices[triangles[(idx * 3) + 1]];
        Vector3 p3 = vertices[triangles[(idx * 3) + 2]];

        return GetNormal(p1, p2, p3);
    }

    Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var side1 = b - a;
        var side2 = c - a;
        return Vector3.Cross(side1, side2);
    }

    // Update is called once per frame
    void Update ()
    {

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Vector3[] points = new Vector3[100];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = Random.Range(0.0f, Mathf.PI * 2.0f);

                var x = Mathf.Cos(angle) * 20;
                var y = Mathf.Sin(angle) * 20;
                points[i] = new Vector3(x, y, 0);
            }

            Emit(points);
        }

        if (Input.GetMouseButton(0))
        {
            var newParticles = emitter.Emit();
            AddParticles(newParticles);
        }

        UpdateColorBuffer();

        kernel.SetFloat("_deltaTime", Time.deltaTime);

        kernel.SetBuffer(kernel.FindKernel("UpdatePosition"), "_particleBuffer", particleBuffer);
        kernel.Dispatch(kernel.FindKernel("UpdatePosition"), count / 8 + (count % 8), 1, 1);

        mat.SetBuffer("_particleBuffer", particleBuffer);
        mat.SetBuffer("_colorBuffer", colorBuf);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, bounds, argBuf, 0, block);
    }

    void Emit(Vector3[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            var pos = points[i];
            var newParticles = emitter.Emit(pos, new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(0f, 0f)));
            AddParticles(newParticles);
        }
    }

    //private IEnumerator Emit()
    //{
    //    while (true)
    //    {
    //        yield return null;

    //        int triangleNum = this.GetComponent<MeshFilter>().mesh.triangles.Length / 3;

    //        for (int i = 0; i < triangleNum; i++)
    //        {
    //            for (int j = 0; j < 1; j++)
    //            {
    //                Vector3 pos = GetPosition(i, new Vector3(Random.value, Random.value));
    //                pos = transform.localToWorldMatrix.MultiplyPoint3x4(pos);
    //                var newParticles = emitter.Emit(pos, GetNormal(i));

    //                AddParticles(newParticles);
    //            }
    //        }
    //    }
    //}

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

        particleGlobalDataBuffer.GetData(globalData);

        newParticles = null;
    }

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

