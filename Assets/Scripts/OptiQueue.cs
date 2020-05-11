using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Очередь с фиксированным размером
/// </summary>
public class OptiQueue<T> : IEnumerable
{
    private T[] items;
    private int offset = 0;

    public OptiQueue(T[] items, int count)
    {
        this.items = items;
        Count = count;
    }

    public T this[int i]
    {
        get { return items[(i + offset) % Count]; }
        set { items[(i + offset) % Count] = value; }
    }

    public void Enqueue(T item)
    {
        offset = (offset + 1) % Count;
        items[offset] = item;
    }

    public T Dequeue()
    {
        return items[(offset + 1) % Count];
    }

    public IEnumerator GetEnumerator()
    {
        return new OptiQueueEnumerator<T>(items, offset);
    }

    public void Clear()
    {
        offset = 0;
    }

    public int Count;
}

public class OptiQueueEnumerator<T> : IEnumerator
{
    T[] items;
    int position = -1;
    public OptiQueueEnumerator(T[] items, int offset)
    {
        this.items = items;
        position = offset - 1;
    }
    public object Current
    {
        get
        {
            return items[position];
        }
    }
    public void Reset()
    {
        Debug.LogError("Reset is called in OptiQueue. That is an error");
        position = -1;
    }

    public bool MoveNext()
    {
        position = (position + 1) % items.Length; 
        return true;
    }
}
