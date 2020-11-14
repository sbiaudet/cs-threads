using System.Text.Json;

namespace Textile.Threads.Client.Models
{
    public class Patch
    {
        public string Type { get; set; }
        public string InstanceId { get; set; }
        public JsonDocument JsonPatch { get; set; }
    }


    public static class PatchType
    {
        public const string Delete = "delete";
        public const string Create = "create";
        public const string Save = "save";
    }
}