namespace AI.Counselor.Website.Models
{   
    public class EmotionData
    {
        public int TotalImages { get; set; }
        public Dictionary<string, double> EmotionCounts { get; set; }
        public TimestampRange TimestampRange { get; set; }
    }

    public class TimestampRange
    {
        public double Start { get; set; }
        public double End { get; set; }
    }

}
