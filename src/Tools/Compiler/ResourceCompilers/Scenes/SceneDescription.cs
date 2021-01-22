using System;
using System.Collections.Generic;
using System.Globalization;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Scenes
{
    public class SceneDescription
    {
        public SceneDescription(IList<EntityDescription> entities)
        {
            this.Entities = entities;
        }

        public IList<EntityLayoutDescription> EntityLayouts { get; } = new List<EntityLayoutDescription>();
        public IList<EntityDescription> Entities { get; }

        public int AddEntityLayoutDescription(EntityLayoutDescription entityLayout)
        {
            if (entityLayout == null)
            {
                throw new ArgumentNullException(nameof(entityLayout));
            }

            var index = -1;

            var result = 0;
            var sortedList = new SortedList<int, string>();

            for (var i = 0; i < entityLayout.Types.Count; i++)
            {
                var typeHashCode = entityLayout.Types[i].GetHashCode(StringComparison.InvariantCulture);
                sortedList.Add(typeHashCode, entityLayout.Types[i]);
                result |= typeHashCode;
            }

            entityLayout.Types.Clear();
            ((List<string>)entityLayout.Types).AddRange(sortedList.Values);

            for (var i = 0; i < this.EntityLayouts.Count; i++)
            {
                if (this.EntityLayouts[i].LayoutHash == result)
                {
                    index = i;
                }
            }

            if (index == -1)
            {
                index = this.EntityLayouts.Count;
                entityLayout.LayoutHash = result;
                this.EntityLayouts.Add(entityLayout);
            }

            return index;
        }
    }
}