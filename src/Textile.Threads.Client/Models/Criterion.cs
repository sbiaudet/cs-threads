using System.Text.Json.Serialization;

namespace Textile.Threads.Client.Models
{
    public class Criterion
    {
        public Criterion(string fieldPath, Query query)
        {
            FieldPath = fieldPath;
            Query = query;
        }

        public string FieldPath { get; set; }
        public Operation Operation { get; set; }
        public QueryValue Value { get; set; }

        [JsonIgnore]
        public Query Query { get; set; }


        public Query Eq(object value)
        {
            return Create(Operation.Eq, value);
        }

        public Query Ne(object value)
        {
            return Create(Operation.Ne, value);
        }

        public Query Gt(object value)
        {
            return Create(Operation.Gt, value);
        }

        public Query Lt(object value)
        {
            return Create(Operation.Lt, value);
        }

        public Query Ge(object value)
        {
            return Create(Operation.Ge, value);
        }

        public Query Le(object value)
        {
            return Create(Operation.Le, value);
        }

        private Query Create(Operation operation, object value)
        {
            this.Operation = operation;
            this.Value = QueryValue.FromObject(value);
            this.Query.Ands.Add(this);
            return this.Query;
        }
    }

    public enum Operation
    {
        Eq = 0,
        Ne,
        Gt,
        Lt,
        Ge,
        Le,
    }
}