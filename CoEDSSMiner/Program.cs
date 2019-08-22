using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading;
using CoEDSSMiner;

namespace CoEDataMiner
{
    class Program
    {
        private static Random random = new Random();

        static void Main( string[] args )
        {
            Console.WriteLine( "Starting CoE REST scraping..." );

            var kingdoms = new[] {
                new Kingdom { Name = "AK", Id = "21337" },
                new Kingdom { Name = "Demalion", Id = "27532" },
                new Kingdom { Name = "Tryggr", Id = "17329" },
                new Kingdom { Name = "Nirath", Id = "18608" },
                new Kingdom { Name = "Arkadia", Id = "22697" }
            };

            /*var zoomLevels = new[]
            {
                new ZoomLevel() { Level = 8, MinX = -50, MaxX = 55, MinY = -105, MaxY = 90 },
                new ZoomLevel() { Level = 6, MinX = -15, MaxX = 15, MinY = -27, MaxY = 21 },
                new ZoomLevel() { Level = 4, MinX = -5, MaxX = 5, MinY = -8, MaxY = 6 },
                new ZoomLevel() { Level = 2, MinX = -3, MaxX = 3, MinY = -3, MaxY = 3 },
            };*/

            var zoomLevels = new[]
            {
                new ZoomLevel() { Level = 8, MinX = -128, MaxX = 128, MinY = -128, MaxY = 128 },
                new ZoomLevel() { Level = 6, MinX = -32, MaxX = 32, MinY = -32, MaxY = 32 },
                new ZoomLevel() { Level = 4, MinX = -8, MaxX = 8, MinY = -8, MaxY = 8 },
                new ZoomLevel() { Level = 2, MinX = -4, MaxX = 4, MinY = -4, MaxY = 4 },
            };

            /*using( var http = new HttpClient() )
            {
                foreach( (var kingdom, int i) in kingdoms.Shuffle().Select( ( val, i ) => (val, i) ) )
                {
                    var folderPath = $"./domains/{kingdom.Id}/";
                    var dir = new DirectoryInfo( folderPath );
                    if( !dir.Exists )
                        dir.Create();

                    foreach( (var duchy, int j) in DownloadData( http, kingdom.Id, "3", folderPath ).Shuffle().Select( ( val, j ) => (val, j) ) )
                    {
                        folderPath = $"./domains/{kingdom.Id}/{duchy}/";
                        dir = new DirectoryInfo( folderPath );
                        if( !dir.Exists )
                            dir.Create();

                        foreach( (var county, int k) in DownloadData( http, duchy, "2", folderPath ).Shuffle().Select( ( val, k ) => (val, k) ) )
                        {
                            folderPath = $"./domains/{kingdom.Id}/{duchy}/{county}/";
                            dir = new DirectoryInfo( folderPath );
                            if( !dir.Exists )
                                dir.Create();

                            Console.WriteLine( $"Downloading county {k + 1} of duchy {j + 1} of {kingdom.Name}" );

                            DownloadData( http, county, "1", folderPath ).Shuffle().Select( ( val, l ) => (val, l) );
                        }
                    }
                }
            }*/

            using( var http = new HttpClient() )
            {
                for( int i = 3; i <= 3; i++ )
                {
                    foreach( (var zoomLevel, int zoomCount) in zoomLevels.Shuffle().Select( ( val, j ) => (val, j) ) )
                    {
                        var range = Enumerable.Range( zoomLevel.MinX, Math.Abs( zoomLevel.MinX ) + Math.Abs( zoomLevel.MaxX ) );
                        var rangeCount = range.Count();
                        foreach( (var x, int nX) in range.Shuffle().Select( ( val, j ) => (val, j) ) )
                        {
                            outer:
                            foreach( (var y, int nY) in range.Shuffle().Select( ( val, j ) => (val, j) ) )
                            {
                                var retry = true;
                                while( retry )
                                {
                                    try
                                    {
                                        Console.WriteLine( $"Downloading tile ({x},{y}) for zoom level {zoomLevel.Level} ({( nX * rangeCount + (float) nY ) / ( rangeCount * rangeCount ) * 100:0.##} for {zoomCount + 1}/{zoomLevels.Count()})" );

                                        var filePath = $"tiles/{i}/{zoomLevel.Level}/{x + Math.Abs( Math.Min( zoomLevel.MinX, 0 ) )}_{y + Math.Abs( Math.Min( zoomLevel.MinY, 0 ) )}.png";
                                        var dir = new DirectoryInfo( "./" + filePath.Substring( 0, filePath.LastIndexOf( '/' ) ) );
                                        if( !dir.Exists )
                                            dir.Create();

                                        var req = http.GetAsync( $"https://tiles.chroniclesofelyria.com/domains/map/{i}/{zoomLevel.Level}/{x}/{y}" ).GetAwaiter().GetResult();
                                        var data = req.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                                        if( data?.Length == 0 )
                                        {
                                            goto outer;
                                        }

                                        using( var file = File.Create( filePath ) )
                                        {

                                            file.Write( data );
                                        }

                                        retry = false;
                                        Thread.Sleep( random.Next( -20, 0 ) + 20 );
                                    }
                                    catch( Exception e )
                                    {
                                        Console.WriteLine( "ERROR! " + e);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string[] DownloadDomainData( HttpClient http, string domainId, string domainPath, string folderPath )
        {
            string[] result = null;
            var retry = true;
            while( retry )
            {
                try
                {
                    var domainRequest = http.GetAsync( $"https://chroniclesofelyria.com/domains/api/domain/3/{domainPath}?parentIds={domainId}" ).GetAwaiter().GetResult();
                    var geoRequest = http.GetAsync( $"https://chroniclesofelyria.com/domains/api/domain/geo/3/{domainPath}?parentIds={domainId}" ).GetAwaiter().GetResult();

                    var domainData = domainRequest.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var geoData = geoRequest.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    using( var file = File.CreateText( folderPath + "domain.txt" ) )
                        file.WriteLine( domainData );
                    using( var file = File.CreateText( folderPath + "geo.txt" ) )
                        file.WriteLine( geoData );

                    var subDomains = JsonConvert.DeserializeAnonymousType( domainData, new[] { new { Id = "" } } );
                    result = subDomains.Select( x => x.Id ).ToArray();

                    retry = false;
                    Thread.Sleep( random.Next( -50, 50 ) + 50 );
                }
                catch( Exception e )
                {
                    Console.WriteLine( "ERROR! " + e );
                }
            }

            return result;
        }
    }
}
