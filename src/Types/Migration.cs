namespace ms_continuus
{
    public class Migration
    {
        public int id;
        public string guid;
        public string state;

        public Migration(int id, string guid, string state)
        {
            this.id = id;
            this.guid = guid;
            this.state = state;
        }

        public override string ToString()
        {
            return $"id: {id}, guid: {guid}, state: {state}";
        }
    }
}
