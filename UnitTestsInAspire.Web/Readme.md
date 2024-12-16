# Update Database
Change to the UniTTeetsInAspire.Web directory, then execute
```dotnet ef migrations add Init --output-dir ./Data/Migrations```

To update the database, a work around is necessary, see [here](https://github.com/dotnet/aspire/issues/4497) and my question [here](https://stackoverflow.com/questions/79077499/why-cant-i-specify-a-startup-project-when-manually-updating-a-database-in-net):
- Run the App and get the connecton string from the Environmental variables in AppHost via the Aspire Dashboard (check Details page)
- While the app is still running, update the database with `dotnet ef database update --no-build --connection "..."`

To delete all database tables etc, simply delete the postgres volume in docker desktop.
