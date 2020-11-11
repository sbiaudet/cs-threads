using System;
using System.Collections.Generic;

namespace Textile.Threads.Client.Models
{
    public class Query
    {
        public Query()
        {
        }

        public Sort Sort { get; set; }
        public List<Criterion> Ands { get; set; } = new List<Criterion>();
        public List<Query> Ors { get; set; } = new List<Query>();

        public static Criterion Where(string fieldPath)
        {
            return new Criterion(fieldPath, null);
        }

        public Criterion And(string fieldpath) => new Criterion(fieldpath, this);

        public Query Or(Query query)
        {
            this.Ors.Add(query);
            return this;
        }

        public Query OrderBy(string fieldPath)
        {
            this.Sort = new Sort()
            {
                FieldPath = fieldPath,
                Desc = false
            };

            return this;
        }

        public Query OrderByDesc(string fieldPath)
        {
            this.Sort = new Sort()
            {
                FieldPath = fieldPath,
                Desc = true
            };

            return this;
        }
    }
}
