using Newtonsoft.Json;

namespace AI.Counselor.Shared.Models
{
    public class EmotionData
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("total_images")]
        public int TotalImages { get; set; }

        [JsonProperty(PropertyName ="date")]
        public string CreatedDate { get; set; }

        [JsonProperty("emotions")]
        public List<Emotion> Emotions { get; set; }
    }

    public class EmotionResponse
    {
        [JsonProperty("emotions")]
        public List<Emotion> Emotions { get; set; }
    }

    public class Emotion
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty("emotion")]
        public string EmotionType { get; set; }
        [JsonProperty("reason")]
        public string Reason { get; set; }


        [JsonProperty("timestamp_range")]
        public TimestampRange TimestampRange { get; set; }
    }

    public class TimestampRange
    {
        [JsonProperty("start")]
        public double Start { get; set; }

        [JsonProperty("end")]
        public double End { get; set; }
    }
}
