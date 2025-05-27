using System;
using System.Collections.Generic;

namespace AAGen
{
    public class CommandQueue 
    {
        public CommandQueue()
        {
        }

        public CommandQueue(Action action, string info)
        {
            AddCommand(action, info);
            Title = info;
        }
        
        public string Title { get; set; }
        public int RemainingCommandCount => m_ProcessingQueue.Count;
        
        readonly Queue<Command> m_ProcessingQueue = new Queue<Command>();
        
        protected void ClearQueue()
        {
            m_ProcessingQueue.Clear();
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
            currentUnit.Action.Invoke();
            return currentUnit.Info;
        }
        
        public void AddCommand(Action action, string info = null)
        {
            m_ProcessingQueue.Enqueue(new Command
            {
                Action = action,
                Info = info,
            });
        }

        public void AddCommand(Command command)
        {
            m_ProcessingQueue.Enqueue(command);
        }
    }
}