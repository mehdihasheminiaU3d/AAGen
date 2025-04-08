using System;
using System.Collections.Generic;
using System.Threading;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    public abstract class ProcessingNode
    {
        public string Name { get; set; }
        public List<ProcessingNode> Children { get; } = new List<ProcessingNode>();

        public void AddChild(ProcessingNode child)
        {
            if (child != null && !Children.Contains(child))
                Children.Add(child);
        }

        protected abstract void OnProcess();
        public void Process() => OnProcess();
    }
    
    public class SampleNode : ProcessingNode
    {
        private string _message;

        public SampleNode(string message)
        {
            _message = message;
        }

        protected override void OnProcess()
        {
            Thread.Sleep(1000);
            Debug.Log($"[{Name}] {_message}");
        }
    }
    
    public class NodeProcessor 
    {
        Queue<ProcessingNode> m_ProcessingQueue = new Queue<ProcessingNode>();
        ProcessingNode m_Root;

        public void SetRoot(ProcessingNode root)
        {
            if (root == null)
            {
                Debug.LogError($"Root node cannot be null!");
                return;
            }
            
            m_Root = root;
            m_ProcessingQueue.Clear();
            EnqueueRecursive(root);
        }

        void EnqueueRecursive(ProcessingNode node)
        {
            if (node == null) return;

            m_ProcessingQueue.Enqueue(node);
            foreach (var child in node.Children)
                EnqueueRecursive(child);
        }
        
        public int RemainingProcessCount => m_ProcessingQueue.Count;

        public ProcessingNode Root => m_Root;

        public void UpdateProcess()
        {
            var currentUnit = m_ProcessingQueue.Dequeue();
            currentUnit.Process();
        }
    }
    
    //----------------------------------------
    public class ProcessingUnit : ProcessingNode
    {
        readonly Action m_Action;
        
        public ProcessingUnit(Action action)
        {
            m_Action = action;
        }

        protected override void OnProcess()
        {
            m_Action?.Invoke();
        }
    }

    public class DataContainer
    {
        public DependencyGraph m_DependencyGraph;
        public DependencyGraph m_TransposedGraph;

        public string SettingsFilePath;
        public AagenSettings Settings;

        public HashSet<AssetNode> IgnoredAssets;

        public Category _allSubgraphs;
        public Dictionary<int, HashSet<AssetNode>> _subgraphSources;
    }
}