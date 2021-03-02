using System;
using System.Collections.Generic;
using System.Text;

namespace CRUD_user
{
    public class Client
    {
        public string id { get; set; }
        public string NameClient { get; set; }
        public string CnpjClient { get; set; }
        public byte[] ImageClient { get; set; }
        public bool ActiveClient { get; set; }
        public string userType { get; set; }
    }
}
