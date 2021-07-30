using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Models
{
    public class Servers
    {

        public string ClusterName { get; set; }
        public string ServerName { get; set; }
        public string RconIp { get; set; }
        public string RconPass { get; set; }
        public int RconPort { get; set; }
    }
}
