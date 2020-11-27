using System;

namespace ms_continuus
{
    public class Migration
    {
        public int id;
        public string guid;
        public string state;

        public DateTime? started;

        public Migration(int id, string guid, string state, DateTime? started = null)
        {
            this.id = id;
            this.guid = guid;
            this.state = state;
            this.started = started;
        }

        public override string ToString()
        {
            return $"Migration: {{ id: {id}, guid: {guid}, state: {state}, started: {started} }}";
        }
    }
}
