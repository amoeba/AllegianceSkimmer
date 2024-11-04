using System.Collections.Generic;

namespace AllegianceSkimmer
{
    // Just in case
    public class Task
    {
    }

    public class InvokeChatTask : Task
    {
        string message;
        public InvokeChatTask(string _message) { message = _message; }
        public string Message { get { return message; } }
    }

    public class Queue
    {
        Stack<Task> tasks;

        public Queue()
        {
            tasks = new Stack<Task>();
        }

        public void OnTick()
        {
            if (tasks.Count <= 0)
            {
                return;
            }

            Task task = tasks.Pop();

            if (task is InvokeChatTask)
            {
                InvokeChatTask chatTask = (InvokeChatTask)task;
                PluginCore.Game().Actions.InvokeChat(chatTask.Message);
            }
            else
            {
                Utilities.Message("On OnTick, found Not implemented task type. This is a bug.");

                return;
            }
        }

        public void Enqueue(Task task)
        {
            tasks.Push(task);
        }

        public void Clear()
        {
            tasks.Clear();
        }
    }
}
