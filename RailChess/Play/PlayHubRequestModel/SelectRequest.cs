namespace RailChess.Play.PlayHubRequestModel
{
    public class SelectRequest:RequestModelBase
    {
        public List<int>? Path { get; set; }
    }
}
