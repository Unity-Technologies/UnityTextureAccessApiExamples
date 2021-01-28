using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public enum PlasmaTextureMethod
{
    SetPixels,
    SetPixel,
    SetPixelDataBurst,
    SetPixelDataBurstParallel,
}

public class PlasmaTexture : MonoBehaviour
{
    public int m_TextureSize = 256;
    public PlasmaTextureMethod m_Method = PlasmaTextureMethod.SetPixels;
    public UnityEngine.UI.Text m_UITime;
    private Texture2D m_Texture;
    private Color[] m_Colors;
    private float m_UpdateTime = -1;

    void CreateTextureIfNeeded()
    {
        if (m_Texture != null && m_Texture.width != m_TextureSize)
        {
            DestroyImmediate(m_Texture);
            m_Texture = null;
        }

        if (m_Texture == null)
        {
            m_Texture = new Texture2D(m_TextureSize, m_TextureSize, TextureFormat.RGBA32, false);
            GetComponent<Renderer>().material.mainTexture = m_Texture;
            m_Colors = new Color[m_TextureSize * m_TextureSize];
        }
    }

    private void OnDisable()
    {
        DestroyImmediate(m_Texture);
    }

    static Color CalcPlasmaPixel(int x, int y, float invSize, float t)
    {
        float cx = x * invSize * 4.0f + t * 0.3f;
        float cy = y * invSize * 4.0f + t * 0.3f;
        float k = 0.1f + math.cos(cy + math.sin(0.148f - t)) + 2.4f * t;
        float w = 0.9f + math.sin(cx + math.cos(0.628f + t)) - 0.7f * t;
        float d = math.sqrt(cx * cx + cy * cy);
        float s = 7.0f * math.cos(d + w) * math.sin(k + w);
        float3 cc = math.cos(s + new float3(0.2f, 0.5f, 0.9f)) * 0.5f + 0.5f;
        return new Color(cc.x, cc.y, cc.z, 1);
    }

    void UpdateSetPixels(float invSize, float t)
    {
        var idx = 0;
        for (var y = 0; y < m_TextureSize; ++y)
            for (var x = 0; x < m_TextureSize; ++x)
                m_Colors[idx++] = CalcPlasmaPixel(x, y, invSize, t);
        m_Texture.SetPixels(m_Colors, 0);
    }

    void UpdateSetPixel(float invSize, float t)
    {
        for (var y = 0; y < m_TextureSize; ++y)
            for (var x = 0; x < m_TextureSize; ++x)
                m_Texture.SetPixel(x, y, CalcPlasmaPixel(x, y, invSize, t));
    }

    [BurstCompile(CompileSynchronously = true)]
    struct CalcPlasmaIntoNativeArrayBurst : IJob
    {
        public NativeArray<Color32> data;
        public int textureSize;
        public float invSize;
        public float t;
        public void Execute()
        {
            var idx = 0;
            for (var y = 0; y < textureSize; ++y)
                for (var x = 0; x < textureSize; ++x)
                    data[idx++] = CalcPlasmaPixel(x, y, invSize, t);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct CalcPlasmaIntoNativeArrayBurstParallel : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<Color32> data;
        public int textureSize;
        public float invSize;
        public float t;
        public void Execute(int y)
        {
            var idx = y * textureSize;
            for (var x = 0; x < textureSize; ++x)
                data[idx++] = CalcPlasmaPixel(x, y, invSize, t);
        }
    }
    
    void UpdateSetPixelDataBurst(float invSize, float t)
    {
        var data = m_Texture.GetPixelData<Color32>(0);
        var job = new CalcPlasmaIntoNativeArrayBurst()
        {
            data = data,
            textureSize = m_TextureSize,
            invSize = invSize,
            t = t
        };
        job.Schedule().Complete();
    }
    void UpdateSetPixelDataBurstParallel(float invSize, float t)
    {
        var data = m_Texture.GetPixelData<Color32>(0);
        var job = new CalcPlasmaIntoNativeArrayBurstParallel()
        {
            data = data,
            textureSize = m_TextureSize,
            invSize = invSize,
            t = t
        };
        job.Schedule(m_TextureSize, 1).Complete();
    }

    void Update()
    {
        CreateTextureIfNeeded();

        var t = Time.time;

        var t0 = Time.realtimeSinceStartup;
        var invSize = 1.0f / m_TextureSize;
        switch (m_Method)
        {
            case PlasmaTextureMethod.SetPixels: UpdateSetPixels(invSize, t); break;
            case PlasmaTextureMethod.SetPixel: UpdateSetPixel(invSize, t); break;
            case PlasmaTextureMethod.SetPixelDataBurst: UpdateSetPixelDataBurst(invSize, t); break;
            case PlasmaTextureMethod.SetPixelDataBurstParallel: UpdateSetPixelDataBurstParallel(invSize, t); break;
        }
        m_Texture.Apply();
        var t1 = Time.realtimeSinceStartup;
        var dt = t1 - t0;
        if (m_UpdateTime < 0)
            m_UpdateTime = dt;
        else
            m_UpdateTime = Mathf.Lerp(m_UpdateTime, dt, 0.3f);
        if (m_UITime != null)
            m_UITime.text = $"Texture {m_TextureSize}x{m_TextureSize} update {m_Method}: {m_UpdateTime*1000.0f:F2}ms";
    }
}
