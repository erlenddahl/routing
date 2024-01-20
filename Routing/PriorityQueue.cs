using System.Collections.Generic;

namespace Routing;

/// <summary>
/// Priority queue class that's slightly quicker than SortedSet because we don't need to
/// remove and re-add an item after updating its cost (we simply add it again). This may
/// cause duplicates in the queue, but that's no biggie -- they will be skipped later anyway.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class PriorityQueue<T>
{
    private readonly IComparer<T> _comparer;
    private readonly List<T> _data = new();

    public PriorityQueue(IComparer<T> comparer)
    {
        _comparer = comparer;
    }

    public void Add(T item)
    {
        _data.Add(item);
        var ci = _data.Count - 1; // child index; start at end
        while (ci > 0)
        {
            var pi = (ci - 1) / 2; // parent index
            if (_comparer.Compare(_data[ci], _data[pi]) >= 0) break; // child item is larger than (or equal) parent so we're done
            (_data[ci], _data[pi]) = (_data[pi], _data[ci]);
            ci = pi;
        }
    }

    public T Remove()
    {
        // assumes pq is not empty; up to calling code
        var li = _data.Count - 1; // last index (before removal)
        var frontItem = _data[0];   // fetch the front
        _data[0] = _data[li];
        _data.RemoveAt(li);

        --li; // last index (after removal)
        var pi = 0; // parent index. start at front of pq
        while (true)
        {
            var ci = pi * 2 + 1; // left child index of parent
            if (ci > li) break;  // no children so done
            var rc = ci + 1;     // right child
            if (rc <= li && _comparer.Compare(_data[rc], _data[ci]) < 0) ci = rc; // if rc exists and is smaller than lc, use rc instead
            if (_comparer.Compare(_data[pi], _data[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
            (_data[pi], _data[ci]) = (_data[ci], _data[pi]);
            pi = ci;
        }
        return frontItem;
    }

    public int Count => _data.Count;
}