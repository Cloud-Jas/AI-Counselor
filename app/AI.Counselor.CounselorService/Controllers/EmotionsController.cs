using AI.Counselor.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using Container = Microsoft.Azure.Cosmos.Container;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace AI.Counselor.CounselorService.Controllers
{
    public class EmotionsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmotionsController> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public EmotionsController(IConfiguration configuration, ILogger<EmotionsController> logger, CosmosClient cosmosClient)
        {
            _configuration = configuration;
            _logger = logger;
            _cosmosClient = cosmosClient;
            var databaseName = _configuration["CosmosDb:DatabaseName"];
            var containerName = _configuration["CosmosDb:ContainerName"];
            _container = _cosmosClient.GetContainer(databaseName, containerName);
        }

        [HttpPut]
        [Route("/emotions/{id}")]
        public async Task<IActionResult> UpdateEmotionReason(string id, [FromBody] List<Emotion> emotions)
        {
            try
            {
                var emotionData = await GetEmotionDataById(id.ToString());

                if (emotionData != null)
                {
                    emotionData.Emotions = emotions;

                    await _container.UpsertItemAsync(emotionData);
                    
                    return Ok("Reason updated successfully.");
                }  
                
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        private async Task<EmotionData> GetEmotionDataById(string id)
        {
            try
            {
                var sqlQueryText = "SELECT * FROM c WHERE c.id = @id";
                var queryDefinition = new QueryDefinition(sqlQueryText)
                    .WithParameter("@id", id);

                var queryResultSetIterator = _container.GetItemQueryIterator<EmotionData>(queryDefinition);

                while (queryResultSetIterator.HasMoreResults)
                {
                    var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    var emotion = currentResultSet.FirstOrDefault();

                    if (emotion != null)
                    {
                        return emotion;
                    }
                }

                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        [HttpGet]
        [Route("/emotions")]
        public async Task<IActionResult> GetEmotions()
        {
            try
            {
                var queryText = "SELECT * FROM c";
                var queryDefinition = new QueryDefinition(queryText);
                var queryResultSetIterator = _container.GetItemQueryIterator<JObject>(queryDefinition);

                var emotionsList = new List<Emotion>();

                while (queryResultSetIterator.HasMoreResults)
                {
                    var currentResultSet = await queryResultSetIterator.ReadNextAsync();

                    emotionsList.AddRange(currentResultSet.SelectMany(emotionItem =>
                    {
                        var emotions = emotionItem.GetValue("emotions").ToObject<List<Emotion>>();
                        var containerItemId = emotionItem.GetValue("id").ToString();

                        return emotions.Select(emotion =>
                            new Emotion
                            {
                                Id = containerItemId,
                                EmotionType = emotion.EmotionType,
                                Reason = emotion.Reason,                                
                                TimestampRange = new TimestampRange
                                {
                                    Start = emotion.TimestampRange.Start,
                                    End = emotion.TimestampRange.End
                                }
                            });
                    }).OrderBy(x=>x.TimestampRange.Start));
                }                
                var result = new EmotionResponse { Emotions = emotionsList };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("/emotions")]
        public async Task<IActionResult> Post([FromBody] EmotionData EmotionData)
        {
            try
            {
                _logger.LogInformation($"Data from ingestion service{JsonConvert.SerializeObject(EmotionData)}");

                EmotionData.CreatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                EmotionData.Emotions.ForEach(x => x.Id = EmotionData.Id);
                
                var response = await _container.CreateItemAsync(EmotionData, new PartitionKey(EmotionData.CreatedDate));

                _logger.LogInformation("Data inserted into Cosmos DB successfully.");

                return Ok(response.Resource);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while inserting data into Cosmos DB: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }
        public string DateTimeFormat(object o)
        {
            return UnixTimeStampToDateTime((double)o).ToString("dddd, dd MMMM yyyy HH:mm:ss");
        }
        public DateTime UnixTimeStampToDateTime(double unixTimestamp)
        {
            // The Unix epoch starts from January 1, 1970
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Convert the Unix timestamp to seconds and add it to the epoch
            return epoch.AddSeconds(unixTimestamp);
        }

    }
}
