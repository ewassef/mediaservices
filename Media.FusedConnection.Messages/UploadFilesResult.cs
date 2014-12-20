using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.FusedConnection.Messages
{
    public class UploadFilesResult
    {
        public string Name { get; set; }

        public string Delete_type
        {
            get;
            set;
        }

        public string Delete_url
        {
            get;
            set;
        }

        public long Size
        {
            get;
            set;
        }

        public string Type
        {
            get;
            set;
        }

        public string Url
        {
            get;
            set;
        }

        public Dictionary<string, string> Metadata
        {
            get;
            set;
        }

        public string OriginalFilename
        {
            get;
            set;
        }
    }
}
