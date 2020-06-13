using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using RuuviTagApp.Models;
using RuuviTagApp.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Linq;
using System.Net.Http;
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
            ViewBag.ShowAddTag = TempData["ShowAddTag"];
            ViewBag.MacErrors = TempData["MacErrorList"];

            if (!string.IsNullOrWhiteSpace(tagMac) && !Request.IsAuthenticated)
            {
                if (TempData["ApiResponse"] != null)
                {
                    ViewBag.TagData = TempData["ApiResponse"];
                }
                else
                {
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
                    foreach (var e in results)
                    {
                        ModelState.AddModelError("MacAddress", e.ErrorMessage);
                    }
                    return View(mac);
                }
            }
            else if (Request.IsAuthenticated)
            {
                List<RuuviTagModel> userTags = await GetUserTagsAsync(User.Identity.GetUserId());
                ViewBag.UserTagsList = userTags;

                // Add error message in case user hasn't added any tags
                if (userTags == null || !userTags.Any() || userTags.Count == 0)
                {
                    ViewBag.UserTagsListError = "Zero RuuviTags saved.";
                }
            }

            if (TempData["MacAddressModel"] is MacAddressModel macAddress)
            {
                foreach (var e in ViewBag.MacErrors)
                {
                    ModelState.AddModelError("MacAddress", e);
                }
                return View(macAddress);
            }
            return View();
        }

        public ActionResult Help()
        {
            ViewBag.Message = "Help";

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SearchTag(MacAddressModel mac)
        {
            if (ModelState.IsValid)
            {
                List<WhereOSApiRuuvi> apiResponse = await GetTagData(mac.GetAddress());
                if (apiResponse.Count == 0)
                {
                    ModelState.AddModelError("MacAddress", "No data found, check RuuviTag ID. See Help -section for more information.");
                    return View("Index", mac);
                }

                // DECODE DATA HERE ?
                
                TempData["ApiResponse"] = apiResponse;
                return RedirectToAction("Index", "Home", new { tagMac = mac.GetAddress() });
            }
            return View("Index", mac);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddTag(MacAddressModel mac)
        {
            if (ModelState.IsValid)
            {
                string userID = User.Identity.GetUserId();
                List<WhereOSApiRuuvi> apiResponse = await GetTagData(mac.GetAddress());
                bool userHasTag = await UserHasTag(userID, mac.GetAddress());
                if (apiResponse.Count == 0 || userHasTag)
                {
                    List<string> tagErrors = new List<string>
                    {
                        userHasTag ? "Couldn't add this tag, since you have already added it!" : "No data found, check RuuviTag ID. See Help -section for more information."
                    };
                    TempData["MacErrorList"] = tagErrors;
                    TempData["ShowAddTag"] = true;
                    TempData["MacAddressModel"] = mac;
                    return RedirectToAction("Index");
                }

                // DECODE DATA HERE ?

                var newTag = db.RuuviTagModels.Add(new RuuviTagModel { UserId = userID, TagMacAddress = mac.GetAddress() });
                await db.SaveChangesAsync();
                // use data and tag to refresh view
                return RedirectToAction("Index");
            }
            TempData["MacErrorList"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["ShowAddTag"] = true;
            TempData["MacAddressModel"] = mac;
            return RedirectToAction("Index");
        }

        private async Task<List<RuuviTagModel>> GetUserTagsAsync(string userID) => await (from t in db.RuuviTagModels
                                                                                          where t.UserId == userID
                                                                                          select t).ToListAsync();

        private async Task<bool> UserHasTag(string userID, string mac) => await (from t in db.RuuviTagModels
                                                                                 where t.UserId == userID && t.TagMacAddress == mac
                                                                                 select t).FirstOrDefaultAsync() != null;
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

        private async Task<List<WhereOSApiRuuvi>> GetTagData(string macAddress)
        {
            string url = ApiHelper.ApiClient.BaseAddress + macAddress;
            using (HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(url))
            {
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<WhereOSApiRuuvi>>(json);

                    return data;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
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

        public ActionResult AppSettings()
        {
            return View();
        }

        [Authorize]
        public async Task<ActionResult> _TagSettingsModal(int? tagID)
        {
            if (tagID == null)
            {
                // error in call
                return RedirectToAction("Index");
            }
            RuuviTagModel tag = await db.RuuviTagModels.FindAsync(tagID);
            if (tag == null)
            {
                // tag not found error
                return RedirectToAction("Index");
            }
            string userID = User.Identity.GetUserId();
            if (tag.UserId != userID)
            {
                // tag is not users tag error
                return RedirectToAction("Index");
            }
            return PartialView(tag);
        }
        
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> _TagSettingsModal([Bind(Include = "TagId,TagMacAddress,TagActive,TagName,UserId")] RuuviTagModel tag)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tag).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            // some error
            return RedirectToAction("Index");
        }


        public ActionResult TagAlerts()
        {
            return View();
        }
    }
}