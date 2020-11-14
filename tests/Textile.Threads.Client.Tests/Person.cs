using System;
using System.Text.Json.Serialization;
namespace Textile.Threads.Client.Tests
{
    public class Person
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("age")]
        public int Age { get; set; }

    }
}
