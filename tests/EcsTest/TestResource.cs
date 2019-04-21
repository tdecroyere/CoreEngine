using System;
using CoreEngine.Resources;

namespace CoreEngine.Tests.EcsTest
{
    public class TestResource : Resource
    {
        public TestResource(string path) : base(path)
        {
            this.Text = "Empty Test Resource";
        }

        public string Text
        {
            get;
            internal set;
        }
    }
}