window.addEventListener("load", () => {
    jQuery.ajax({
        url: "https://communities-events.azurewebsites.net/api/GetNextMeetupEvents?code=q4nYZ5Ehr84QXgzo3ypZO8c4c8rwZ2obLjvP4tCWqNyuAzFuVCACYA==",
        success: function( result ) {
            console.log(jQuery(".meetup-events"));
            console.log(result);

            result.forEach(event => 
            { 
                var html = "<div class='row'>";
                
                //For test purpose
                //html += "<div>Communauté : "+JSON.stringify(event)+"</div>";

                //Col 1 -> image
                html += "<div class='col'><div><a href='"+event.url+"'><img src='"+event.eventImgUri+"'/></a></div></div>";

                //Col 2 -> infos
                html += "<div class='col'>";
                html += "<table>";
                html += "<div>Titre : <span class='event-title'><a href='"+event.url+"'>"+event.title+"</a></span></div>";
                html += "<div>Proposé par : <span>"+event.community+"</span></div>";
                html += "<div>Date : "+new Date(event.eventDate).toLocaleDateString()+"</div>";
                if(event.eventLocation != null)
                {
                    html += "<div>Lieu : "+event.eventLocation+"</div>";
                }

                html += "</table>";
                html += "</div>";

                html += "</div><hr/>";


                jQuery(".meetup-events").append(html);
         });
        }
    })
  });
