﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzFuncGetMeetupEventAPI.Models
{
    public class MeetupEvent
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Community { get; set; } = default!;

        public string Title { get; set; } = default!;

        public DateTime PubDate { get; set; }

        // TODO : gérer la date de l'évènement
        public DateTime EventDate { get; set; }

        public string Url { get; set; } = default!;

    }
}
