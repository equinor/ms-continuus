using System;

namespace ms_continuus
{
    public static class Utility
    {
        public static DateTime DateMinusDays(int days)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSpan = new TimeSpan(days, 0,0,0 );
            return now - timeSpan;
        }
    }

}
