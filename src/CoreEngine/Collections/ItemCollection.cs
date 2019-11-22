using System;
using System.Collections.Generic;
using CoreEngine.Diagnostics;

namespace CoreEngine.Collections
{
    // TODO: Make this class thread-safe!
    public class ItemCollection<T> where T : TrackedItem
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
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var itemId = new ItemIdentifier(++this.currentId);
            item.Id = itemId;

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

        public void CleanItems()
        {
            var itemsToRemove = new List<ItemIdentifier>();

            for (var i = 0; i < this.list.Count; i++)
            {
                var item = this.list[i];

                if (!item.IsAlive)
                {
                    itemsToRemove.Add(item.Id);
                }
            }

            for (var i = 0; i < itemsToRemove.Count; i++)
            {
                Logger.WriteMessage($"Remove {typeof(T).ToString()} item");
                Remove(itemsToRemove[i]);
            }
        }

        public void ResetItemsStatus()
        {
            for (var i = 0; i < this.list.Count; i++)
            {
                var item = this.list[i];

                item.IsAlive = false;
                item.IsDirty = false;
            }
        }
    }
}