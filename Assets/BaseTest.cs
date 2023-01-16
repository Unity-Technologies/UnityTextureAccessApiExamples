using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

// Note that Meshfilter should have a mesh assigned to display the generated texture.
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public abstract class BaseTest<TestCase> : MonoBehaviour where TestCase : unmanaged, Enum
{
    public TestCase m_Method;
    public int m_TextureSize = 1024;

    public UnityEngine.UI.Text m_UITime;
    const float m_UIUpdateInterval = 0.1f;
    float m_UIUpdateTimer = 0.0f;

    const int MaxTextureSize = 8196;
    const int MinTextureSize = 2;

    List<float> m_History = new List<float>(100);
    int m_ValidHistoryFrames = 0;
    float m_AverageTime = float.NaN;
    float m_MedianTime = float.NaN;
    float m_MinTime = float.NaN;
    float m_MaxTime = float.NaN;

    ProfilerMarker TestCaseUpdateMarker = new ProfilerMarker("UpdateTestCase()");

    void Start()
    {
        for(var i = 0; i < m_History.Capacity; ++i)
        {
            m_History.Add(0.0f);
        }
    }

    void Update()
    {
        CreateTextureIfNeeded();

        UpdateTestCaseSetup();

        TestCaseUpdateMarker.Begin();
        var t0 = Time.realtimeSinceStartup;

        UpdateTestCase();

        var t1 = Time.realtimeSinceStartup;
        TestCaseUpdateMarker.End();

        var dt = t1 - t0;

        m_History[m_ValidHistoryFrames % m_History.Count] = dt;
        ++m_ValidHistoryFrames;

        m_UIUpdateTimer += Time.deltaTime;

        if (m_UIUpdateTimer >= m_UIUpdateInterval)
        {
            m_UIUpdateTimer = 0.0f;

            if (m_ValidHistoryFrames >= m_History.Count)
            {
                m_ValidHistoryFrames = 0;

                m_AverageTime = 0.0f;

                m_MinTime = float.PositiveInfinity;
                m_MaxTime = float.NegativeInfinity;

                {
                    for (var i = 0; i < m_History.Count; i++)
                    {
                        var time = m_History[i];
                        m_AverageTime += time;

                        m_MinTime = Mathf.Min(m_MinTime, time);
                        m_MaxTime = Mathf.Max(m_MaxTime, time);
                    }
                    m_AverageTime /= m_History.Count;
                }
                {
                    m_History.Sort();

                    // Odd-length history?
                    if ((m_History.Count & 1) != 0)
                    {
                        m_MedianTime = m_History[m_History.Count / 2];
                    }
                    else
                    {
                        m_MedianTime = (m_History[m_History.Count / 2] + m_History[m_History.Count / 2 - 1]) / 2.0f;
                    }
                }

            }
            var statistics = $"{m_History.Count} frame sample:\n average: {m_AverageTime * 1000.0f:F2}ms\n median: {m_MedianTime * 1000.0f:F2}ms\n min: {m_MinTime * 1000.0f:F2}ms\n max: {m_MaxTime * 1000.0f:F2}ms\n";

            if (m_UITime != null)
                m_UITime.text = $"{SceneManager.GetActiveScene().name} | Texture: {m_TextureSize}x{m_TextureSize} Method: {m_Method}\nLast Frame: {dt * 1000.0f:F2}ms \n{statistics}";
        }

        UpdateInputs();
    }

    void InvalidateTimings()
    {
        m_ValidHistoryFrames = 0;
        m_AverageTime = float.NaN;
        m_MedianTime = float.NaN;
        m_MinTime = float.NaN;
        m_MaxTime = float.NaN;
    }

    protected abstract void CreateTextureIfNeeded();

    protected virtual void UpdateTestCaseSetup()
    {

    }

    protected abstract void UpdateTestCase();

    void UpdateInputs()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SceneManager.LoadScene((SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            var sceneIdx = SceneManager.GetActiveScene().buildIndex;
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            SceneManager.LoadScene(sceneIdx == 0 ? (sceneCount - 1) : (sceneIdx - 1));
        }

        int testCaseValue = Convert.ToInt32(m_Method);
        int testCaseValueCount = Enum.GetValues(typeof(TestCase)).Length;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            m_Method = ToEnum<TestCase>((testCaseValue + 1) % testCaseValueCount);
            InvalidateTimings();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            m_Method = ToEnum<TestCase>(testCaseValue == 0 ? (testCaseValueCount - 1) : ((testCaseValue - 1)));
            InvalidateTimings();
        }

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            m_TextureSize = Mathf.Min(m_TextureSize * 2, MaxTextureSize);
            InvalidateTimings();
        }
        else if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            m_TextureSize = Mathf.Max(m_TextureSize / 2, MinTextureSize);
            InvalidateTimings();
        }
    }

    static TEnum ToEnum<TEnum>(int value) where TEnum : unmanaged, Enum
    {
        Span<int> span = stackalloc int[] { value };
        return MemoryMarshal.Cast<int, TEnum>(span)[0];
    }
}
