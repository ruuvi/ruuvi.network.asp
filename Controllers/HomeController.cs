using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using RuuviTagApp.Models;
using RuuviTagApp.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RuuviTagApp.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        public async Task<ActionResult> Index(string tagMac)
        {
            ViewBag.ShowRegisterModal = TempData["ShowRegisterModal"];
            ViewBag.LoginProvider = TempData["LoginProvider"];
            ViewBag.ShowAddTag = TempData["ShowAddTag"];
            ViewBag.ShowTagSettings = TempData["ShowTagSettings"];
            ViewBag.TagErrors = TempData["TagErrorList"];
            ViewBag.GeneralError = TempData["GeneralError"];
            ViewBag.apiTime = TempData["apiTime"];
            ViewBag.apiTempData = TempData["apiTempData"];
            ViewBag.apiHumData = TempData["apiHumData"];
            ViewBag.apiPressData = TempData["apiPressData"];

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
                string userID = User.Identity.GetUserId();
                List<RuuviTagModel> userTags = await GetUserTagsAsync(userID);
                ViewBag.UserTagsList = userTags;
                ViewBag.UserHasEmail = db.Users.Find(userID).Email != null;

                // Add error message in case user hasn't added any tags
                if (userTags == null || !userTags.Any() || userTags.Count == 0)
                {
                    ViewBag.UserTagsListError = "Zero RuuviTags saved.";
                }
            }

            if (TempData["TagModel"] is AddTagModel addTag)
            {
                foreach (var e in ViewBag.TagErrors)
                {
                    ModelState.AddModelError(string.Empty, e);
                }
                return View(addTag);
            }
            else if (TempData["TagModel"] is RuuviTagModel tag)
            {
                foreach (var e in ViewBag.TagErrors)
                {
                    // When more thigs are added to settings "TagName" will need to be changed to 'string.Empty'.
                    ModelState.AddModelError("TagName", e);
                }
                return View(tag);
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

                string dataTimeList;
                string dataTempList;
                string dataHumList;
                string dataPressList;
                List<UnpackData> lstapiData = new List<UnpackData>();

                foreach (WhereOSApiRuuvi apiRuuviTag in apiResponse)
                {
                    UnpackData ApiRowData = new UnpackData();
                    UnpackRawData RawDataRow = new UnpackRawData();
                    RawDataRow.UnpackAllData(apiRuuviTag.data);
                    ApiRowData.Data = RawDataRow;
                    ApiRowData.Time = apiRuuviTag.time;
                    lstapiData.Add(ApiRowData);
                }

                dataTimeList = "'" + string.Join("','", lstapiData.Select(n => n.Time.TimeOfDay).ToList()) + "'";
                dataTempList = string.Join(",", lstapiData.Select(n => n.Data.temperature).ToList());
                dataHumList = string.Join(",", lstapiData.Select(n => n.Data.humidity).ToList());
                dataPressList = string.Join(",", lstapiData.Select(n => n.Data.pressure).ToList());

                TempData["apiTime"] = dataTimeList;
                TempData["apiTempData"] = dataTempList;
                TempData["apiHumData"] = dataHumList;
                TempData["apiPressData"] = dataPressList;


                // DECODE DATA HERE ?

                TempData["ApiResponse"] = lstapiData;
                return RedirectToAction("Index", "Home", new { tagMac = mac.GetAddress() });
            }
            return View("Index", mac);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddTag(AddTagModel tag)
        {
            if (ModelState.IsValid)
            {
                string userID = User.Identity.GetUserId();
                List<WhereOSApiRuuvi> apiResponse = await GetTagData(tag.GetAddress());
                bool userHasTag = await UserHasTagMacAsync(userID, tag.GetAddress());
                bool tagNameTaken = !string.IsNullOrEmpty(tag.AddTagName) && await TagNameTakenAsync(userID, tag.AddTagName);
                if (apiResponse.Count == 0 || userHasTag || tagNameTaken)
                {
                    List<string> tagErrors = new List<string>();
                    if (userHasTag)
                    {
                        tagErrors.Add("Couldn't add this tag, since you have already added it!");
                    }
                    if (tagNameTaken)
                    {
                        tagErrors.Add("Couldn't add this tag, since you have a tag with that name already!");
                    }
                    if (apiResponse.Count == 0)
                    {
                        tagErrors.Add("No data found, check RuuviTag ID. See Help -section for more information.");
                    }
                    TempData["TagErrorList"] = tagErrors;
                    TempData["ShowAddTag"] = true;
                    TempData["TagModel"] = tag;
                    return RedirectToAction("Index");
                }

                // DECODE DATA HERE ?

                var newTag = db.RuuviTagModels.Add(new RuuviTagModel { UserId = userID, TagMacAddress = tag.GetAddress(), TagName = tag.AddTagName });
                await db.SaveChangesAsync();
                // use data and tag to refresh view
                return RedirectToAction("Index");
            }

            TempData["TagErrorList"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["ShowAddTag"] = true;
            TempData["TagModel"] = tag;
            return RedirectToAction("Index");
        }

        private async Task<List<RuuviTagModel>> GetUserTagsAsync(string userID) => await (from t in db.RuuviTagModels
                                                                                          where t.UserId == userID
                                                                                          select t).ToListAsync();

        private async Task<bool> UserHasTagMacAsync(string userID, string mac) => await (from t in db.RuuviTagModels
                                                                                         where t.UserId == userID && t.TagMacAddress == mac
                                                                                         select t).FirstOrDefaultAsync() != null;

        private async Task<bool> UserHasTagIdAsync(string userID, int tagID) => await (from t in db.RuuviTagModels
                                                                                       where t.UserId == userID && t.TagId == tagID
                                                                                       select t).FirstOrDefaultAsync() != null;

        private async Task<bool> TagNameTakenAsync(string userID, string name) => await (from t in db.RuuviTagModels
                                                                                         where t.UserId == userID && t.TagName == name
                                                                                         select t).FirstOrDefaultAsync() != null;

        private async Task<TagAlertModel> TagHasAlertOfType(int tagID, int typeID) => await (from a in db.TagAlertModels
                                                                                             where a.TagId == tagID && a.AlertTypeId == typeID
                                                                                             select a).FirstOrDefaultAsync();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddTagAlert(AddAlertModel alert, int? tagID)
        {
            if (tagID == null)
            {
                // error no id
                return RedirectToAction("Index");
            }
            if (!await UserHasTagIdAsync(User.Identity.GetUserId(), (int)tagID))
            {
                // error tag is not users
                return RedirectToAction("Index");
            }
            List<TagAlertType> alarmTypes = new List<TagAlertType>();
            foreach (var type in await db.TagAlertTypes.ToListAsync())
            {
                type.TypeName = string.Join("", type.TypeName.Split('-'));
                alarmTypes.Add(type);
            }
            bool NoAlarmsAdded = true;
            foreach (PropertyInfo pi in alert.GetType().GetProperties())
            {
                if (pi.GetValue(alert) != null)
                {
                    int? alarmTypeId = alarmTypes
                        .Where(at => string.Equals(at.TypeName, pi.Name, StringComparison.OrdinalIgnoreCase))
                        .Select(at => at.AlertTypeId).FirstOrDefault();
                    if (alarmTypeId == null)
                    {
                        continue;
                    }
                    if (await TagHasAlertOfType((int)tagID, (int)alarmTypeId) is TagAlertModel oldAlert && oldAlert != null)
                    {
                        oldAlert.AlertLimit = (double)pi.GetValue(alert);
                        db.Entry(oldAlert).State = EntityState.Modified;
                    }
                    else
                    {
                        db.TagAlertModels.Add(new TagAlertModel
                        {
                            AlertTypeId = (int)alarmTypeId,
                            TagId = (int)tagID,
                            AlertLimit = (double)pi.GetValue(alert)
                        }); 
                    }
                    if (NoAlarmsAdded)
                    {
                        NoAlarmsAdded = false;
                    }
                }
            }
            if (NoAlarmsAdded)
            {
                // error no values for alarms
                return RedirectToAction("Index");
            }
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<JsonResult> GetAllAlerts(int? tagID)
        {
            List<object> res = new List<object>();
            if (tagID == null)
            {
                return Json(res, JsonRequestBehavior.AllowGet);
            }
            if (!await UserHasTagIdAsync(User.Identity.GetUserId(), (int)tagID))
            {
                return Json(res, JsonRequestBehavior.AllowGet);
            }
            foreach (var alert in await db.TagAlertModels.Where(t => t.TagId == tagID).ToListAsync())
            {
                res.Add(new { alert.AlertTypeId, alert.AlertLimit });
            }
            return Json(res, JsonRequestBehavior.AllowGet);
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
                TempData["GeneralError"] = "tagId was null.";
                return RedirectToAction("Index");
            }
            RuuviTagModel tag = await db.RuuviTagModels.FindAsync(tagID);
            if (tag == null)
            {
                TempData["GeneralError"] = "Tag not found.";
                return RedirectToAction("Index");
            }
            if (tag.UserId != User.Identity.GetUserId())
            {
                TempData["GeneralError"] = "You don't have access to that tag.";
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
                if (!string.IsNullOrWhiteSpace(tag.TagName) && await TagNameTakenAsync(User.Identity.GetUserId(), tag.TagName))
                {
                    TempData["TagErrorList"] = new List<string> { "Couldn't change tag name, since you already have a tag with that name!" };
                    TempData["ShowTagSettings"] = true;
                    TempData["TagModel"] = tag;
                    return RedirectToAction("Index");
                }
                db.Entry(tag).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            TempData["TagErrorList"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["ShowTagSettings"] = true;
            TempData["TagModel"] = tag;
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpDelete, ActionName("_TagSettingsModal")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteTag(int tagid)
        {
            RuuviTagModel tag = await db.RuuviTagModels.FindAsync(tagid);
            if (User.Identity.GetUserId() != tag.UserId)
            {
                TempData["GeneralError"] = "You don't have access to that tag.";
                return RedirectToAction("Index");
            }
            db.RuuviTagModels.Remove(tag);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public ActionResult TagAlerts()
        {
            return View();
        }

        [Authorize]
        public async Task<ActionResult> Groups()
        {
            string userID = User.Identity.GetUserId();
            var userTags = new List<SelectListItem>();
            var tags = new Dictionary<int, RuuviTagModel>();
            foreach(var tag in await GetUserTagsAsync(userID))
            {
                userTags.Add(new SelectListItem { Value = tag.TagId.ToString(), Text = tag.TagName ?? tag.TagMacAddress });
                tags.Add(tag.TagId, tag);
            }
            ViewBag.UserTagDropdownList = new SelectList(userTags, "Value", "Text");
            ViewBag.UsersTags = tags;
            List<UserTagListModel> userGroups = await db.UserTagListModels.Where(g => g.UserId == userID).Include(r => r.TagListRowModels).ToListAsync();
            return View(userGroups);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> AddUserTagList(NewTagListModel list)
        {
            if (ModelState.IsValid)
            {
                string userID = User.Identity.GetUserId();
                var userLists = await db.UserTagListModels.Where(l => l.UserId == userID).ToListAsync();
                var userTags = await GetUserTagsAsync(userID);
                if (userLists.Select(l => l.ListName).Contains(list.ListName))
                {
                    // TempData error list = You already have a list with that name.
                    return RedirectToAction("Groups");
                }
                else if (string.IsNullOrWhiteSpace(list.ListName))
                {
                    // TempData error list = name required.
                    return RedirectToAction("Groups");
                }
                var newList = db.UserTagListModels.Add(new UserTagListModel { ListName = list.ListName, UserId = userID });
                if (!string.IsNullOrWhiteSpace(list.IdsAsString))
                {
                    string[] ids = list.IdsAsString.Trim(';').Split(';');
                    foreach (string idstring in ids)
                    {
                        if (int.TryParse(idstring, out int tagId) && userTags.Any(t => t.TagId == tagId))
                        {
                            db.TagListRowModels.Add(new TagListRowModel { ListId = newList.UserTagListId, TagId = tagId });
                        }
                    }
                }
                await db.SaveChangesAsync();
                return RedirectToAction("Groups");
            }
            // TempData error list = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return RedirectToAction("Groups");
        }
    }
}