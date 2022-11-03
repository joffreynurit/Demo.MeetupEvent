using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using AzFuncGetMeetupEventAPI.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MeetupEventsAggregator.AzFunction
{
    public class TestCancelledMeetupEvents
    {
        private readonly ILogger<LoadNewMeetupsEvents> _logger;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public TestCancelledMeetupEvents(ILogger<LoadNewMeetupsEvents> log, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _logger = log;
            _configuration = configuration;
        }

        [FunctionName("TestCancelledMeetupEvents")]
        public async Task RunAsync([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, ILogger log)
        {
            if (myTimer is null)
            {
                throw new ArgumentNullException(nameof(myTimer));
            }

            try
            {
                //return FeedloadingService.Feeds.Take(limit);

                using CosmosClient client = new(
                    accountEndpoint: _configuration.GetConnectionStringOrSetting("COSMOS_ENDPOINT")!,
                    authKeyOrResourceToken: _configuration.GetConnectionStringOrSetting("COSMOS_KEY")!
                );

                var db = client.GetDatabase("MtgFranceDb");

                var meetupEventsContainer = db.GetContainer("meetup_events");

                var meetupEvents = await GetNextEvents();


                foreach(var meetup in meetupEvents)
                {
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(meetup.Url);
                    var pageContents = await response.Content.ReadAsStringAsync();

                    var matchingJson = Regex.Matches(pageContents, "<script type=\"application\\/ld\\+json\">(.*?)<\\/script>");
                    var eventJson = matchingJson.Where(m => m.Groups[1].Value.Contains("\"@type\":\"Event\"")).First().Groups[1].Value;

                    using var jsonDocument = JsonDocument.Parse(eventJson);

                    var rootElement = jsonDocument.RootElement;
                    var statusElement = rootElement.GetProperty("eventStatus");
                    var status = statusElement.GetString();

                    if (status.Contains("Cancelled"))
                    {
                        try
                        {
                            var upsertState = await meetupEventsContainer.DeleteItemAsync<MeetupEvent>(meetup.Id, new PartitionKey(meetup.Id));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }
        }


        private async Task<List<MeetupEvent>> GetNextEvents(int limit = 10, string meetup = "")
        {
            if (limit <= 0)
                limit = 10;

            using CosmosClient client = new(
                accountEndpoint: _configuration.GetConnectionStringOrSetting("COSMOS_ENDPOINT")!,
                authKeyOrResourceToken: _configuration.GetConnectionStringOrSetting("COSMOS_KEY")!
            );

            var db = client.GetDatabase("MtgFranceDb");

            var meetupEventsContainer = db.GetContainer("meetup_events");

            IQueryable<MeetupEvent> meetupQuery = meetupEventsContainer.GetItemLinqQueryable<MeetupEvent>(true);

            if (!String.IsNullOrWhiteSpace(meetup))
                meetupQuery = meetupQuery.Where(e => e.Community == meetup);

            meetupQuery = meetupQuery
                .Where(e => e.EventDate >= DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Take(limit);

            return meetupQuery.ToList();
        }
    }
}
