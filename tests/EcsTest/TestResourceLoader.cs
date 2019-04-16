using System;
using System.Threading.Tasks;
using CoreEngine.Resources;

namespace CoreEngine.Tests.EcsTest
{
    public class TestResourceLoader : ResourceLoader
    {
        public override string Name => "Test Resource Loader";
        public override string FileExtension => ".tst";

        public TestResourceLoader()
        {

        }

        public override Resource CreateEmptyResource(string path)
        {
            return new TestResource(path);
        }

        public override Task LoadResource(Resource resource)
        {
            throw new NotImplementedException();
        }
    }
}