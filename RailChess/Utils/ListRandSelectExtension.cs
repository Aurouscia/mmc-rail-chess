namespace RailChess.Utils
{
    public static class ListRandSelectExtension
    {
        public static T? RandomSelect<T>(this List<T> list)
        {
            if(list.Count == 0)
                return default;
            return list[Random.Shared.Next(0, list.Count)];
        }
    }
}
