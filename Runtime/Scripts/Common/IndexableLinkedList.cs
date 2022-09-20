using System.Collections;
using System.Collections.Generic;

namespace Tuntenfisch.Lighting2D.Common
{
    public sealed class IndexableLinkedList<T> : IEnumerable<(int, T)>
    {
        #region Public Properties
        public T this[int id] => m_slots[id].m_element;
        public int Count => m_count;
        #endregion

        #region Private Fields
        private const int c_freeID = -1;

        private List<Slot> m_slots;
        private int m_count;
        private int m_nextFree;
        private int m_lastTaken;
        #endregion

        #region Public Methods
        public IndexableLinkedList()
        {
            m_slots = new List<Slot>();
            m_nextFree = c_freeID;
            m_lastTaken = c_freeID;
        }

        public int Insert(T element)
        {
            var slot = new Slot
            {
                m_element = element,
                m_next = c_freeID,
                m_last = m_lastTaken
            };
            var id = c_freeID;

            if (m_nextFree == c_freeID)
            {
                id = m_slots.Count;
                m_slots.Add(slot);
            }
            else
            {
                id = m_nextFree;
                m_nextFree = m_slots[m_nextFree].m_next;
                m_slots[id] = slot;
            }

            if (m_lastTaken != c_freeID)
            {
                var lastSlot = m_slots[m_lastTaken];
                lastSlot.m_next = id;
                m_slots[m_lastTaken] = lastSlot;
            }
            m_lastTaken = id;
            m_count++;

            return id;
        }

        public void Erase(int id)
        {
            // Retrieve the slot by id. We're going to assume the id is
            // always pointing at a valid, taken slot for now.
            var slot = m_slots[id];

            // If the slot we are erasing has a next element, then that one's
            // last will have to point to this erased slot's last.
            if (slot.HasNext)
            {
                var nextSlot = m_slots[slot.m_next];
                nextSlot.m_last = slot.m_last;
                m_slots[slot.m_next] = nextSlot;
            }

            // Likewise, if the slot we are erasing has a last element, the
            // last element's next needs to point to this slot's next.
            if (slot.HasLast)
            {
                var lastSlot = m_slots[slot.m_last];
                lastSlot.m_next = slot.m_next;
                m_slots[slot.m_last] = lastSlot;
            }

            // We also need to check if the id we are erasing is the last one that was taken.
            // If that is the case, we need to update the last taken id accordingly.
            if (m_lastTaken == id)
            {
                m_lastTaken = slot.m_last;
            }

            // After removing the slot from the doubly linked list, we erase
            // its element and update next to point to the next free id.
            m_slots[id] = new Slot
            {
                m_element = default,
                m_next = m_nextFree
            };
            m_nextFree = id;
            m_count--;
        }

        // We cannot return an interface to IEnumerator here because
        // converting our struct to the IEnumerator interface would
        // lead to boxing (heap allocation).
        public IndexableListEnumerator GetEnumeratorNonAlloc()
        {
            return new IndexableListEnumerator(this);
        }

        public IEnumerator<(int, T)> GetEnumerator()
        {
            return new IndexableListEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region Public Classes, Enums and Structs
        // We implement our own custom IEnumerator that doesn't allocate heap memory.
        public struct IndexableListEnumerator : IEnumerator<(int, T)>
        {
            #region Public Properties
            public (int, T) Current => (m_id, m_parent.m_slots[m_id].m_element);
            object IEnumerator.Current => Current;
            #endregion

            #region Private Fields
            private const int c_resetID = -2;

            private IndexableLinkedList<T> m_parent;
            private int m_id;
            #endregion

            #region Public Methods
            public IndexableListEnumerator(IndexableLinkedList<T> parent)
            {
                m_parent = parent;
                m_id = c_resetID;
            }

            public void Dispose()
            {
                m_parent = null;
            }

            public bool MoveNext()
            {
                if (m_id == c_resetID)
                {
                    m_id = m_parent.m_lastTaken;
                }
                else
                {
                    m_id = m_parent.m_slots[m_id].m_last;
                }
                return m_id != c_freeID;
            }

            public void Reset()
            {
                m_id = c_resetID;
            }

            public IndexableListEnumerator GetEnumerator()
            {
                return this;
            }
            #endregion
        }
        #endregion

        #region Private Classes, Enums and Structs
        private struct Slot
        {
            #region Public Properties
            public bool HasNext => m_next != c_freeID;
            public bool HasLast => m_last != c_freeID;
            #endregion

            #region Public Fields
            public T m_element;
            public int m_next;
            public int m_last;
            #endregion
        }
        #endregion
    }
}