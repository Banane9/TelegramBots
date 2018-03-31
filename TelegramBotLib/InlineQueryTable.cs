using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramBotLib
{
    public class InlineQueryTable
    {
        private readonly Timer cleanupTimer;
        private readonly Dictionary<int, TaskEntry> dictionary = new Dictionary<int, TaskEntry>();
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public InlineQueryTable()
        {
            cleanupTimer = new Timer(cleanup, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(5));
        }

        public void Cancel(int user)
        {
            lock (dictionary)
            {
                if (dictionary.ContainsKey(user))
                    dictionary[user].Cancel();
            }
        }

        public void Run(int user, Action task)
        {
            RunFor(user, task, DefaultTimeout);
        }

        public void RunFor(int user, Action task, TimeSpan timeout)
        {
            lock (dictionary)
            {
                if (dictionary.ContainsKey(user))
                    dictionary[user].Cancel();

                var taskEntry = new TaskEntry(task);
                taskEntry.RunAndCancelAfter(timeout);

                dictionary[user] = taskEntry;
            }
        }

        private void cleanup(object _)
        {
            lock (dictionary)
            {
                var users = dictionary.Keys.ToArray();

                foreach (var user in users)
                {
                    if (dictionary[user].IsDone)
                        dictionary.Remove(user);
                }
            }
        }

        private class TaskEntry
        {
            private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            private readonly Task task;

            public bool IsDone
            {
                get { return task.IsCompleted || task.IsCanceled; }
            }

            public TaskEntry(Action task)
            {
                this.task = new Task(task, cancellationTokenSource.Token);
            }

            public void Cancel()
            {
                cancellationTokenSource.Cancel();
            }

            public void RunAndCancelAfter(TimeSpan timeSpan)
            {
                task.Start();
                cancellationTokenSource.CancelAfter(timeSpan);
            }
        }
    }
}