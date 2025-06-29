namespace RailChess.Play.PlayHubRequestModel
{
    public class SyncMeRequest:RequestModelBase
    {
        public int TFilterId { get; set; }
        public long MyLastSyncTimeMs { get; set; }
    }
}
