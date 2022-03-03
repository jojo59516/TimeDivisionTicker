#define DEBUG

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

public class TimeDivisionTicker
{
    private const int InvalidIndex = -1;

    private int m_FreelistHead;
    private readonly int[] m_ChannelHeads;
    private readonly List<int> m_Next;
    private readonly List<Action<int>> m_Actions;
    private int m_CurrentHint;

    public TimeDivisionTicker(int channels)
    {
        if (channels <= 0)
            throw new ArgumentOutOfRangeException(nameof(channels), channels, "must be positive");

        m_CurrentHint = 0;
        m_FreelistHead = InvalidIndex;
        m_ChannelHeads = new int[channels];
        for (int i = 0; i < m_ChannelHeads.Length; ++i)
        {
            m_ChannelHeads[i] = InvalidIndex;
        }
        m_Next = new List<int>();
        m_Actions = new List<Action<int>>();
    }

    public int Register(Action<int> action)
    {
        return Register(action, m_CurrentHint);
    }

    public int Register(Action<int> action, int hint)
    {
        m_CurrentHint = (hint + 1) % m_ChannelHeads.Length;

        int index = PopFromFreeList();
        if (index == m_Actions.Count)
        {
            m_Actions.Add(action);
        }
        else
        {
            m_Actions[index] = action;
        }
        Assert.AreEqual(m_Next.Count, m_Actions.Count);

        int channelIndex = hint % m_ChannelHeads.Length;
        ref int channelHead = ref m_ChannelHeads[channelIndex];
        PushBack(ref channelHead, index);

        return index * m_ChannelHeads.Length + channelIndex;
    }

    public void Unregister(int handle)
    {
        int index = handle / m_ChannelHeads.Length;
        if (m_Actions[index] == null)
            return;

        int channelIndex = handle % m_ChannelHeads.Length;
        ref var channelHead = ref m_ChannelHeads[channelIndex];
        bool removed = Remove(ref channelHead, index);
        Assert.IsTrue(removed);

        m_Actions[index] = null;
        // 必须先从 channel Remove 再 PushToFreeList
        PushToFreeList(index);
    }

    public void Tick(int tickCount)
    {
        var channelIndex = tickCount % m_ChannelHeads.Length;
        tickCount /= m_ChannelHeads.Length;
        var currentIndex = m_ChannelHeads[channelIndex];
        while (currentIndex != InvalidIndex)
        {
            var action = m_Actions[currentIndex];
            try
            {
                action?.Invoke(tickCount);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Assert.AreNotEqual(currentIndex , m_Next[currentIndex]);
            currentIndex = m_Next[currentIndex];
        }
    }

#if DEBUG
    public void Dump(StringBuilder builder)
    {
        builder.AppendLine("{");
        builder.AppendLine($"\t{nameof(m_CurrentHint)} = {m_CurrentHint},");
        builder.AppendLine($"\t{nameof(m_FreelistHead)} = {m_FreelistHead},");
        builder.AppendLine($"\t{nameof(m_ChannelHeads)} = {{");
        for (int i = 0; i < m_ChannelHeads.Length; ++i)
        {
            builder.AppendLine($"\t\t[{i}] = {m_ChannelHeads[i]},");
        }
        builder.AppendLine($"\t}},");
        builder.AppendLine($"\t{nameof(m_Next)} = {{");
        for (int i = 0; i < m_Next.Count; ++i)
        {
            builder.AppendLine($"\t\t[{i}] = {m_Next[i]},");
        }
        builder.AppendLine($"\t}},");
        builder.AppendLine($"\t{nameof(m_Actions)} = {{");
        for (int i = 0; i < m_Actions.Count; ++i)
        {
            builder.AppendLine($"\t\t[{i}] = {m_Actions[i]},");
        }
        builder.AppendLine($"\t}},");
        builder.AppendLine("}");
    }
#endif

    #region Linked List Operations

    private int FindPrevIndex(int head, int index)
    {
        var curr = head;
        while (curr != InvalidIndex)
        {
            var next = m_Next[curr];
            if (next == index)
                return curr;

            Assert.AreNotEqual(curr, next);
            curr = next;
        }
        return InvalidIndex;
    }

    private void PushFront(ref int head, int index)
    {
        m_Next[index] = head;
        head = index;
    }

    private void PushBack(ref int head, int index)
    {
        if (head == InvalidIndex)
        {
            head = index;
            return;
        }

        var tail = FindPrevIndex(head, InvalidIndex);
        m_Next[tail] = index;
    }

    private int PopFront(ref int head)
    {
        if (head == InvalidIndex)
            return InvalidIndex;

        int index = head;
        head = m_Next[head];
        m_Next[index] = InvalidIndex;
        return index;
    }

    private bool Remove(ref int head, int index)
    {
        if (head == InvalidIndex)
            return false;

        if (head == index)
        {
            PopFront(ref head);
            return true;
        }

        int prev = FindPrevIndex(head, index);
        m_Next[prev] = m_Next[index];
        m_Next[index] = InvalidIndex;
        return true;
    }

    #endregion

    #region Free List Operations

    private void PushToFreeList(int freeIndex)
    {
        Assert.IsTrue(freeIndex >= 0);
        PushFront(ref m_FreelistHead, freeIndex);
    }

    private int PopFromFreeList()
    {
        if (m_FreelistHead == InvalidIndex)
        {
            int freeIndex = m_Next.Count;
            m_Next.Add(InvalidIndex);
            return freeIndex;
        }

        return PopFront(ref m_FreelistHead);
    }

    #endregion
}
