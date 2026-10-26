namespace Content.Shared._CentRadio;

/// Conceptually, a queue which can have new elements enqueued into it while it is being drained without those new
/// elements being drained.
/// In essence, two queues which can be swapped, say, between the two update frames of a video game.
/// Not thread safe.
public sealed partial class SwapQueue<T>
{
    private Queue<T> _elements = new();
    private Queue<T> _elementsEnqueuedWhileDraining = new();
    private bool _currentlyDraining;

    /// The number of elements in the queue, both currently draining and to-be-drained.
    public int TotalCount => _elements.Count + _elementsEnqueuedWhileDraining.Count;

    /// Adds an element to this queue. If this queue is currently being drained, the element will not be yielded by that
    /// drain, but will be yielded by the next one.
    public void Enqueue(T element)
    {
        var queue = _currentlyDraining ? _elementsEnqueuedWhileDraining : _elements;
        queue.Enqueue(element);
    }

    /// Yields every element which was enqueued before the drain began. Elements enqueued while draining are held back
    /// until the drain finishes.
    /// Any elements not consumed because the drain was abandoned early are discarded.
    public IEnumerable<T> DrainAndSwap()
    {
        if (_currentlyDraining)
            throw new InvalidOperationException("Already draining");

        _currentlyDraining = true;
        try
        {
            while (_elements.TryDequeue(out var element))
            {
                yield return element;
            }
        }
        finally
        {
            // Discard anything that wasn't consumed.
            _elements.Clear();

            (_elementsEnqueuedWhileDraining, _elements) = (_elements, _elementsEnqueuedWhileDraining);
            _currentlyDraining = false;
        }
    }
}
