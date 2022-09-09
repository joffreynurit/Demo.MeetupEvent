using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using AzFuncGetMeetupEventAPI.Models;

namespace MeetupEventsAggregator.AzFunction
{
    public class GetNextMeetupEvents
    {
        private readonly ILogger<GetNextMeetupEvents> _logger;
        private readonly IConfiguration _configuration;

        public GetNextMeetupEvents(ILogger<GetNextMeetupEvents> log, IConfiguration configuration)
        {
            _logger = log;
            _configuration = configuration;
        }

        [FunctionName("GetNextMeetupEvents")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Limit of next events displayed")]
        [OpenApiParameter(name: "meetup", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter to see next events of the selected community")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(List<MeetupEvent>), Description = "The list of nexts meetups events")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            var meetupEvents = new List<MeetupEvent>();

            int limit = 10;

            string limitStr = req.Query["limit"];
            string meetup = req.Query["meetup"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            limitStr ??= data?.limit;
            meetup ??= data?.meetup;

            if(!string.IsNullOrWhiteSpace(limitStr))
                _ = int.TryParse(limitStr, out limit);

            meetupEvents = await GetLastEvents(limit, meetup);

            return new OkObjectResult(meetupEvents);
        }



        private async Task<List<MeetupEvent>> GetLastEvents(int limit = 10, string meetup = "")
        {
            if(limit <= 0)
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

            meetupQuery = meetupQuery.OrderByDescending(e => e.EventDate)
                .Take(limit);

            return meetupQuery.ToList();
        }
    }
}

