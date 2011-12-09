using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SignalR.Infrastructure
{
    internal sealed class CustomStack<T>
    {
        private Node _node;

        public void Add(T value)
        {
            Node lastReadFieldValue = _node;
            while (true)
            {
                Node newNode = new Node(value, lastReadFieldValue);
                Node fieldValueDuringInterlocked = Interlocked.CompareExchange(ref _node, newNode, lastReadFieldValue);

                if (fieldValueDuringInterlocked == lastReadFieldValue)
                {
                    // We successfully updated the field value during this iteration, so there is no more work to do. 
                    return;
                }
                else
                {
                    // Otherwise, another thread updated the field before we did, so we need to retry. 
                    lastReadFieldValue = fieldValueDuringInterlocked;
                }
            }
        }

        public T[] GetAllAndClear()
        {
            Node current = Interlocked.Exchange(ref _node, null);

            if (current == null)
            {
                return null;
            }
            else
            {
                T[] all = new T[current.Remaining + 1];
                for (int i = 0; current != null; i++, current = current.Next)
                {
                    all[i] = current.Value;
                }
                return all;
            }
        }

        private sealed class Node
        {
            public readonly T Value;
            public readonly Node Next;
            public readonly int Remaining;

            public Node(T value, Node next)
            {
                Value = value;
                Next = next;
                if (next != null)
                {
                    Remaining = next.Remaining + 1;
                }
            }
        }
    }
}
