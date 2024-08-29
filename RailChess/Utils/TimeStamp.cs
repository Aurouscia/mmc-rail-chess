namespace RailChess.Utils
{
    public static class TimeStamp
    {
        private readonly static DateTime epoch = new(1970, 1, 1, 0, 0, 0);
        public static long DateTime2Long(DateTime dateTime)
        {
            return (long)(dateTime - epoch).TotalMilliseconds;
        }
        public static DateTime Long2DateTime(long timestamp)
        {
            return epoch.AddMilliseconds(timestamp);
        }
    }
}
