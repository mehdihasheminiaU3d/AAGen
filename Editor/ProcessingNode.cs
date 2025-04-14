using System;
using System.Collections.Generic;
using System.Threading;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    public abstract class ProcessingNode
    {
        public string Info { get; set; }
        public List<ProcessingNode> Children { get; } = new List<ProcessingNode>();

        public void AddChild(ProcessingNode child)
        {
            if (child != null && !Children.Contains(child))
                Children.Add(child);
        }

        protected abstract void OnProcess();
        public void Process() => OnProcess();
    }
    
    /// <summary>
    /// Node-based command processor
    /// </summary>
    public class CommandProcessor
    {
        public string Title { get; set; }
        readonly Queue<ProcessingNode> m_ProcessingQueue = new Queue<ProcessingNode>();
        readonly ProcessingNode m_Root = new ProcessingUnit();

        public void EnqueueCommands()
        {
            m_ProcessingQueue.Clear();
            EnqueueRecursive(m_Root);
        }

        void EnqueueRecursive(ProcessingNode node)
        {
            if (node == null) return;

            m_ProcessingQueue.Enqueue(node);
            foreach (var child in node.Children)
                EnqueueRecursive(child);
        }
        
        public int RemainingCommandCount => m_ProcessingQueue.Count;

        public ProcessingNode Root => m_Root;

        public string ExecuteNextCommand()
        {
            var currentUnit = m_ProcessingQueue.Dequeue();
            currentUnit.Process();
            return currentUnit.Info;
        }

        public void AddCommand(ProcessingNode node)
        {
            Root.AddChild(node);
        }
    }
    
    //----------------------------------------
    public class ProcessingUnit : ProcessingNode
    {
        readonly Action m_Action;
        
        public ProcessingUnit()
        {
            m_Action = null;
        }
        
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

        public Dictionary<string, GroupLayoutInfo> _groupLayout;
    }
}