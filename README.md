# Demo.MeetupEvent

This project is a Proof of concept and a demonstration of using Azure free tier service for a dynamic website.

We have 3 elements :
- a CosmoDB database, in free tier. Data of communities events' are saved here
- A Azure Function with 2 functions : one to load news events and save it in Cosmo DB Database. The other to be an API for loading next events. Free tier service
- A Static Web App page (free tier too) to load events and display it in a page.

Real implementation for this POC was here : https://www.mtg-france.org/events/
