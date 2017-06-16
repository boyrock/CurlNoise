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

    Material _mat;
    Material mat
    {
        get
        {
            if (_mat == null)
                _mat = new Material(instancedShader);
            return _mat;
        }
    }

    [SerializeField]
    Bounds bounds;

    ComputeBuffer argBuf;
    ComputeBuffer posBuf;
    ComputeBuffer posBuf1;

    Vector3[] positions;

    [SerializeField]
    ComputeShader kernel;

    uint[] argData;

    Shader instancedShader;

    MaterialPropertyBlock block;

    // Use this for initialization
    void Start ()
    {
        count = column * row;
        block = new MaterialPropertyBlock();
        block.SetFloat("_UniqueID", Random.value);

        instancedShader = Shader.Find("Hidden/InstancingShader");

        InitArgumentsBuffer();

        InitPosition();

        InitPositionBuffer();

        InitKernel();
    }

    private void InitPositionBuffer()
    {
        var vector3_stride = Marshal.SizeOf(typeof(Vector3));
        posBuf = new ComputeBuffer(count, vector3_stride);
        posBuf1 = new ComputeBuffer(count, vector3_stride);

        posBuf.SetData(positions);
        posBuf1.SetData(positions);
    }

    private void InitPosition()
    {
        positions = new Vector3[count];
        
        //int n = 0;
        //for (int i = 0; i < column; i++)
        //{
        //    for (int j = 0; j < row; j++)
        //    {
        //        positions[n] = new Vector3(i, 0, j);
        //        n++;
        //    }
        //}

        for (int i = 0; i < count; i++)
        {
            positions[i] = Random.insideUnitCircle * 30;
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

    [SerializeField]
    bool onCurlNoise;

    [Range(0,1000)]
    int time;

    // Update is called once per frame
    void Update () {

        kernel.SetFloat("_time", Time.time);

        if(Input.GetKeyDown(KeyCode.R))
        {
            kernel.SetFloat("_seed", Random.value * 1000);

            kernel.SetBuffer(0, "_initPosBuffer", posBuf1);
            kernel.SetBuffer(0, "_posBuffer", posBuf);
            kernel.Dispatch(0, count / 8 + (count % 8), 1, 1);
        }

        if (onCurlNoise)
        {
            kernel.SetBuffer(1, "_initPosBuffer", posBuf1);
            kernel.SetBuffer(1, "_posBuffer", posBuf);
            kernel.Dispatch(1, count / 8 + (count % 8), 1, 1);
        }

        mat.SetBuffer("_posBuffer", posBuf);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, bounds, argBuf, 0, block);
	}
}
