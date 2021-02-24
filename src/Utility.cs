using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
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
        public static void PrintVersion()
        {
            var versionFile = File.ReadAllLines("src/version");
            Console.WriteLine($"Version: {versionFile[0]}");
        }

        // MD5 hashes a List of strings, and returns the first x characters
        public static string HashStingArray(List<string> stringList, int length = 8)
        {
            var input = string.Join(",", stringList);
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString().Substring(0,length);
            }
        }
    }
}
