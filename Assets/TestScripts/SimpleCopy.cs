using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TextureCopyMethod
{
    SetPixel,
    SetPixels,
    SetPixels32,
    LoadRawTextureData,
    LoadRawTextureDataTemplated,
    SetPixelData,
    ConvertTexture,
    CopyTexture,
    CopyTextureNonReadable, // Note that CopyTexture will always perform the CPU data copy in the editor. Even for non-readable textures.
}

public class SimpleCopy : BaseTest<TextureCopyMethod>
{
    Texture2D m_SourceTexture;
    Texture2D m_SourceTextureNonReadable;
    Texture2D m_TargetTexture;

    protected override void CreateTextureIfNeeded()
    {
        if (m_SourceTexture != null && m_SourceTexture.width != m_TextureSize)
        {
            DestroyImmediate(m_SourceTexture);
            m_SourceTexture = null;

            DestroyImmediate(m_SourceTextureNonReadable);
            m_SourceTextureNonReadable = null;
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

            m_SourceTextureNonReadable = new Texture2D(m_SourceTexture.width, m_SourceTexture.height, m_SourceTexture.format, m_SourceTexture.mipmapCount > 1);
            m_SourceTextureNonReadable.Apply(true, true);
            Graphics.CopyTexture(m_SourceTexture, m_SourceTextureNonReadable);
        }

        if (m_TargetTexture != null && m_TargetTexture.width != m_TextureSize)
        {
            DestroyImmediate(m_TargetTexture);
            m_TargetTexture = null;
        }
        if (m_TargetTexture == null)
        {
            m_TargetTexture = new Texture2D(m_TextureSize, m_TextureSize, TextureFormat.RGBA32, false);
            m_TargetTexture.wrapMode = TextureWrapMode.Clamp;
            GetComponent<Renderer>().material.mainTexture = m_TargetTexture;
        }
    }

    public void OnDisable()
    {
        DestroyImmediate(m_TargetTexture);
    }

    void UpdateSetPixel()
    {
        for (var y = 0; y < m_TextureSize; ++y)
            for (var x = 0; x < m_TextureSize; ++x)
                m_TargetTexture.SetPixel(x, y, m_SourceTexture.GetPixel(x, y));

        m_TargetTexture.Apply();
    }

    void UpdateSetPixels()
    {
        m_TargetTexture.SetPixels(m_SourceTexture.GetPixels(0), 0);

        m_TargetTexture.Apply();
    }

    void UpdateSetPixels32()
    {
        m_TargetTexture.SetPixels32(m_SourceTexture.GetPixels32());

        m_TargetTexture.Apply();
    }

    void UpdateLoadRawTextureData()
    {
        m_TargetTexture.LoadRawTextureData(m_SourceTexture.GetRawTextureData());

        m_TargetTexture.Apply();
    }

    void UpdateLoadRawTextureDataTemplated()
    {
        m_TargetTexture.LoadRawTextureData(m_SourceTexture.GetRawTextureData<byte>());

        m_TargetTexture.Apply();
    }

    void UpdateSetPixelData()
    {
        m_TargetTexture.SetPixelData(m_SourceTexture.GetPixelData<byte>(0), 0);

        m_TargetTexture.Apply();
    }

    void UpdateConvertTexture()
    {
        Graphics.ConvertTexture(m_SourceTexture, m_TargetTexture);
    }

    void UpdateCopyTexture()
    {
        Graphics.CopyTexture(m_SourceTexture, m_TargetTexture);
    }

    void UpdateCopyTextureNonReadable()
    {
        Graphics.CopyTexture(m_SourceTextureNonReadable, m_TargetTexture);
    }

    protected override void UpdateTestCaseSetup()
    {
        Graphics.ConvertTexture(Texture2D.blackTexture, m_TargetTexture);
    }

    protected override void UpdateTestCase()
    {
        switch (m_Method)
        {
            case TextureCopyMethod.SetPixel: UpdateSetPixel(); break;
            case TextureCopyMethod.SetPixels: UpdateSetPixels(); break;
            case TextureCopyMethod.SetPixels32: UpdateSetPixels32(); break;
            case TextureCopyMethod.LoadRawTextureData: UpdateLoadRawTextureData(); break;
            case TextureCopyMethod.LoadRawTextureDataTemplated: UpdateLoadRawTextureDataTemplated(); break;
            case TextureCopyMethod.SetPixelData: UpdateSetPixelData(); break;
            case TextureCopyMethod.ConvertTexture: UpdateConvertTexture(); break;
            case TextureCopyMethod.CopyTexture: UpdateCopyTexture(); break;
            case TextureCopyMethod.CopyTextureNonReadable: UpdateCopyTextureNonReadable(); break;
        }
    }
}
