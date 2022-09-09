jQuery.ajax({
    url: "http://communities-events.azurewebsites.net/api/GetNextMeetupEvents?code=q4nYZ5Ehr84QXgzo3ypZO8c4c8rwZ2obLjvP4tCWqNyuAzFuVCACYA==",
    success: function( result ) {
        jQuery( "#meetup-events" ).html( result );
    }
})