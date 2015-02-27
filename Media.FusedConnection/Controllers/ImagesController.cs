using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Windows.Media.Imaging;
using ImageResizer;
using Media.FusedConnection.Code;
using Media.FusedConnection.Helpers;
using Media.FusedConnection.Messages;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace Media.FusedConnection.Controllers
{
    namespace Media.FusedConnection.com.Controllers
    {
        
        
        [HandleError]
        public class ImagesController : Controller
        {
            static string _cacheSettings = "public, max-age=7776000"; // 3 months
            [System.Web.Http.HttpPost]
            public ActionResult Upload(string Id)
            {
                var result = new List<UploadFilesResult>();
                //Loop through each uploaded file
                for (var fileKey = 0; fileKey < Request.Files.Count; fileKey++)
                {
                    var file = Request.Files[fileKey];

                    if (file != null && file.ContentLength <= 0) continue; //Skip unused file controls.

                    var db = ImageStore.AddImage(new Image()
                    {
                        CreatedUtc = DateTime.UtcNow,
                        Mime = string.Empty,
                        Owner = Id,
                        Name = file.FileName,
                        Exif = string.Empty,
                        Id = Guid.NewGuid()
                    });
                    var creds = new StorageCredentials("camyam",
                        "jSOJER/BuVv78ihT9pekSdoxgy97jPDxr8J/oLBf0cYawuNSoN+sfJTJr1oUOU2SkvxZxq1EsnaxnyZfYBQ97Q==");
                    var storageAccount = new CloudStorageAccount(creds,
                        new Uri("https://camyam.blob.core.windows.net/"),
                        new Uri("https://camyam.queue.core.windows.net/"),
                        new Uri("https://camyam.table.core.windows.net/"),
                        new Uri("https://camyam.files.core.windows.net/"));
                    //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

                    // Create the blob client.
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    // Retrieve reference to a previously created container.
                    CloudBlobContainer container = blobClient.GetContainerReference("images");

                    // Retrieve reference to the blob we want to create            
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(db.Id + ".jpg");


                    //The resizing settings can specify any of 30 commands.. See http://imageresizing.net for details.
                    //Destination paths can have variables like <guid> and <ext>, or 
                    // Populate our blob with contents from the uploaded file.
                    using (var ms = new MemoryStream())
                    {
                        var i = new ImageJob(file.InputStream,
                                ms, new Instructions("width=2048;height=2048;copymetadata=true;format=jpg;mode=max"));
                        i.Build();

                        blockBlob.Properties.ContentType = "image/jpeg";
                        blockBlob.Properties.CacheControl = _cacheSettings;
                        ms.Seek(0, SeekOrigin.Begin);
                        blockBlob.UploadFromStream(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        var source = BitmapFrame.Create(ms);
                        BitmapMetadata meta = null;
                        if (source != null)
                        {
                            meta = source.Metadata as BitmapMetadata;
                        }
                        var fileModel = new UploadFilesResult()
                        {
                            Name = "azure/images/" + Path.GetFileName(blockBlob.Uri.ToString()),
                            Delete_type = "DELETE",
                            Delete_url = Url.Action("Delete", new { id = Path.GetFileNameWithoutExtension(i.FinalPath) }),
                            Size = file.ContentLength,
                            OriginalFilename = file.FileName,
                            Type = file.ContentType,
                            Url = "azure/images/" + Path.GetFileName(blockBlob.Uri.ToString()),
                            Metadata = ConvertToPairs(meta)
                        };
                        result.Add(fileModel);
                        db.Exif = JsonConvert.SerializeObject(fileModel.Metadata);
                        db.Mime = i.ResultMimeType;
                        db.Path = blockBlob.Uri.ToString();
                        //db.Path = Path.GetFileName(i.FinalPath);
                        ImageStore.UpdateImage(db);
                    }


                }

                return Json(result);
            }

            [System.Web.Http.HttpDelete]
            public ActionResult Delete(string id)
            {
                Guid imageId;
                if (Guid.TryParse(id, out imageId))
                {
                    var image = ImageStore.GetImage(imageId);
                    if (image == null)
                        return new HttpStatusCodeResult(HttpStatusCode.NoContent);
                    ImageStore.DeleteImage(image);
                    var info = new DirectoryInfo(Server.MapPath(string.Format("~/uploads"))).GetFiles(string.Format("{0}.*", image.Id));
                    if (info.Any())
                    {
                        info.ToList().ForEach(x => x.Delete());
                        return new HttpStatusCodeResult(HttpStatusCode.OK);
                    }
                }
                return new HttpNotFoundResult();
            }

            [System.Web.Http.HttpDelete]
            public ActionResult DeleteByUser(string id)
            {
                var images = ImageStore.DeleteByUser(id);
                if (images == null || !images.Any())
                    return new HttpStatusCodeResult(HttpStatusCode.NoContent);
                images.ForEach(image =>
                {
                    var info = new DirectoryInfo(Server.MapPath(string.Format("~/uploads"))).GetFiles(string.Format("{0}.*", image.Id));
                    if (info.Any())
                    {
                        info.ToList().ForEach(x => x.Delete());
                    }
                });
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            [System.Web.Http.HttpGet]
            public ActionResult Test()
            {
                return View();
            }
            [System.Web.Http.HttpPost]
            public ActionResult GetByOwner(string id)
            {
                var images = ImageStore.GetByOwner(id);
                return Json(images.Select(x => new UploadFilesResult()
                {
                    Name = string.Format("/uploads/{0}", x.Path),
                    Delete_type = "DELETE",
                    Delete_url = Url.Action("Delete", new { id = Path.GetFileNameWithoutExtension(x.Path) }),
                    Size = x.Size.GetValueOrDefault(),
                    OriginalFilename = x.Name,
                    Type = x.Mime,
                    Url = "/uploads/" + x.Path,
                    Metadata = ConvertToPairs(x.Exif)
                }));
            }

            [System.Web.Http.HttpPost]
            public ActionResult TestPost(string owner)
            {
                Images.Initialize(Url.Action("~", string.Empty, null, Request.Url.Scheme).Replace("/Home", ""));
                var result = Request.RedirectToCdn("Test");
                result.Wait();
                var res = result.Result;
                return Json(res);
            }

            private string[] Ignore = new[] { "CanFreeze", "IsFrozen", "DependencyObjectType", "IsSealed", "Dispatcher" };


            private Dictionary<string, string> ConvertToPairs<T>(T objectToConvert)
            {
                var result = new Dictionary<string, string>();
                var t = typeof(T).GetProperties();
                foreach (var p in t.Where(x => Ignore.All(i => x.Name != i)))
                {
                    try
                    {
                        var value = p.GetValue(objectToConvert);
                        if (value != null && typeof(IList<string>).IsAssignableFrom(p.PropertyType))
                        {
                            result[p.Name] = string.Join(",", value as IList<string>);
                        }
                        else
                        {
                            result[p.Name] = Convert.ToString(value);
                        }

                    }
                    catch
                    {
                        //result[p.Name] = string.Empty;
                    }
                }
                return result;
            }
        }
    }
}


