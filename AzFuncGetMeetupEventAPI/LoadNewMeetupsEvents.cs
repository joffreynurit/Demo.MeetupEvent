using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using AzFuncGetMeetupEventAPI.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MeetupEventsAggregator.AzFunction
{
    public class LoadNewMeetupsEvents
    {
        private readonly ILogger<LoadNewMeetupsEvents> _logger;
        private readonly IConfiguration _configuration;

        public LoadNewMeetupsEvents(ILogger<LoadNewMeetupsEvents> log, IConfiguration configuration)
        {
            _logger = log;
            _configuration = configuration;
        }

        [FunctionName("LoadNewMeetupsEvents")]
        public async Task RunAsync([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
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

                var communities = await InsertAndGetCommunities(db);

                await UpdateCommunitiesEvents(db, communities);
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }
        }

        private async Task<List<Community>> InsertAndGetCommunities(Database db)
        {
            var communities = new List<Community>
            {
                new Community(){ Id = "mtg-clermont-ferrand" },
                new Community(){ Id = "mtg-lille" },
                new Community(){ Id = "mtg-luxembourg" },
                new Community(){ Id = "MUGLyon" },
                new Community(){ Id = "MTG-Montpellier" },
                new Community(){ Id = "MTG-Nantes" },
                new Community(){ Id = "MTG-Rennes" },
                new Community(){ Id = "MtgStrasbourg" },
                new Community(){ Id = "MTG-Toulouse" },
                new Community(){ Id = "Meetup-MTG-Tours" },
                new Community(){ Id = "AZUG-FR" }
            };

            Container communitiesContainer = db.GetContainer("communities");

            foreach (var community in communities)
            {
                _ = await communitiesContainer.UpsertItemAsync(community);
            }

            return communities;
        }

        private async Task UpdateCommunitiesEvents(Database db, List<Community> communities)
        {
            var meetupEventsContainer = db.GetContainer("meetup_events");
            
            foreach (var feed in communities.Select(g => GetRssUrl(g.Id)))
            {
                try
                {
                    await LoadAsync(meetupEventsContainer, feed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        private string GetRssUrl(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Le nom du groupe Meetup est obligatoire", nameof(name));
            }

            return $"https://www.meetup.com/{name}/events/rss";
        }

        #region Feed



        private async Task LoadAsync(Container meetupEventsContainer, string feedUrl)
        {
            var feed = SyndicationFeed.Load(XmlReader.Create(feedUrl));

            if (feed != null)
            {
                var community = feed.Title.Text.Substring("Events - ".Length);

                foreach (var item in feed.Items)
                {
                    await SaveEvent(meetupEventsContainer, item, community);
                }
            }
        }

        private async Task SaveEvent(Container meetupEventsContainer, SyndicationItem item, string community)
        {
            MeetupEvent meetupEvent = null;

            try
            {
                int id;
                var date = ExtractDateFromSummary(item.Summary.Text);

                var matchs = Regex.Match(item.Id, @"\/events\/([\d]+?)\/");

                if (matchs.Success && matchs.Groups.Count == 2)
                {
                    var capturedId = matchs.Groups[1].Value;

                    meetupEvent = new MeetupEvent
                    {
                        Id = capturedId,
                        Community = community,
                        Title = item.Title.Text,
                        Url = item.Id,
                        PubDate = item.PublishDate.UtcDateTime,
                        EventDate = date
                    };

                    try
                    {
                        _ = await meetupEventsContainer.UpsertItemAsync(meetupEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private DateTime ExtractDateFromSummary(string summary)
        {
            var pRegex = new Regex("<p>([^<]+)</p>");
            var matches = pRegex.Matches(summary);
            var dateMatch = matches[matches.Count() - 3];

            return DateTime.ParseExact(dateMatch.Groups[1].Value, "dddd, MMMM d \"at\" h:mm tt", CultureInfo.InvariantCulture);
        }

        #endregion

    }
}
