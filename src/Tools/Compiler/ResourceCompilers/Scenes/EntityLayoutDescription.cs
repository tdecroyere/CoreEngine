using System;
using System.Collections.Generic;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Scenes
{
    public class EntityLayoutDescription
    {
        public EntityLayoutDescription()
        {
            this.Types = new List<string>();
        }

        public int LayoutHash { get; set; }
        public IList<string> Types { get; }
    }
}