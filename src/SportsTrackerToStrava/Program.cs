using System;
using static System.Console;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SportsTrackerToStrava
{
    public class Program
    {
        private const string SportsTrackerEp = "https://www.sports-tracker.com/apiserver/v1";
        private const string StravaEp = "https://www.strava.com/api/v3";
        private static HttpClient _client;
        private static IConfigurationRoot _config;
        private static string _sportsTrackerToken;
        private static string _stravaToken;

        public static void Main(string[] args)
        {
            _client = new HttpClient();
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _sportsTrackerToken = _config["SportsTrackerToken"];
            _stravaToken = _config["StravaToken"];

            if (args.Length == 1)
            {
                HandleSingleWorkout(args[0]).Wait();

                return;
            }

            UploadLatestWorkouts().Wait();

            WriteLine("Done everything!");
        }

        private static async Task HandleSingleWorkout(string workoutId)
        {
            WriteLine($"Doing workout id {workoutId}");

            HttpResponseMessage gpx = await GetGpxData(workoutId);

            bool success = await UploadGpx(gpx.Content);

            WriteLine(success ? "Successfully uploaded workout" : "Failed to upload workout");
        }

        private static async Task UploadLatestWorkouts()
        {
            WriteLine("Getting latest workout...");

            dynamic workouts = await GetLatestWorkouts(1);

            foreach (var workout in workouts.payload)
            {
                HttpResponseMessage gpx = await GetGpxData(workout.workoutKey.ToString());
                bool success = false;

                try
                {
                    success = await UploadGpx(gpx.Content);
                }
                catch (Exception e)
                {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine(e.Message);
                    ResetColor();
                }

                WriteLine($"{(success ? "Successfully uploaded gpx data" : "Failed to upload gpx data")} for workout {workout.workoutKey}");
            }
        }

        private static async Task<dynamic> GetLatestWorkouts(int limit = 10, int offset = 0)
        {
            dynamic workoutsResponse = await _client.GetAsync(
                $"{SportsTrackerEp}/workouts?sortonst=true&limit={limit}&offset={offset}&token={_sportsTrackerToken}");

            return JsonConvert.DeserializeObject<dynamic>(await workoutsResponse.Content.ReadAsStringAsync());
        }

        private static async Task<HttpResponseMessage> GetGpxData(string workoutId) =>
            await _client.GetAsync($"{SportsTrackerEp}/workout/exportGpx/{workoutId}?token={_sportsTrackerToken}");

        private static async Task<bool> UploadGpx(HttpContent gpx)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{StravaEp}/uploads?data_type=gpx"),
                Headers = { { "Authorization", $"Bearer {_stravaToken}" } },
                Content = new MultipartFormDataContent
                    {
                        { new ByteArrayContent(await gpx.ReadAsByteArrayAsync()), "file", "file.gpx" }
                    }
            };

            HttpResponseMessage response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                dynamic content = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                throw new Exception(content.error.ToString());
            }

            return response.IsSuccessStatusCode;
        }
    }
}
