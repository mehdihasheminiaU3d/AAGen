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
        
        public virtual int RemainingCommandCount => m_ProcessingQueue.Count;
        Command Root => m_Root;

        public virtual void EnqueueCommands()
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

        public virtual string ExecuteNextCommand()
        {
            var currentUnit = m_ProcessingQueue.Dequeue();
            currentUnit.Execute();
            return currentUnit.Info;
        }

        public virtual void AddCommand(Command node)
        {
            Root.AddChild(node);
        }
        
        public virtual void AddCommand(Action action, string info=null)
        {
            Root.AddChild(new ActionCommand(action, info));
        }
    }

    public class NewCommandQueue : CommandQueue
    {
        readonly Queue<NewActionCommand> m_ProcessingQueue = new Queue<NewActionCommand>();
        public override int RemainingCommandCount => m_ProcessingQueue.Count;
        
        public override void EnqueueCommands()
        {
            throw new Exception($"{nameof(EnqueueCommands)} is deprecated");
        }

        protected void ClearQueue()
        {
            m_ProcessingQueue.Clear();
        }
        
        public override string ExecuteNextCommand()
        {
            var currentUnit = m_ProcessingQueue.Dequeue();
            currentUnit.Action.Invoke();
            return currentUnit.Info;
        }
        
        public override void AddCommand(Command node)
        {
            throw new Exception($"{nameof(AddCommand)} is deprecated");
        }

        public override void AddCommand(Action action, string info = null)
        {
            m_ProcessingQueue.Enqueue(new NewActionCommand
            {
                Action = action,
                Info = info,
            });
        }

        public void AddCommand(NewActionCommand command)
        {
            m_ProcessingQueue.Enqueue(command);
        }
    }
    
    public class ActionCommand : Command
    {
        Action m_Action;
        
        public ActionCommand()
        {
            m_Action = null;
        }
        
        public ActionCommand(Action action, string info = null)
        {
            Initialize(action, info);
        }

        public void Initialize(Action action, string info = null)
        {
            m_Action = action;
            Info = info;
        }

        protected override void OnExecute()
        {
            m_Action?.Invoke();
        }
    }
    
    public struct NewActionCommand
    {
        public Action Action;
        public string Info;
    }
}
