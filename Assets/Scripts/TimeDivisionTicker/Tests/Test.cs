using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private Updater m_Updater;
    [SerializeField] private int m_Channels = 0;
    [SerializeField] private int m_Tickers = 0;

    private void Start()
    {
        Application.targetFrameRate = 15;
    }
    
    private void OnEnable()
    {
        if (m_Channels == 0)
        {
            m_Updater.ticker = null;
            return;
        }

        var ticker = new TimeDivisionTicker(m_Channels);
        DoTest(ticker);
        m_Updater.ticker = ticker;
    }

    private void DoTest(TimeDivisionTicker ticker)
    {
        StringBuilder builder = new StringBuilder();
        // 插入 m_Tickers 个 action
        var handles = new int[m_Tickers];
        for (int i = 0; i < m_Tickers; ++i)
        {
            var index = i;
            handles[i] = ticker.Register(frameIndex => Debug.Log($"[{Time.frameCount}] [{frameIndex}] {index}"));
        }
        
        ticker.Dump(builder);
        Debug.Log(builder.ToString());
        builder.Clear();

        // 反注册掉第 0 个和第 m_Tickers - 1 个
        if (m_Tickers > 0)
        {
            ticker.Unregister(handles[0]);
            ticker.Unregister(handles[m_Tickers - 1]);
        }
        
        ticker.Dump(builder);
        Debug.Log(builder.ToString());
        builder.Clear();
        
        // 再往后插入两个
        ticker.Register(frameIndex => Debug.Log($"[{Time.frameCount}] [{frameIndex}] {m_Tickers}"), handles[m_Tickers - 1] + 1);
        ticker.Register(frameIndex => Debug.Log($"[{Time.frameCount}] [{frameIndex}] {m_Tickers}"));
        
        ticker.Dump(builder);
        Debug.Log(builder.ToString());
        builder.Clear();
    }
}
