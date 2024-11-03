using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Decal.Adapter;
using UtilityBelt.Scripting.Actions;
using static System.Net.Mime.MediaTypeNames;

namespace AllegianceSkimmer
{
  
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

                Utilities.Message("Invoking Task of InvokeChat type");
                PluginCore.Game().Actions.InvokeChat(chatTask.Message);
            }
            else
            {
                Utilities.Message("On OnTick, found Not implemented task type.");

                return;
            }
        }

        public void Enqueue(Task task)
        {
            Utilities.Message("Enqueing task...");
            tasks.Push(task);
        }

        public void Clear()
        {
            tasks.Clear();
        }
    }
}
