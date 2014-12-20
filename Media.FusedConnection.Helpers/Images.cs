using Media.FusedConnection.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;

namespace Media.FusedConnection.Helpers
{
    public static class Images
    {
        private const string DeleteStem = "Images/Delete/{0}";
        private const string DeleteByUserStem = "Images/DeleteByUser?id={0}";
        private const string GetStem = "Images/GetByOwner?id={0}";
        private const string UploadStem = "Images/Upload?id={0}";
        private static Uri _cdnUrl;

        public static Uri CdnRootUri
        {
            get { return _cdnUrl; }
        }

        public static async Task DeleteImage(string id)
        {
            if (_cdnUrl == null)
                throw new InvalidOperationException("You must call Initialize(cdnRootUrl) before using this method");
            var uri = new Uri(_cdnUrl, new Uri(string.Format(DeleteStem, id), UriKind.Relative));

            var c = new HttpClient();
            await c.DeleteAsync(uri);
        }

        public static async Task DeleteByUser(string id)
        {
            if (_cdnUrl == null)
                throw new InvalidOperationException("You must call Initialize(cdnRootUrl) before using this method");
            var uri = new Uri(_cdnUrl, new Uri(string.Format(DeleteByUserStem, id), UriKind.Relative));

            var c = new HttpClient();
            await c.DeleteAsync(uri);
        }

        public static async Task<List<UploadFilesResult>> GetByOwner(string ownerIdentifier)
        {
            if (_cdnUrl == null)
                throw new InvalidOperationException("You must call Initialize(cdnRootUrl) before using this method");
            var results = new List<UploadFilesResult>();
            var uri = new Uri(_cdnUrl, new Uri(string.Format(GetStem, ownerIdentifier), UriKind.Relative));
            var c = new HttpClient();
            using (var content = new MultipartFormDataContent())
            {
                content.Add(
                    new FormUrlEncodedContent(
                        new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string,string>( "id", ownerIdentifier )
                        }));
                var t = c.PostAsync(uri, content);
                var post = t.Result;
                var result = await post.Content.ReadAsStringAsync();
                var tmp = Json.Decode<List<UploadFilesResult>>(result);
                if (tmp != null)
                    results.AddRange(tmp);
            }
            return results;
        }

        public static void Initialize(string cdnRootUrl)
        {
            _cdnUrl = new Uri(cdnRootUrl, UriKind.Absolute);
        }

        public static async Task<List<UploadFilesResult>> RedirectToCdn(this HttpRequestBase requestBase, string ownerIdentifier)
        {
            if (_cdnUrl == null)
                throw new InvalidOperationException("You must call Initialize(cdnRootUrl) before using this method");
            var results = new List<UploadFilesResult>();
            var uri = new Uri(_cdnUrl, new Uri(string.Format(UploadStem, ownerIdentifier), UriKind.Relative));

            var c = new HttpClient();
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            //send one at a time to get around a bug sending them all at once
            using (var content = new MultipartFormDataContent())
            {
                for (var i=0;i<requestBase.Files.Count;i++)
                {
                    var input = requestBase.Files[i];
                    var bites = new byte[input.ContentLength];
                    input.InputStream.Read(bites, 0, bites.Length);
                    content.Add(CreateFileContent(new MemoryStream(bites), input.FileName, input.ContentType));
                }
                var t = c.PostAsync(uri, content);
                var post = t.Result;
                var result = await post.Content.ReadAsStringAsync();
                var tmp = Json.Decode<List<UploadFilesResult>>(result);
                if (tmp != null)
                    results.AddRange(tmp);

            }
            return results;
        }
        private static StreamContent CreateFileContent(Stream stream, string fileName, string contentType)
        {
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"files\"",
                FileName = "\"" + fileName + "\""
            }; // the extra quotes are key here
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return fileContent;
        }
    }
}