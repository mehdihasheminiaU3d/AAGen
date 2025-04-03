namespace AAGen
{
    internal class DefaultSystemSetupCreatorProcessor : NodeProcessor
    {
        public void Init()
        {
            var defaultSetupCreator = new DefaultSetupCreator();
            
            var root = new ProcessingUnit(null) { Name = "Root" };
            var child1 = new ProcessingUnit(defaultSetupCreator.CreateDefaultAddressableSettings) { Name = "CreateDefaultAddressableSettings" };
            var child2 = new ProcessingUnit(defaultSetupCreator.CreateDefaultToolSettings) { Name = "CreateDefaultToolSettings" };
            
            root.AddChild(child1);
            root.AddChild(child2);
            SetRoot(root);
        }
    }
}