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
using AzFuncGetMeetupEventAPI.Helpers;
using System.Diagnostics;

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

            Stopwatch stopWatch = Stopwatch.StartNew();
            using CosmosClient client = new(
                accountEndpoint: _configuration.GetConnectionStringOrSetting("COSMOS_ENDPOINT")!,
                authKeyOrResourceToken: _configuration.GetConnectionStringOrSetting("COSMOS_KEY")!
            );

            var db = client.GetDatabase("MtgFranceDb");

            var meetupEventsContainer = db.GetContainer("meetup_events");
            _logger.LogMetric("Cosmo DB Client init", stopWatch.ElapsedMilliseconds);

            meetupEvents = await CosmoMeetupHelper.GetNextEvents(meetupEventsContainer, limit, meetup);
            _logger.LogMetric("Get Next meetup duration", stopWatch.ElapsedMilliseconds);

            stopWatch.Stop();

            _logger.LogInformation(10200, $"We load {meetupEvents.Count()} events");

            return new OkObjectResult(meetupEvents);
        }
    }
}

