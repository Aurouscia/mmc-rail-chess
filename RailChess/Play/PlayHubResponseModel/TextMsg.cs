namespace RailChess.Play.PlayHubResponseModel
{
    public class TextMsg
    {
        public string Content { get; set; }
        public string Sender { get; set; }
        public TextMsgType Type { get; set; }   
        public TextMsg(string content,string sender="服务器", TextMsgType type = TextMsgType.Plain)
        {
            Content = content;
            Sender = sender;
            Type = type;
        }
    }
    public enum TextMsgType
    {
        Plain,
        Important,
        Err
    }
}
