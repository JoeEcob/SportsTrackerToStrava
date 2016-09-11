# Sports Tracker to Strava

A quick and dirty script for copying workouts from [Sports Tracker](https://www.sports-tracker.com) to [Strava](https://www.strava.com).

## Setup

Add your application keys to a `appsettings.json` file in the project root:

```JSON
{
  "SportsTrackerToken": "abc123",
  "StravaToken": "abc123"
}
```

This will then automatically copy to your output folder on build.

You can run the app by passing in a single Sports Tracker workout key:

```Shell
$ dotnet run <WORKOUT_KEY>
```

or let it upload the most recent workouts:

```Shell
$ dotnet run
```

## Todo
* Add support for uploading most recent workouts (with optional time frame)
