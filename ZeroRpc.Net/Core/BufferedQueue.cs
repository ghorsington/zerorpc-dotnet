using System.Collections.Generic;

namespace ZeroRpc.Net.Core
{
    internal class BufferedQueue<T>
    {
        private readonly Queue<T> queue;

        public BufferedQueue(int capacity)
        {
            Capacity = capacity;
            queue = new Queue<T>(capacity);
        }

        public int Capacity { get; set; }

        public int Count => queue.Count;

        public bool HasCapacity => Capacity > 0;

        public void Enqueue(T item)
        {
            queue.Enqueue(item);
        }

        public T Dequeue()
        {
            return queue.Dequeue();
        }

        public void ReduceCapacity()
        {
            Capacity--;
        }
    }
}