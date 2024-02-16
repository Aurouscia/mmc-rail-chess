namespace RailChess.Play.PlayHubResponseModel
{
    public class TextMsg
    {
        public string Content { get; set; }
        public string Sender { get; set; }
        public string Time { get; set; }
        public TextMsgType Type { get; set; }   
        public TextMsg(string content,string sender="服务器", TextMsgType type = TextMsgType.Plain)
        {
            Content = content;
            Sender = sender;
            Time = DateTime.Now.ToString("HH:mm:ss:fff");
            Type = type;
        }
    }
    public enum TextMsgType
    {
        Plain = 0,
        Important = 1,
        Err = 2
    }
}
