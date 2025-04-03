using System;
using System.Collections.Generic;
using System.Threading;
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

        public void SetRoot(ProcessingNode root)
        {
            m_ProcessingQueue.Clear();
            EnqueueRecursive(root);
        }

        private void EnqueueRecursive(ProcessingNode node)
        {
            if (node == null) return;

            m_ProcessingQueue.Enqueue(node);
            foreach (var child in node.Children)
                EnqueueRecursive(child);
        }

        public int RemainingProcessCount => m_ProcessingQueue.Count;

        public void Process()
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
}