using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TransferMethod
{
    Blit,
    CopyTexture,
}

// Note that Meshfilter should have a mesh assigned to display the generated texture.
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class TransferGPUTexture : BaseTest<TransferMethod>
{
    Texture2D m_SourceTexture;
    RenderTexture m_TargetTexture;

    protected override void CreateTextureIfNeeded()
    {
        if (m_SourceTexture != null && m_SourceTexture.width != m_TextureSize)
        {
            DestroyImmediate(m_SourceTexture);
            m_SourceTexture = null;
        }
        if (m_SourceTexture == null)
        {
            m_SourceTexture = new Texture2D(m_TextureSize, m_TextureSize, TextureFormat.RGBA32, false);
            m_SourceTexture.wrapMode = TextureWrapMode.Clamp;

            var pixelData = m_SourceTexture.GetPixelData<Color32>(0);

            var offset = 0;
            for(var y = 0; y < m_TextureSize; ++y)
            {
                for (var x = 0; x < m_TextureSize; ++x, ++offset)
                {
                    pixelData[offset] = new Color32((byte)(x / (float)m_TextureSize * byte.MaxValue), (byte)(y / (float)m_TextureSize * byte.MaxValue), (byte)((x + y) % m_TextureSize), byte.MaxValue);
                }
            }

            m_SourceTexture.Apply();
        }

        if (m_TargetTexture != null && m_TargetTexture.width != m_TextureSize)
        {
            DestroyImmediate(m_TargetTexture);
            m_TargetTexture = null;
        }
        if (m_TargetTexture == null)
        {
            m_TargetTexture = new RenderTexture(m_TextureSize, m_TextureSize, 0, RenderTextureFormat.ARGB32);
            m_TargetTexture.wrapMode = TextureWrapMode.Clamp;
            GetComponent<Renderer>().material.mainTexture = m_TargetTexture;
        }
    }

    public void OnDisable()
    {
        DestroyImmediate(m_TargetTexture);
    }

    void UpdateBlit()
    {
        Graphics.Blit(m_SourceTexture, m_TargetTexture);
    }

    void UpdateCopyTexture()
    {
        Graphics.CopyTexture(m_SourceTexture, m_TargetTexture);
    }

    protected override void UpdateTestCaseSetup()
    {
        RenderTexture.active = m_TargetTexture;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = null;
    }

    protected override void UpdateTestCase()
    {
        switch (m_Method)
        {
            case TransferMethod.Blit: UpdateBlit(); break;
            case TransferMethod.CopyTexture: UpdateCopyTexture(); break;
        }
    }
}
