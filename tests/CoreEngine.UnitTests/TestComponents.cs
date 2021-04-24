namespace CoreEngine.UnitTests
{
    partial struct TestComponent : IComponentData
    {
        public int TestField { get; set; }

        public void SetDefaultValues()
        {
            this.TestField = 5;
        }
    }

    partial struct TestComponent2 : IComponentData
    {
        public int TestField { get; set; }

        public void SetDefaultValues()
        {
            this.TestField = 10;
        }
    }
}