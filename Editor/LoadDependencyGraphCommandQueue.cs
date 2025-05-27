using AAGen.AssetDependencies;
using AAGen.Shared;
using Newtonsoft.Json;

namespace AAGen
{
    internal class LoadDependencyGraphCommandQueue : NewCommandQueue
    {
        readonly DataContainer m_DataContainer;
        
        public LoadDependencyGraphCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(DependencyGraphCommandQueue);
        }
        
        public override void PreExecute()
        {
            ClearQueue();
            m_DataContainer.DependencyGraph = new DependencyGraph();
            AddCommand(LoadDependencyGraph, "Load DependencyGraph");
        }
        
        void LoadDependencyGraph()
        {
            var stringData = FileUtils.LoadFromFile(Constants.DependencyGraphFilePath);
            var serializedData = JsonConvert.DeserializeObject<DependencyGraph.SerializedData>(stringData);
            m_DataContainer.DependencyGraph = DependencyGraph.Deserialize(serializedData);
        }
    }
}