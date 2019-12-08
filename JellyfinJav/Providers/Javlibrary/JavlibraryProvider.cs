using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using System.Web;
using MediaBrowser.Controller.Library;

namespace JellyfinJav.Providers.JavlibraryProvider
{
    public class JavlibraryProvider : IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
    {
        private readonly IHttpClient httpClient;
        private readonly ILibraryManager libraryManager;
        private static readonly Javlibrary.Client client = new Javlibrary.Client();

        public string Name => "Javlibrary";
        public int Order => 11;

        public JavlibraryProvider(IHttpClient httpClient, ILibraryManager libraryManager)
        {
            this.httpClient = httpClient;
            this.libraryManager = libraryManager;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancelToken)
        {
            var originalTitle = Utility.GetVideoOriginalTitle(info, libraryManager);

            Javlibrary.Video? result = null;
            if (info.ProviderIds.ContainsKey("Javlibrary"))
                result = await client.LoadVideo(info.ProviderIds["Javlibrary"]);
            else
                result = await client.SearchFirst(originalTitle);

            if (!result.HasValue)
                return new MetadataResult<Movie>();

            return new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    OriginalTitle = originalTitle,
                    Name = result.Value.Title,
                    ProviderIds = new Dictionary<string, string> { { "Javlibrary", result.Value.Id } },
                    Studios = new[] { result.Value.Studio }.OfType<string>().ToArray(),
                    Genres = result.Value.Genres.ToArray()
                },
                People = (from actress in result.Value.Actresses
                          select new PersonInfo
                          {
                              Name = actress,
                              Type = "JAV Actress"
                          }).ToList(),
                HasMetadata = true
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo info, CancellationToken cancelToken)
        {
            return from video in await client.Search(info.Name)
                   select new RemoteSearchResult
                   {
                       Name = video.code,
                       ProviderIds = new Dictionary<string, string>
                       {
                           // Functionality should be instead moved into the javlibrary lib
                           { "Javlibrary", HttpUtility.ParseQueryString(video.url.Query).Get("v") }
                       }
                   };
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancelToken)
        {
            return httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancelToken
            });
        }
    }
}