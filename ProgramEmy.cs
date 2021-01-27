using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;

namespace myDotNetApp
{
    class ProgramEmy
    {

        private static readonly EmyModelService emyModelService = EmyModelService.NewInstance("localhost", 3399); // connect to Emy on port 3399
        private static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library

        static async Task Main(string[] args)
        {
            Console.WriteLine("Find with Emy");
            Console.WriteLine("The current time is " + DateTime.Now);

            // call();

            try
            {

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string fullFfpmSOng = "full-ffpm.wav";
                Task task = StoreForLaterRetrieval(fullFfpmSOng);
                task.Wait();
                stopwatch.Stop();
                TimeSpan stopwatchElapsed = stopwatch.Elapsed;
                Console.WriteLine("Stores " + fullFfpmSOng + " in " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + "ms.");

                stopwatch.Start();
                string ffpmSOng = "song.wav";
                TrackData foundTrack = await GetBestMatchForSongEmy(ffpmSOng);
                Console.WriteLine("Found track : " + foundTrack.Title + " by " + foundTrack.Artist);
                stopwatch.Stop();
                stopwatchElapsed = stopwatch.Elapsed;
                Console.WriteLine("GetBestMatchForSongEmy " + ffpmSOng + " in " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + "ms.");

                string safidikoSong = "song2.wav";
                try
                {
                    TrackData trackData = await GetBestMatchForSongEmy(safidikoSong);
                    Console.WriteLine(trackData.Title);
                }
                catch (Exception)
                {
                    Console.WriteLine("Track not found from: " + safidikoSong);
                }
            }
            catch (System.Exception)
            {
                throw new Exception("Emy is down!");
            }

        }


        public static async Task StoreForLaterRetrieval(string pathToAudioFile)
        {
            var track = new TrackInfo("FFPM1", "FFPM 1", "FJKM");

            // create fingerprints
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(pathToAudioFile)
                                        .UsingServices(audioService)
                                        .Hash();

            // store hashes in the database for later retrieval
            emyModelService.Insert(track, hashedFingerprints);
        }

        public static async Task<TrackData> GetBestMatchForSongEmy(string queryAudioFile)
        {
            int secondsToAnalyze = 10; // number of seconds to analyze from query file
            int startAtSecond = 0; // start at the begining            

            // query Emy database
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                    .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                    .UsingServices(emyModelService, audioService)
                                                    .Query();

            // register matches s.t. they appear in the dashboard					
            emyModelService.RegisterMatches(queryResult.ResultEntries);

             if (queryResult.BestMatch != null)
            {
                return queryResult.BestMatch.Track;
            }
            else throw new Exception("Track not found from: " + queryAudioFile);
        }
    }
}
