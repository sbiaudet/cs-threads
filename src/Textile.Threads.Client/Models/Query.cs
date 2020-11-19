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

        public int Limit { get; set; }

        public int Skip { get; set; }

        public string Index { get; set; }


        public static Criterion Where(string fieldPath)
        {
            return new Criterion(fieldPath, null);
        }

        public Criterion And(string fieldpath)
        {
            return new(fieldpath, this);
        }

        public Query Or(Query query)
        {
            this.Ors.Add(query);
            return this;
        }

        public Query LimitTo(int limit)
        {
            this.Limit = limit;
            return this;
        }

        public Query SkipNum(int num)
        {
            this.Skip = num;
            return this;
        }

        public Query UseIndex(string path)
        {
            this.Index = path;
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
