namespace CoreEngine
{
    public interface IComponentData
    {
        void SetDefaultValues()
        {
            
        }

        // TODO: Use the new C# static method interfaces
        ComponentHash GetComponentHash() => throw new NotImplementedException();
    }
}