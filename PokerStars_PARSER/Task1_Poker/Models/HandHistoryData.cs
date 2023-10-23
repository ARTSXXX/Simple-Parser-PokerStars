namespace Task1_Poker.Models
{
    public class HandHistoryData
    {
        public string GameID { get; set; }
        public string TableName { get; set; }
        public int TotalHands { get; set; }
        public string FileName { get; set; }
        public Dictionary<string, double> PlayerWinnings { get; set; }
    }


}