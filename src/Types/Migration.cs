using System;
using System.Collections.Generic;

namespace ms_continuus
{
    public class Migration
    {
        public readonly int Id;
        public readonly string Guid;
        public readonly string State;

        public DateTime? Started;
        public readonly List<string> Repositories;

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
