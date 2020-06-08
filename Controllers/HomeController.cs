using Microsoft.AspNet.Identity;
using RuuviTagApp.Models;
using RuuviTagApp.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Core.Mapping;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RuuviTagApp.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        public async Task<ActionResult> Index(string tagMac)
        {
            // tämän pitäisi luoda tietokanta
            //var tag = db.RuuviTagModels.Find(1);
            ViewBag.RenderRegisterModal = TempData["RenderRegisterModal"];
            ViewBag.LoginProvider = TempData["LoginProvider"];

            if (!string.IsNullOrWhiteSpace(tagMac))
            {
                if (TempData["ApiResponse"] != null)
                {
                    ViewBag.TagData = TempData["ApiResponse"];
                }
                else
                {
                    //ViewBag.TagData = await Task.Run(() => SimulateApiCallResponse(tagMac));
                    var mac = new MacAddressModel
                    {
                        MacAddress = tagMac
                    };
                    var context = new ValidationContext(mac);
                    var results = new List<ValidationResult>();
                    var isValid = Validator.TryValidateObject(mac, context, results, true);
                    if (isValid)
                    {
                        return await SearchTag(mac);
                    }
                    ViewBag.TagError = results;
                }
            }

            if (Request.IsAuthenticated)
            {
                string userID = User.Identity.GetUserId();
                List<RuuviTagModel> userTags = (from t in db.RuuviTagModels
                                            where t.UserId == userID
                                            select t).ToList();
                ViewBag.UserTagsDropdownList = new SelectList(userTags, "TagId", "TagMacAddress");
                ViewBag.UserTagsList = userTags;
            }
            return View();
        }

        public ActionResult Help()
        {
            ViewBag.Message = "Help page";

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SearchTag(MacAddressModel mac)
        {
            if (ModelState.IsValid)
            {
                TempData["ApiResponse"] = await Task.Run(() => SimulateApiCallResponse(mac.GetAddress()));
                return RedirectToAction("Index", "Home", new { tagMac = mac.GetAddress() });
            }
            return View("Index", mac);
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddTag(MacAddressModel mac, string userID)
        {
            if (ModelState.IsValid)
            {
                // Backend call
                // if fails return View("Index", mac);
                // else
                string macAddress = mac.GetAddress();
                // Check if this tag has been added by this user
                if (db.RuuviTagModels.Any(t => t.UserId == userID && t.TagMacAddress == macAddress))
                {
                    ViewBag.TagAlreadyExists = "Couldn't add this tag, since you have already added it!";
                    ViewBag.ShowAddTag = true;
                }
                else
                {
                    var newTag = db.RuuviTagModels.Add(new RuuviTagModel { UserId = userID, TagMacAddress = mac.GetAddress() });
                    db.SaveChanges();
                    // use data and tag to refresh view
                    return RedirectToAction("Index");
                }
            }
            ViewBag.ShowAddTag = true;
            return View("Index", mac);
        }

        public ActionResult GetUserTags()
        {
            throw new NotImplementedException();
        }

        public ActionResult AddTagList()
        {
            throw new NotImplementedException();
        }

        public ActionResult AddTagAlarm()
        {
            throw new NotImplementedException();
        }

        public ActionResult TagNav()
        {
            return View();
        }

        public async Task<JsonResult> GetTagData(string macAddress)
        {
            throw new NotImplementedException();
        }

        public List<SimulatedData> SimulateApiCallResponse(string mac)
        {
            Random rng = new Random();
            Thread.Sleep(rng.Next(500, 2500));
            List<SimulatedData> simDat = new List<SimulatedData>();
            DateTime now = DateTime.Now;
            for (int i = 0; i < 20; i++)
            {
                simDat.Add(new SimulatedData
                {
                    Id = mac,
                    Time = now - TimeSpan.FromHours(i),
                    Data = new TagData
                    {
                        Temperature = rng.Next(-20, 20) / 100D,
                        Pressure = rng.Next(100000, 100100),
                        Humidity = rng.Next(50000, 60000) / 1000D,
                        Acc_X = rng.Next(-10, 10) / 1000D,
                        Acc_Y = rng.Next(-10, 10) / 1000D,
                        Acc_Z = rng.Next(1000, 1100) / 1000D,
                        TXPower = rng.Next(0, 10),
                        Voltage = rng.Next(2000, 2500) / 1000D,
                        SeqNum = 200 - i
                    }
                });
            }
            return simDat;
        }

        public class SimulatedData
        {
            public string Id { get; set; }
            public TagData Data { get; set; }
            public DateTime Time { get; set; }
            //public string Coordinates { get; set; }
            //public string Gwmac { get; set; }
            //public int Rssi { get; set; }
            //public int Rssi_max { get; set; }
            //public int Rssi_min { get; set; }
        }

        public class TagData
        {
            public double Temperature { get; set; }
            public int Pressure { get; set; }
            public double Humidity { get; set; }
            public double Acc_X { get; set; }
            public double Acc_Y { get; set; }
            public double Acc_Z { get; set; }
            public int TXPower { get; set; }
            public double Voltage { get; set; }
            public int SeqNum { get; set; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult TemperatureChart()
        {
            return View();
        }

        public ActionResult HumidityChart()
        {
            return View();
        }

        public ActionResult AirpressureChart()
        {
            return View();
        }
    }
}