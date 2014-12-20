using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Mvc;
using Media.FusedConnection.Code;
using Media.FusedConnection.Helpers;
using Media.FusedConnection.Messages;
using Newtonsoft.Json;

namespace Media.FusedConnection.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Web.Http;
    using System.Windows.Media.Imaging;
    using ImageResizer;

    namespace Media.FusedConnection.com.Controllers
    {
        public class ImagesController : Controller
        {
            [HttpPost]
            public ActionResult Upload(string Id)
            {
                var result = new List<UploadFilesResult>();
                //Loop through each uploaded file
                for(var fileKey=0;fileKey <Request.Files.Count;fileKey++)
                {
                    var file = Request.Files[fileKey];

                    if (file != null && file.ContentLength <= 0) continue; //Skip unused file controls.

                    var db = ImageStore.AddImage(new Image()
                    {
                        CreatedUtc = DateTime.UtcNow,
                        Mime =  string.Empty,
                        Owner = Id,
                        Name = file.FileName,
                        Exif = string.Empty
                    });

                    //The resizing settings can specify any of 30 commands.. See http://imageresizing.net for details.
                    //Destination paths can have variables like <guid> and <ext>, or 

                    var i = new ImageJob(file, string.Format("~/uploads/{0}.<ext>",db.Id), new Instructions(
                                                                             "width=2048;height=2048;mode=max;copymetadata=true"))
                        {
                            CreateParentDirectory = true
                        };
                    i.Build();
                    var source = BitmapFrame.Create(new Uri(i.FinalPath));
                    BitmapMetadata meta = null;
                    if (source != null)
                    {
                        meta = source.Metadata as BitmapMetadata;
                    }
                    var fileModel = new UploadFilesResult()
                        {
                            Name = string.Format("/uploads/{0}",Path.GetFileName(i.FinalPath)) ,
                            Delete_type = "DELETE",
                            Delete_url = Url.Action("Delete", new { id = Path.GetFileNameWithoutExtension(i.FinalPath) }),
                            Size = file.ContentLength,
                            OriginalFilename = file.FileName,
                            Type = file.ContentType,
                            Url = "/uploads/" + Path.GetFileName(i.FinalPath),
                            Metadata = ConvertToPairs(meta)
                        };
                    result.Add(fileModel);
                    db.Exif = JsonConvert.SerializeObject(fileModel.Metadata);
                    db.Mime = i.ResultMimeType;
                    db.Path = Path.GetFileName(i.FinalPath);
                    ImageStore.UpdateImage(db);
                }

                return Json(result);
            }

            [HttpDelete]
            public ActionResult Delete(string id)
            {
                Guid imageId;
                if (Guid.TryParse(id, out imageId))
                {
                    var image = ImageStore.GetImage(imageId);
                    if (image == null)
                        return new HttpStatusCodeResult(HttpStatusCode.NoContent);
                    ImageStore.DeleteImage(image);
                    var info = new DirectoryInfo(Server.MapPath(string.Format("~/uploads"))).GetFiles(string.Format("{0}.*",image.Id));
                    if (info.Any())
                    {
                        info.ToList().ForEach(x=>x.Delete());
                        return new HttpStatusCodeResult(HttpStatusCode.OK);
                    }
                }
                return new HttpNotFoundResult();
            }

            [HttpDelete]
            public ActionResult DeleteByUser(string id)
            {
                    var images = ImageStore.DeleteByUser(id);
                    if (images == null || !images.Any())
                        return new HttpStatusCodeResult(HttpStatusCode.NoContent);
                    images.ForEach(image=>{
                    var info = new DirectoryInfo(Server.MapPath(string.Format("~/uploads"))).GetFiles(string.Format("{0}.*", image.Id));
                    if (info.Any())
                    {
                        info.ToList().ForEach(x => x.Delete());
                    }
                    });
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            [HttpGet]
            public ActionResult Test()
            {
                return View();
            }
            [HttpPost]
            public ActionResult GetByOwner(string id)
            {
                var images = ImageStore.GetByOwner(id);
                return Json(images.Select(x => new UploadFilesResult()
                {
                    Name = string.Format("/uploads/{0}", x.Path),
                    Delete_type = "DELETE",
                    Delete_url = Url.Action("Delete", new {id = Path.GetFileNameWithoutExtension(x.Path)}),
                    Size = x.Size.GetValueOrDefault(),
                    OriginalFilename = x.Name,
                    Type = x.Mime,
                    Url = "/uploads/" + x.Path,
                    Metadata = ConvertToPairs(x.Exif)
                }));
            }

            [HttpPost]
            public ActionResult TestPost(string owner)
            {
                Images.Initialize("http://localhost"+Url.Content("~"));
                var result = Request.RedirectToCdn("Test");
                result.Wait();
                var res = result.Result;
                return Json(res);
            }

            private string[] Ignore = new []{"CanFreeze","IsFrozen","DependencyObjectType","IsSealed","Dispatcher"};

            
            private Dictionary<string, string> ConvertToPairs<T>(T objectToConvert)
            {
                var result = new Dictionary<string, string>();
                var t = typeof(T).GetProperties();
                foreach (var p in t.Where(x=>Ignore.All(i=>x.Name!=i)))
                {
                    try
                    {
                        var value = p.GetValue(objectToConvert);
                        if (value != null && typeof (IList<string>).IsAssignableFrom(p.PropertyType))
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


