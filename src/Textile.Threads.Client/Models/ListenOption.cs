using System;
using System.Collections.Generic;

namespace Textile.Threads.Client.Models
{
    public class ListenOption
    {
        public string CollectionName { get; set; }

        public string InstanceId { get; set; }

        public ActionType Action { get; set; }
    }

    public enum ActionType
    {
        All,
        Create,
        Save,
        Delete
    }
}