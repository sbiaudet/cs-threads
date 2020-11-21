namespace Textile.Threads.Client.Models
{
    public class ListenAction<T>
    {

        public string Collection { get; set; }
        public ActionType Action { get; set; }
        public string InstanceId { get; set; }
        public T Instance { get; set; }

    }
}
