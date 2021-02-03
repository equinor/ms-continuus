using System;

namespace ms_continuus
{
    public static class Utility
    {
        public static DateTime DateMinusDays(int days)
        {
            var now = DateTime.Now;
            var timeSpan = new TimeSpan(days, 0, 0, 0);
            return now - timeSpan;
        }

        public static string BytesToString(long byteCount)
        {
            string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"};
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return Math.Sign(byteCount) * num + suf[place];
        }

        public static string TransferSpeed(long totalBytes, DateTime startTime)
        {
            var elapsed = DateTime.Now - startTime;
            var elapsedSeconds = elapsed.Seconds;
            if (elapsedSeconds == 0) elapsedSeconds++;
            var avgBytes = totalBytes / elapsedSeconds;
            var bytesToString = BytesToString(avgBytes);
            return $"{bytesToString}/sec";
        }
    }
}
