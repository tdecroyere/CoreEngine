using System;
using System.IO;
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

        public override Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data)
        {
            var testResource = resource as TestResource;

            if (testResource != null)
            {
                using var streamReader = new StreamReader(new MemoryStream(data));
                var inputText = streamReader.ReadToEnd();
                
                testResource.Text = inputText;
                return Task<Resource>.FromResult((Resource)testResource);
            }

            throw new InvalidOperationException("Cannot load test resource");
        }
    }
}