using System;
using System.Collections.Generic;

namespace ms_continuus
{
    public class Migration
    {
        public readonly string Guid;
        public readonly int Id;
        public readonly List<string> Repositories;
        public readonly string State;

        public DateTime? Started;

        public Migration(int id, string guid, string state, DateTime? started = null, List<string> repositories = null)
        {
            Id = id;
            Guid = guid;
            State = state;
            Started = started;
            Repositories = repositories;
        }

        public override string ToString()
        {
            return $"Migration: {{ id: {Id}, guid: {Guid}, state: {State}, started: {Started} }}";
        }
    }
}
