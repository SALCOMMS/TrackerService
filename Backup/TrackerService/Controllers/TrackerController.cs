using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Device.Location;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using TrackerService.Models;

namespace TrackerService.Controllers
{
    public class TrackerController : Controller
    {
        private TrackerContext db = new TrackerContext();

        public string Index()
        {
            return "Tracker API Service is fired up.";
        }

        //[AllowCrossSiteJson]
        public JsonResult GetDispatchTrackerInfo(string systemNumber, DateTime? startDateTime, DateTime? enDateTime)
        {
            var trackingInformation = new List<TrackerInfo>();

            try
            {
                SqlParameter[] sqlParameters = 
                {
                    new SqlParameter("@StartDateTime", SqlDbType.DateTime) { Value = startDateTime.Value },
                    new SqlParameter("@EndDateTime", SqlDbType.DateTime) { Value = enDateTime.Value }
                };

                string strSQL = "SELECT [Time], [Longitude], [Latitude], [Velocity] " +
                                "FROM " + systemNumber + " WHERE Time BETWEEN @StartDateTime AND @EndDateTime";

                trackingInformation = (db.Database.SqlQuery<TrackerInfo>(strSQL, sqlParameters)).ToList();


                foreach (var trackingInfo in trackingInformation)
                {
                    if (trackingInfo != null)
                    {
                        trackingInfo.Address = "";
                    }
                }

                return Json(trackingInformation, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                return Json(trackingInformation, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetLastTrackerDetail(string systemNumber)
        {
            TrackerInfo trackingInformation = new TrackerInfo();
            try
            {
                var strSQL = "SELECT TOP 1 [Time], [Longitude], [Latitude], [Velocity] FROM " + systemNumber + " " +
                             "WHERE [Time] <= '" + DateTime.Now.AddHours(5) + "' ORDER BY Time DESC";
                trackingInformation = db.Database.SqlQuery<TrackerInfo>(strSQL).FirstOrDefault();

                if (trackingInformation != null)
                {
                    string latitude = trackingInformation.Latitude.ToString();
                    string longitude = trackingInformation.Longitude.ToString();
                    trackingInformation.Time.AddHours(-5);
                    trackingInformation.Address = "";
                }

                return Json(trackingInformation, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                return Json(trackingInformation, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetDepartureTimeAtPlant(string systemNumber, decimal longitude, decimal latitude, DateTime date, DateTime standard)
        {
            TrackerInfo trackingInformation = new TrackerInfo();
            try
            {
                string strSQL = "SELECT Top 1 Time, Longitude, Latitude, Velocity, Distance " +
                                "From (SELECT Time, Longitude, Latitude, Velocity, (6371.393 * " +
                                "acos (cos ( radians(" + latitude + ") ) * cos( radians( Latitude ) ) * " +
                                "cos( radians( Longitude ) - radians(" + longitude + ") ) + " +
                                "sin ( radians(" + latitude + ") ) * sin( radians( Latitude ) ) ) ) AS distance " +
                                "FROM " + systemNumber + ") X Where Distance <= 0.5 And Time < '" + date + "' And Time > '" + standard + "' " +
                                "Order By Time Desc";
                trackingInformation = db.Database.SqlQuery<TrackerInfo>(strSQL).FirstOrDefault();

                return Json(trackingInformation, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                return Json(trackingInformation, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetReturnArrivalTimeAtPlant(string systemNumber, decimal longitude, decimal latitude, DateTime date)
        {
            TrackerInfo trackingInformation = new TrackerInfo();
            try
            {
                string strSQL = "SELECT Top 1 Time, Longitude, Latitude, Velocity, Distance " +
                                "From (SELECT Time, Longitude, Latitude, Velocity, (6371.393 * " +
                                "acos (cos ( radians(" + latitude + ") ) * cos( radians( Latitude ) ) * " +
                                "cos( radians( Longitude ) - radians(" + longitude + ") ) + " +
                                "sin ( radians(" + latitude + ") ) * sin( radians( Latitude ) ) ) ) AS distance " +
                                "FROM " + systemNumber + ") X Where Distance <= 0.5 And Time > '" + date + "' " +
                                "Order By Time Asc";
                trackingInformation = db.Database.SqlQuery<TrackerInfo>(strSQL).FirstOrDefault();

                return Json(trackingInformation, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                return Json(trackingInformation, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetMinMaxOld(string systemNumber, decimal longitude, decimal latitude, DateTime startDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParameters = 
                {
                    new SqlParameter("@systemNumber", SqlDbType.NVarChar) { Value = systemNumber },
                    new SqlParameter("@longitude", SqlDbType.Decimal) { Value = longitude },
                    new SqlParameter("@latitude", SqlDbType.Decimal) { Value = latitude },
                    new SqlParameter("@startDate", SqlDbType.DateTime) { Value = startDate },
                    new SqlParameter("@endDate", SqlDbType.DateTime) { Value = endDate }
                };

                string strSQL = "SELECT Time, Longitude, Latitude, Velocity, Distance From " +
                                "(SELECT Time, Longitude, Latitude, Velocity, " +
                                "(6371.393 * acos (cos ( radians(" + latitude + ") ) * " +
                                "cos( radians( Latitude ) ) * cos( radians( Longitude ) - " +
                                "radians(" + longitude + ") ) + sin ( radians(" + latitude + ") ) * " +
                                "sin( radians( Latitude ) ) ) ) AS distance FROM " + systemNumber + ") X " +
                                "Where Distance <= 0.5 And Time Between @startDate And @endDate";

                var trackingInformation = (db.Database.SqlQuery<TrackerInfo>(strSQL, sqlParameters)).ToList();

                var minMax = new List<MinMax>();

                if (trackingInformation.Count > 0)
                {
                    var arrivalTime = trackingInformation.Min(i => i.Time);
                    var departureTime = trackingInformation.Max(i => i.Time);

                    TrackerInfo arrival = trackingInformation.FirstOrDefault(i => i.Time == arrivalTime);
                    TrackerInfo departure = trackingInformation.FirstOrDefault(i => i.Time == departureTime);

                    if (arrival != null)
                    {
                        minMax.Add(
                            new MinMax()
                            {
                                Time = arrival.Time,
                                Longitude = arrival.Longitude,
                                Latitude = arrival.Latitude,
                                Velocity = arrival.Velocity,
                                Address = ""
                            });
                    }

                    if (departure != null)
                    {
                        minMax.Add(
                            new MinMax()
                            {
                                Time = departure.Time,
                                Longitude = departure.Longitude,
                                Latitude = departure.Latitude,
                                Velocity = departure.Velocity,
                                Address = String.Empty
                            });
                    }
                }

                return Json(minMax, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                TrackerInfo trackerDetail = new TrackerInfo();

                return Json(trackerDetail, JsonRequestBehavior.AllowGet);
            }
        }
        
        public JsonResult GetMinMax(string systemNumber, decimal longitude, decimal latitude, DateTime startDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParameters = 
                {
                    new SqlParameter("@systemNumber", SqlDbType.NVarChar) { Value = systemNumber },
                    new SqlParameter("@longitude", SqlDbType.Decimal) { Value = longitude },
                    new SqlParameter("@latitude", SqlDbType.Decimal) { Value = latitude },
                    new SqlParameter("@startDate", SqlDbType.DateTime) { Value = startDate },
                    new SqlParameter("@endDate", SqlDbType.DateTime) { Value = endDate }
                };

                string strSQL = "IF EXISTS (SELECT name FROM sysobjects WHERE type='U' AND name='" + systemNumber + "') " +
                                "SELECT Time, Longitude, Latitude, Velocity FROM " + systemNumber + " " +
                                "WHERE Time BETWEEN @startDate AND @endDate AND Locate=1 ORDER BY Time ASC";

                var trackingInformation = (db.Database.SqlQuery<TrackerInfo>(strSQL, sqlParameters)).OrderBy(t => t.Time).ToList();
                var trackingInfo = new List<TrackerInfo>();
                
                if (trackingInformation.Count > 0)
                {
                    TrackerInfo min = new TrackerInfo();
                    TrackerInfo max = new TrackerInfo();
                    
                    foreach (var info in trackingInformation)
                    {
                        if (GetDistanceTo(Convert.ToDouble(latitude), Convert.ToDouble(longitude), Convert.ToDouble(info.Latitude), Convert.ToDouble(info.Longitude)) <= 500 && info.Time > startDate && info.Time < endDate)
                        {
                            min = info;
                            break;
                        }
                    }

                    var updatedTrackingInformation = trackingInformation.Where(t => t.Time > min.Time).OrderBy(t => t.Time).ToList();
                    foreach (var info in updatedTrackingInformation)
                    {
                        if (GetDistanceTo(Convert.ToDouble(latitude), Convert.ToDouble(longitude), Convert.ToDouble(info.Latitude), Convert.ToDouble(info.Longitude)) <= 500 && info.Time > min.Time)
                        {
                            max = info;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (!min.ToString().Contains("0001"))
                    {
                        trackingInfo.Add(min);
                    }

                    if (!max.ToString().Contains("0001"))
                    {
                        trackingInfo.Add(max);
                    }
                }

                return Json(trackingInfo, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                TrackerInfo trackerDetail = new TrackerInfo();
                return Json(trackerDetail, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetMinMaxReverse(string systemNumber, decimal longitude, decimal latitude, DateTime startDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParameters = 
                {
                    new SqlParameter("@systemNumber", SqlDbType.NVarChar) { Value = systemNumber },
                    new SqlParameter("@longitude", SqlDbType.Decimal) { Value = longitude },
                    new SqlParameter("@latitude", SqlDbType.Decimal) { Value = latitude },
                    new SqlParameter("@startDate", SqlDbType.DateTime) { Value = startDate },
                    new SqlParameter("@endDate", SqlDbType.DateTime) { Value = endDate }
                };

                string strSQL = "IF EXISTS (SELECT name FROM sysobjects WHERE type='U' AND name='" + systemNumber + "') " +
                                "SELECT Time, Longitude, Latitude, Velocity FROM " + systemNumber + " " +
                                "WHERE Time BETWEEN @startDate AND @endDate AND Locate=1 ORDER BY Time ASC";

                var trackingInformation = (db.Database.SqlQuery<TrackerInfo>(strSQL, sqlParameters)).OrderByDescending(t => t.Time).ToList();
                var trackingInfo = new List<TrackerInfo>();

                if (trackingInformation.Count > 0)
                {
                    TrackerInfo min = new TrackerInfo();
                    TrackerInfo max = trackingInformation.First();

                    foreach (var info in trackingInformation)
                    {
                        if (GetDistanceTo(Convert.ToDouble(latitude), Convert.ToDouble(longitude), Convert.ToDouble(info.Latitude), Convert.ToDouble(info.Longitude)) <= 500)
                        {
                            min = info;
                        }
                        else
                        {
                            break;
                        }
                    }
                    trackingInfo.Add(min);
                    trackingInfo.Add(max);
                }

                return Json(trackingInfo, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                TrackerInfo trackerDetail = new TrackerInfo();
                return Json(trackerDetail, JsonRequestBehavior.AllowGet);
            }
        }

        public double GetDistanceTo(double latA, double longA, double latB, double longB)
        {
            double distance = 5000.0;
            if (latA >= -90.0 && latA <= 90.0 && longA >= -90.0 && longA <= 90.0 && latB >= -90.0 && latB <= 90.0 && longB >= -90.0 && longB <= 90.0)
            {
                var locA = new GeoCoordinate(latA, longA);
                var locB = new GeoCoordinate(latB, longB);
                distance = locA.GetDistanceTo(locB);
            }

            return distance;
        }

        //private string GetAddress(string latitude, string longitude)
        //{
        //    string locationName = "";
        //    string url = string.Format("http://maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=false&key=AIzaSyCGNVa3gq_2FB1iKRyyxX3WX6vy-ZoWs6g",
        //        latitude, longitude);
        //    XElement xml = XElement.Load(url);
        //    if (xml.Element("status").Value == "OK")
        //    {
        //        locationName = string.Format("{0}", xml.Element("result").Element("formatted_address").Value);
        //    }
        //    return locationName;
        //}

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}