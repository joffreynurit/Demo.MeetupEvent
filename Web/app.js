window.addEventListener("load", () => {
    jQuery.ajax({
        url: "https://communities-events.azurewebsites.net/api/GetNextMeetupEvents?code=q4nYZ5Ehr84QXgzo3ypZO8c4c8rwZ2obLjvP4tCWqNyuAzFuVCACYA==",
        success: function( result ) {
            console.log(jQuery(".meetup-events"));
            console.log(result);

            result.forEach(event => 
            { 
                var html = "<div>";

                html += "<div>Communauté : "+JSON.stringify(event)+"</div>";
                html += "<div>Communauté : "+event.community+"</div>";
                html += "<div>Id : "+event.id+"</div>";
                html += "<div>Date : "+new Date(event.eventDate).toLocaleDateString()+"</div>";
                html += "<div>Titre : "+event.title+"</div>";
                html += "<div>Url : "+event.url+"</div>"; 
                html += "<div>Img : <img src='"+event.eventImgUri+"'/></div>";
                if(event.eventLocation != null)
                {
                    html += "<div>Location : "+event.eventLocation+"</div>";
                }

                html += "</div><hr/>";


                jQuery(".meetup-events").append(html);
         });
        }
    })
  });
