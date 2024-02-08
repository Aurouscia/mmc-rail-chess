namespace RailChess.Utils
{
    public static class ListRandSelectExtension
    {
        private readonly static Random r = new();
        public static T RandomSelect<T>(this List<T> list)
        {
            return list[r.Next(0, list.Count)];
        }
    }
}
