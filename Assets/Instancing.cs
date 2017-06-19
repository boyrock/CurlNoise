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
    ComputeBuffer posBuf;
    ComputeBuffer posBuf1;
    ComputeBuffer colorBuf;

    Vector4[] positions;
    Vector3[] colors;

    [SerializeField]
    ComputeShader kernel;

    uint[] argData;

    Shader instancedShader;

    MaterialPropertyBlock block;
    public Gradient gradientColor;

    // Use this for initialization
    void Start ()
    {
        count = column * row;
        block = new MaterialPropertyBlock();
        block.SetFloat("_UniqueID", Random.value);

        instancedShader = Shader.Find("Hidden/InstancedSurfaceShader");

        InitArgumentsBuffer();

        InitPosition();

        InitPositionBuffer();

        InitColorBuffer();

        InitKernel();
    }

    private void InitColorBuffer()
    {
        var vector3_stride = Marshal.SizeOf(typeof(Vector3));
        colorBuf = new ComputeBuffer(count, vector3_stride);

        colors = new Vector3[100];
        for (int i = 0; i < 100; i++)
        {
            var col = gradientColor.Evaluate(i / 100f);
            colors[i] = new Vector3(col.r, col.g, col.b);
        }

        colorBuf.SetData(colors);
    }

    private void InitPositionBuffer()
    {
        var vector4_stride = Marshal.SizeOf(typeof(Vector4));
        posBuf = new ComputeBuffer(count, vector4_stride);
        posBuf1 = new ComputeBuffer(count, vector4_stride);

        posBuf.SetData(positions);
        posBuf1.SetData(positions);
    }

    private void InitPosition()
    {
        positions = new Vector4[count];

        for (int i = 0; i < count; i++)
        {
            float size = Random.Range(2f, 6f);
            var pos = Random.insideUnitSphere * 50;
            positions[i] =  new Vector4(pos.x, pos.y, pos.z, size);
        }
    }

    private void InitArgumentsBuffer()
    {
        argData = new uint[5] { mesh.GetIndexCount(0), (uint)count, 0, 0, 0 };
        argBuf = new ComputeBuffer(1, argData.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argBuf.SetData(argData);
    }

    void InitKernel()
    {
    }

    [Range(0,1000)]
    int time;

    // Update is called once per frame
    void Update () {

        kernel.SetFloat("_time", Time.time);

        if (Input.GetKeyDown(KeyCode.R))
        {
            kernel.SetFloat("_seed", Random.value * 1000);

            kernel.SetBuffer(0, "_posBuffer", posBuf);
            kernel.SetBuffer(0, "_initPosBuffer", posBuf1);
            kernel.Dispatch(0, count / 8 + (count % 8), 1, 1);
        }

        kernel.SetBuffer(1, "_posBuffer", posBuf);
        kernel.Dispatch(1, count / 8 + (count % 8), 1, 1);

        mat.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        mat.SetMatrix("_WorldToLocal", transform.worldToLocalMatrix);

        mat.SetBuffer("_posBuffer", posBuf);

        mat.SetBuffer("_colorBuffer", colorBuf);
        mat.SetFloat("_RotateAngle", Mathf.Deg2Rad * rotateAngle);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, bounds, argBuf, 0, block);
	}

    [SerializeField]
    [Range(0,360)]
    float rotateAngle;

    void OnDisable()
    {
        if (posBuf != null) posBuf.Release();
        posBuf = null;

        if (argBuf != null) argBuf.Release();
        argBuf = null;
    }
}
