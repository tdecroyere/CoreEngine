using System;
using System.Collections.Generic;

namespace CoreEngine.Collections
{
    // TODO: Make this class thread-safe!
    public class ItemCollection<T>
    {
        private List<T> list;
        private Dictionary<ItemIdentifier, T> lookupTable;
        private uint currentId;

        public ItemCollection()
        {
            this.currentId = 0;
            this.list = new List<T>();
            this.lookupTable = new Dictionary<ItemIdentifier, T>();
        }

        public ItemIdentifier Add(T item)
        {
            var itemId = new ItemIdentifier(++this.currentId);

            this.list.Add(item);
            this.lookupTable.Add(itemId, item);

            return itemId;
        }

        public void Remove(ItemIdentifier id)
        {
            var item = this.lookupTable[id];
            this.lookupTable.Remove(id);
            this.list.Remove(item);
        }

        public bool Contains(T item)
        {
            return this.list.Contains(item);
        }

        public bool Contains(ItemIdentifier id)
        {
            return this.lookupTable.ContainsKey(id);
        }

        public int Count
        {
            get
            {
                return this.list.Count;
            }
        }

        public IList<ItemIdentifier> Keys
        {
            get
            {
                // TODO: Avoid the copy
                return new List<ItemIdentifier>(this.lookupTable.Keys);
            }
        }

        public T this[int index]
        {
            get
            {
                return this.list[index];
            }
        }

        public T this[ItemIdentifier id]
        {
            get
            {
                return this.lookupTable[id];
            }
        }

        public IList<T> ToList()
        {
            return this.list;
        }
    }
}