using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrackerService.Models
{
    public class TrackerInfo
    {
        public DateTime Time { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public int Velocity { get; set; }
        public string Address { get; set; }
    }

    public class MinMax
    {
        public DateTime Time { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public int Velocity { get; set; }
        public string Address { get; set; }
    }
}