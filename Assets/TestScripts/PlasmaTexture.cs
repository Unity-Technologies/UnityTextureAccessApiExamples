using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public enum PlasmaTextureMethod
{
    SetPixel,
    SetPixels,
    SetPixels32,
    SetPixels32NoConversion,
    SetPixelDataBurst,
    SetPixelDataBurstParallel,
}

// Note that Meshfilter should have a mesh assigned to display the generated texture.
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PlasmaTexture : BaseTest<PlasmaTextureMethod>
{
    Texture2D m_Texture;
    Color[] m_Colors;
    Color32[] m_Colors32;
    protected override void CreateTextureIfNeeded()
    {
        if (m_Texture != null && m_Texture.width != m_TextureSize)
        {
            DestroyImmediate(m_Texture);
            m_Texture = null;
        }
        if (m_Texture == null)
        {
            m_Texture = new Texture2D(m_TextureSize, m_TextureSize, TextureFormat.RGBA32, false);
            m_Texture.wrapMode = TextureWrapMode.Clamp;
            GetComponent<Renderer>().material.mainTexture = m_Texture;
            m_Colors = new Color[m_TextureSize * m_TextureSize];
            m_Colors32 = new Color32[m_TextureSize * m_TextureSize];
        }
    }

    public void OnDisable()
    {
        DestroyImmediate(m_Texture);
    }

    // Calculate pixel color of a classical "plasma" effect, that
    // is a combination of several moving linear & radial sine waves.
    static Color CalcPlasmaPixel(int x, int y, float invSize, float t)
    {
        var cx = x * invSize * 4.0f + t * 0.3f;
        var cy = y * invSize * 4.0f + t * 0.3f;
        var k = 0.1f + math.cos(cy + math.sin(0.148f - t)) + 2.4f * t;
        var w = 0.9f + math.sin(cx + math.cos(0.628f + t)) - 0.7f * t;
        var d = math.sqrt(cx * cx + cy * cy);
        var s = 7.0f * math.cos(d + w) * math.sin(k + w);
        var cc = math.cos(s + new float3(0.2f, 0.5f, 0.9f)) * 0.5f + 0.5f;
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

    void UpdateSetPixels32(float invSize, float t)
    {
        var idx = 0;
        for (var y = 0; y < m_TextureSize; ++y)
            for (var x = 0; x < m_TextureSize; ++x)
            {
                m_Colors32[idx++] = CalcPlasmaPixel(x, y, invSize, t);
            }
        m_Texture.SetPixels32(m_Colors32, 0);
    }

    void UpdateSetPixels32NoConversion(float invSize, float t)
    {
        var idx = 0;
        for (var y = 0; y < m_TextureSize; ++y)
            for (var x = 0; x < m_TextureSize; ++x)
            {
                // The Color -> Color32 conversion in the regular SetPixels32 sample causes it to be slower than a SetPixels approach where Unity handles the color conversion for the user.
                // This example is a little more indicative of the potential of SetPixels32 as it performs the pixel calculation but skips the conversion in favor of assigning a default black color value.
                CalcPlasmaPixel(x, y, invSize, t);
                m_Colors32[idx++] = new Color32();
            }
        m_Texture.SetPixels32(m_Colors32, 0);
    }

    void UpdateSetPixel(float invSize, float t)
    {
        for (var y = 0; y < m_TextureSize; ++y)
            for (var x = 0; x < m_TextureSize; ++x)
                m_Texture.SetPixel(x, y, CalcPlasmaPixel(x, y, invSize, t));
    }
    [BurstCompile]
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

    [BurstCompile]
    struct CalcPlasmaIntoNativeArrayBurstParallel : IJobParallelFor
    {
        // Our job accesses does not just access one element of
        // this array that maps to the job index --
        // we compute the whole row of pixels in one job invocation. Thus have to
        // tell the job safety system to stop checking that array
        // accesses map to job index on this array, via
        // the NativeDisableParallelForRestriction attribute.
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

    protected override void UpdateTestCase()
    {
        var t = Time.time;
        var invSize = 1.0f / m_TextureSize;

        switch (m_Method)
        {
            case PlasmaTextureMethod.SetPixel: UpdateSetPixel(invSize, t); break;
            case PlasmaTextureMethod.SetPixels: UpdateSetPixels(invSize, t); break;
            case PlasmaTextureMethod.SetPixels32: UpdateSetPixels32(invSize, t); break;
            case PlasmaTextureMethod.SetPixels32NoConversion: UpdateSetPixels32NoConversion(invSize, t); break;
            case PlasmaTextureMethod.SetPixelDataBurst: UpdateSetPixelDataBurst(invSize, t); break;
            case PlasmaTextureMethod.SetPixelDataBurstParallel: UpdateSetPixelDataBurstParallel(invSize, t); break;
        }
        // All the above calculations wrote new pixel values into a CPU
        // side texture memory copy. We need to send it off to the GPU now.
        m_Texture.Apply();
    }
}
