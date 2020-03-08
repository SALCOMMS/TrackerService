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

    public class ResponseData
    {
        public string Time { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public int Velocity { get; set; }
        public string Description { get; set; }
    }

    public class MinMax
    {
        public DateTime Time { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public int Velocity { get; set; }
        public string Address { get; set; }
    }

    public class RectData
    {
        public int id { get; set; }
        public string FeatureName { get; set; }
        public string strPoints { get; set; }
    }

    public class KmlData
    {
        public string groupid { get; set; }
        public string groupname { get; set; }
        public string points { get; set; }
        public DateTime regtime { get; set; }
    }
}