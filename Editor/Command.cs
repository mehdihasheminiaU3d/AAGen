using System;
using System.Collections.Generic;

namespace AAGen
{
    public abstract class Command
    {
        public string Info { get; set; }
        public List<Command> Children { get; } = new List<Command>();

        public void AddChild(Command child) //<------------------- ToDo: probably we don't need this anymore!
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
        
        public CommandQueue(Command command)
        {
            Title = command.Info;
            AddCommand(command);
            EnqueueCommands();
        }
        
        public CommandQueue(Action action, string info)
        {
            var loadSettingsCommand = new ActionCommand(action, info);
            AddCommand(loadSettingsCommand);
            EnqueueCommands();
        }
        
        public string Title { get; set; }
        readonly Queue<Command> m_ProcessingQueue = new Queue<Command>();
        readonly Command m_Root = new ActionCommand();
        
        public int RemainingCommandCount => m_ProcessingQueue.Count;
        public Command Root => m_Root;

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

        public virtual void PreExecute()
        {
        }
        
        public virtual void PostExecute()
        {
        }

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
        
        public void AddCommand(Action action, string info=null)
        {
            Root.AddChild(new ActionCommand(action, info));
        }
    }
    
    public class ActionCommand : Command
    {
        readonly Action m_Action;
        
        public ActionCommand()
        {
            m_Action = null;
        }
        
        public ActionCommand(Action action, string info = null)
        {
            m_Action = action;
            Info = info;
        }

        protected override void OnExecute()
        {
            m_Action?.Invoke();
        }
    }
}