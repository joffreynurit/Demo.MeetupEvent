using AzFuncGetMeetupEventAPI.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzFuncGetMeetupEventAPI.Helpers
{
    public static class CosmoMeetupHelper
    {
        public static async Task<List<MeetupEvent>> GetNextEvents(Container meetupEventsContainer, int limit = 10, string meetup = "")
        {
            if (limit <= 0)
                limit = 10;

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
