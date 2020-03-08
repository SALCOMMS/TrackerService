using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Device.Location;
using System.Linq;
using System.Web.Mvc;
using TrackerService.Models;

namespace TrackerService.Controllers
{
    public class TrackerController : Controller
    {
        private TrackerContext _db = new TrackerContext();
        private CxgpsBaseInfoContext _cxgpsBaseInfoDb = new CxgpsBaseInfoContext();

        public string Index()
        {
            return "New Tracker Service is fired up.";
        }
        
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

                trackingInformation = (_db.Database.SqlQuery<TrackerInfo>(strSQL, sqlParameters)).ToList();


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
                trackingInformation = _db.Database.SqlQuery<TrackerInfo>(strSQL).FirstOrDefault();

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

        public JsonResult GetMinMax(string systemNumber, decimal longitude, decimal latitude, decimal distance, DateTime startDate, DateTime endDate)
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

                var trackingInformation = (_db.Database.SqlQuery<TrackerInfo>(strSQL, sqlParameters)).OrderBy(t => t.Time).ToList();
                var trackingInfo = new List<TrackerInfo>();
                var responseData = new List<ResponseData>();

                if (trackingInformation.Count > 0)
                {
                    TrackerInfo min = new TrackerInfo();
                    TrackerInfo minDriveOut = new TrackerInfo();
                    TrackerInfo max = new TrackerInfo();

                    //Get min time within bound
                    foreach (var info in trackingInformation)
                    {
                        if (GetDistanceTo(Convert.ToDouble(latitude), Convert.ToDouble(longitude), Convert.ToDouble(info.Latitude), Convert.ToDouble(info.Longitude)) <= Convert.ToDouble(distance) && info.Time > startDate && info.Time < endDate)
                        {
                            min = info;
                            break;
                        }
                    }

                    // Get drive in time outside bound
                    if (min != null && !min.Time.ToString().Contains("0001"))
                    {
                        SqlParameter[] minSqlParameters =
                        {
                            new SqlParameter("@minStartDate", SqlDbType.DateTime) { Value = min.Time.AddDays(-3) },
                            new SqlParameter("@minEndDate", SqlDbType.DateTime) { Value = min.Time }
                        };

                        string strMinSQL = "IF EXISTS (SELECT name FROM sysobjects WHERE type='U' AND name='" + systemNumber + "') " +
                                    "SELECT Time, Longitude, Latitude, Velocity FROM " + systemNumber + " " +
                                    "WHERE Time BETWEEN @minStartDate AND @minEndDate AND Locate=1 ORDER BY Time ASC";

                        var minTrackingInformation = (_db.Database.SqlQuery<TrackerInfo>(strMinSQL, minSqlParameters)).OrderByDescending(t => t.Time).ToList();

                        foreach (var item in minTrackingInformation)
                        {
                            if (GetDistanceTo(Convert.ToDouble(latitude), Convert.ToDouble(longitude), Convert.ToDouble(item.Latitude), Convert.ToDouble(item.Longitude)) <= Convert.ToDouble(distance))
                            {
                                min = item;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    //Get first drive out time
                    if (min != null && !min.Time.ToString().Contains("0001"))
                    {
                        var updatedTrackingInformation = trackingInformation.Where(t => t.Time > min.Time).OrderBy(t => t.Time).ToList();
                        if(updatedTrackingInformation.Count > 0)
                        {
                            foreach (var data in updatedTrackingInformation)
                            {
                                if (GetDistanceTo(Convert.ToDouble(latitude), Convert.ToDouble(longitude), Convert.ToDouble(data.Latitude), Convert.ToDouble(data.Longitude)) <= Convert.ToDouble(distance))
                                {
                                    minDriveOut = data;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }

                    //Get max time within bound
                    foreach (var info in trackingInformation.OrderByDescending(t => t.Time))
                    {
                        if (GetDistanceTo(Convert.ToDouble(latitude), Convert.ToDouble(longitude), Convert.ToDouble(info.Latitude), Convert.ToDouble(info.Longitude)) <= Convert.ToDouble(distance) && info.Time >= startDate && info.Time <= endDate)
                        {
                            max = info;
                            break;
                        }
                    }

                    // Get drive out time outside bound
                    if (max != null && !max.Time.ToString().Contains("0001"))
                    {
                        SqlParameter[] maxSqlParameters =
                        {
                            new SqlParameter("@maxStartDate", SqlDbType.DateTime) { Value = max.Time },
                            new SqlParameter("@maxEndDate", SqlDbType.DateTime) { Value = max.Time.AddDays(3) }
                        };

                        string strMaxSQL = "IF EXISTS (SELECT name FROM sysobjects WHERE type='U' AND name='" + systemNumber + "') " +
                                    "SELECT Time, Longitude, Latitude, Velocity FROM " + systemNumber + " " +
                                    "WHERE Time BETWEEN @maxStartDate AND @maxEndDate AND Locate=1 ORDER BY Time ASC";

                        var maxTrackingInformation = (_db.Database.SqlQuery<TrackerInfo>(strMaxSQL, maxSqlParameters)).OrderBy(t => t.Time).ToList();

                        foreach (var item in maxTrackingInformation)
                        {
                            if (GetDistanceTo(Convert.ToDouble(latitude), Convert.ToDouble(longitude), Convert.ToDouble(item.Latitude), Convert.ToDouble(item.Longitude)) <= Convert.ToDouble(distance))
                            {
                                max = item;
                            } else {
                                break;
                            }
                        }
                    }

                    if (min != null && !min.Time.ToString().Contains("0001"))
                    {
                        responseData.Add(new ResponseData()
                        {
                            Time = min.Time.ToString(),
                            Longitude = min.Longitude,
                            Latitude = min.Latitude,
                            Velocity = min.Velocity,
                            Description = "Min"
                        });
                    }

                    if (minDriveOut != null && !minDriveOut.Time.ToString().Contains("0001"))
                    {
                        responseData.Add(new ResponseData()
                        {
                            Time = minDriveOut.Time.ToString(),
                            Longitude = minDriveOut.Longitude,
                            Latitude = minDriveOut.Latitude,
                            Velocity = minDriveOut.Velocity,
                            Description = "MinDriveOut"
                        });
                    }

                    if (max != null && !max.Time.ToString().Contains("0001"))
                    {
                        responseData.Add(new ResponseData()
                        {
                            Time = max.Time.ToString(),
                            Longitude = max.Longitude,
                            Latitude = max.Latitude,
                            Velocity = max.Velocity,
                            Description = "Max"
                        });
                    }
                }

                return Json(responseData, JsonRequestBehavior.AllowGet);
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

        public JsonResult GetRectTableData()
        {
            try
            {
                string strSQL = "SELECT id, FeatureName, strPoints FROM RectTable";

                var rectDataList = (_cxgpsBaseInfoDb.Database.SqlQuery<RectData>(strSQL)).ToList();
                return Json(rectDataList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                var rectTableData = new List<RectData>();
                return Json(rectTableData, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetPlantsFromRectTable()
        {
            try
            {
                string strSQL = "Select * From RectTable Where FeatureName IN ('IK1', 'BE2', 'LA2', 'SI6', 'EK2', 'ON1', 'KD1', 'JS1', " + 
                    "'IM3', 'EN2', 'HQ1',  'OD1', 'KD1', 'IB2', 'AB1', 'EK2', 'SE5')";

                var rectDataList = (_cxgpsBaseInfoDb.Database.SqlQuery<RectData>(strSQL)).ToList();
                return Json(rectDataList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                var rectTableData = new List<RectData>();
                return Json(rectTableData, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetKmlToTableData()
        {
            try
            {
                string strSQL = "Select groupid, groupname, points, regtime From KmlToTable";

                var kmlData = (_cxgpsBaseInfoDb.Database.SqlQuery<KmlData>(strSQL)).ToList();
                return Json(kmlData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorLog.LogException(ex);
                var kmlData = new List<KmlData>();
                return Json(kmlData, JsonRequestBehavior.AllowGet);
            }
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
            _db.Dispose();
            _cxgpsBaseInfoDb.Dispose();
            base.Dispose(disposing);
        }
    }
}