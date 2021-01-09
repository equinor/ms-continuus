using System;
using System.Collections.Generic;

namespace ms_continuus
{
    public class Migration
    {
        public int id;
        public string guid;
        public string state;

        public DateTime? started;
        public List<String> repositories;

        public Migration(int id, string guid, string state, DateTime? started = null, List<String> repositories = null)
        {
            this.id = id;
            this.guid = guid;
            this.state = state;
            this.started = started;
            this.repositories = repositories;
        }

        public override string ToString()
        {
            return $"Migration: {{ id: {id}, guid: {guid}, state: {state}, started: {started} }}";
        }
    }
}
