using System;
using System.Collections.Generic;
using AAGen.AssetDependencies;

namespace AAGen
{
    public abstract class Command
    {
        public string Info { get; set; }
        public List<Command> Children { get; } = new List<Command>();

        public void AddChild(Command child)
        {
            if (child != null && !Children.Contains(child))
                Children.Add(child);
        }

        protected abstract void OnExecute();
        public void Execute() => OnExecute();
    }
    
    /// <summary>
    /// Node-based command processor
    /// </summary>
    public class CommandQueue
    {
        public CommandQueue(){}

        public CommandQueue(string title)
        {
            Title = title;
        }
        
        public string Title { get; set; }
        readonly Queue<Command> m_ProcessingQueue = new Queue<Command>();
        readonly Command m_Root = new ActionCommand();

        public void EnqueueCommands()
        {
            m_ProcessingQueue.Clear();
            EnqueueRecursive(m_Root);
        }

        void EnqueueRecursive(Command node)
        {
            if (node == null) return;

            m_ProcessingQueue.Enqueue(node);
            foreach (var child in node.Children)
                EnqueueRecursive(child);
        }
        
        public int RemainingCommandCount => m_ProcessingQueue.Count;

        public Command Root => m_Root;

        public string ExecuteNextCommand()
        {
            var currentUnit = m_ProcessingQueue.Dequeue();
            currentUnit.Execute();
            return currentUnit.Info;
        }

        public void AddCommand(Command node)
        {
            Root.AddChild(node);
        }
    }
    
    public class ActionCommand : Command
    {
        readonly Action m_Action;
        
        public ActionCommand()
        {
            m_Action = null;
        }
        
        public ActionCommand(Action action)
        {
            m_Action = action;
        }
        
        public ActionCommand(Action action, string info)
        {
            m_Action = action;
            Info = info;
        }

        protected override void OnExecute()
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