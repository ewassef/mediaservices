using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cache;
using Media.FusedConnection.Helpers;

namespace Media.FusedConnection.Code
{
    public static class ImageStore
    {
        private static readonly CollectionCache<Image> _images;

        static ImageStore()
        {
            _images = new CollectionCache<Image>(images => images.Id);
        }

        public static Image AddImage(Image image)
        {
            using (var entity = new MediaEntities())
            {
                entity.Images.Add(image);
                entity.SaveChanges();
                _images.ItemCache.Add(image);
                return image;
            }
        }

        public static Image UpdateImage(Image image)
        {
            using (var entity = new MediaEntities())
            {
                var db = entity.Images.FirstOrDefault(x=>x.Id == image.Id);
                db.Exif = image.Exif;
                db.Mime = image.Mime;
                db.Name = image.Name;
                db.Owner = image.Owner;
                db.Path = image.Path;
                db.Size = image.Size;
                entity.SaveChanges();
                _images.ItemCache.Add(image);
                return image;
            }
        }

        public static void DeleteImage(Image image)
        {
            using (var entity = new MediaEntities())
            {
                entity.Images.Attach(image);
                entity.Images.Remove(image);
                entity.SaveChanges();
                _images.Clear(image.Id);
            }
        }

        public static Image GetImage(Guid id)
        {
           return _images.ItemCache.Get(id, o =>
                {
                    using (var entity = new MediaEntities())
                    {
                        return entity.Images.FirstOrDefault(x => x.Id == id);
                    }
                });
        }


        public static List<Image> GetByOwner(string id)
        {
            using (var entity = new MediaEntities())
            {
                return entity.Images.Where(x => x.Owner == id).ToList();
            }
        }

        public static List<Image> DeleteByUser(object ownerId)
        {
            using (var entity = new MediaEntities())
            {
                var images = entity.Images.Where(x => x.Owner == ownerId).ToList();
                images.ForEach(image =>
                {
                    entity.Images.Remove(image);
                    _images.Clear(image.Id);
                });
                entity.SaveChanges();
                return images;
            }
            
        }
    }
}