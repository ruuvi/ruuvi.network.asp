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
    [HandleError]
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

            // Get the time when site was loaded (when api was called) in milliseconds
            long currentTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            ViewBag.apiCallTime = currentTimeMs;
            string userID = string.Empty;

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
                userID = User.Identity.GetUserId();
                List<RuuviTagModel> userTags = await GetUserTagsAsync(userID);
                ViewBag.UserTagsList = userTags;
                ViewBag.UserGroups = await GetUsersGroupsAsync(userID);
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
                ViewBag.AvailableGroups = await GetAvailableGroupsForTag(tag.TagId, userID);
                ViewBag.Id = tag.TagId;
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
                dataTempList = "'" + string.Join("','", lstapiData.Select(n => n.Data.temperature).ToList()) + "'";
                dataHumList = "'" + string.Join("','", lstapiData.Select(n => n.Data.humidity).ToList()) + "'";
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

        [Authorize]
        public async Task<ActionResult> LoggedInApiData(int? tagID, int? interval)
        {
            if (tagID == null)
            {
                TempData["GeneralError"] = "Something went wrong while fetching data. Please try again";
                return RedirectToAction("Index");
            }
            RuuviTagModel tag = db.RuuviTagModels.Find(tagID);
            if (tag == null)
            {
                TempData["GeneralError"] = "Something went wrong while fetching data. Please try again";
                return RedirectToAction("Index");
            }
            if (tag.UserId != User.Identity.GetUserId())
            {
                TempData["GeneralError"] = "You do not have permission to do that.";
                return RedirectToAction("Index");
            }
            List<WhereOSApiRuuvi> apiResponse;
            if (interval != null)
            {
                apiResponse = await GetTagData(tag.TagMacAddress, (int)interval);
            }
            else
            {
                apiResponse = await GetTagData(tag.TagMacAddress); 
            }

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

            
            return Json(lstapiData, JsonRequestBehavior.AllowGet);
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

        private async Task<bool> UserHasGroupIdAsync(string userID, int listsID) => await (from l in db.UserTagListModels
                                                                                           where l.UserId == userID && l.UserTagListId == listsID
                                                                                           select l).FirstOrDefaultAsync() != null;

        private async Task<bool> TagIsInListAsync(int tagsId, int listsId) => await (from r in db.TagListRowModels
                                                                                where r.TagId == tagsId && r.ListId == listsId
                                                                                select r).FirstOrDefaultAsync() != null;

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddTagAlert(AddAlertModel alert, int? tagID)
        {
            if (tagID == null)
            {
                TempData["GeneralError"] = "Could not add alert due to missing ID. Please try again.";
                return RedirectToAction("Index");
            }
            if (!await UserHasTagIdAsync(User.Identity.GetUserId(), (int)tagID))
            {
                TempData["GeneralError"] = "You do not have permission to do that.";
                return RedirectToAction("Index");
            }
            List<TagAlertType> alarmTypes = await db.TagAlertTypes.ToListAsync();
            bool NoAlertsAdded = true;
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
                    if (NoAlertsAdded)
                    {
                        NoAlertsAdded = false;
                    }
                }
            }
            if (NoAlertsAdded)
            {
                TempData["GeneralError"] = "No alerts were added.";
                return RedirectToAction("Index");
            }
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<ActionResult> _GetAllAlerts(int? tagID)
        {
            if (tagID == null)
            {
                TempData["GeneralError"] = "Unable to get alerts due to missing ID. Please try again.";
                return RedirectToAction("Index");
            }
            if (!await UserHasTagIdAsync(User.Identity.GetUserId(), (int)tagID))
            {
                TempData["GeneralError"] = "You do not have permission to do that.";
                return RedirectToAction("Index");
            }
            var alerts = await db.TagAlertModels.Where(t => t.TagId == tagID).ToListAsync();
            return PartialView(alerts);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> RemoveAlert(int? alertID)
        {
            if (alertID == null)
            {
                TempData["GeneralError"] = "Unable to remove alert due to missig ID. Please try again.";
                return RedirectToAction("Index");
            }
            TagAlertModel alert = db.TagAlertModels.Find(alertID);
            if (!string.Equals(User.Identity.GetUserId(), alert.RuuviTagModel.UserId))
            {
                TempData["GeneralError"] = "You do not have permission to do that.";
                return RedirectToAction("Index");
            }
            db.TagAlertModels.Remove(alert);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
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

        private async Task<List<WhereOSApiRuuvi>> GetTagData(string macAddress, int interval)
        {
            string[] intervals = { "5m", "15m", "30m", "1h", "2h" };
            string url = ApiHelper.ApiClient.BaseAddress + macAddress + "?p_aggregation=";
            if (intervals.ElementAtOrDefault(interval) is string val)
            {
                url += val;
            }
            else
            {
                url += intervals[1];
            }
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

        [Authorize]
        public ActionResult AppSettings()
        {
            string userID = User.Identity.GetUserId();
            ViewBag.UserHasEmail = db.Users.Find(userID).Email != null;
            return View();
        }

        [Authorize]
        [HttpPost, ActionName("AppSettings")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteUser()
        {
            string userID = User.Identity.GetUserId();
            ApplicationUser user = db.Users.Find(userID);
            if (user == null)
            {
                // something went wrong
                return RedirectToAction("AppSettings");
            }
            foreach (UserTagListModel userList in await db.UserTagListModels.Where(g => g.UserId == userID).ToListAsync())
            {
                db.UserTagListModels.Remove(userList);
            }
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            var acc = DependencyResolver.Current.GetService<AccountController>();
            acc.ControllerContext = new ControllerContext(Request.RequestContext, acc);
            return acc.LogOff();
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
            string userID = User.Identity.GetUserId();
            if (tag.UserId != userID)
            {
                TempData["GeneralError"] = "You don't have access to that tag.";
                return RedirectToAction("Index");
            }
            ViewBag.AvailableGroups = await GetAvailableGroupsForTag(tagID, userID);
            return PartialView(tag);
        }

        private async Task<List<UserTagListModel>> GetAvailableGroupsForTag(int? tagID, string userID)
        {
            List<UserTagListModel> availabeGroups = new List<UserTagListModel>();
            foreach (var group in await GetUsersGroupsWithRowsAsync(userID))
            {
                bool canAddTagTo = true;
                foreach (var row in group.TagListRowModels)
                {
                    if (row.TagId == tagID)
                    {
                        canAddTagTo = false;
                        break;
                    }
                }
                if (canAddTagTo)
                {
                    availabeGroups.Add(group);
                }
            }

            return availabeGroups;
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddTagToList(AddTagToListModel row)
        {
            if (ModelState.IsValid)
            {
                string userID = User.Identity.GetUserId();
                if (!await UserHasTagIdAsync(userID, row.TagsId) && !await UserHasGroupIdAsync(userID, row.ListsId))
                {
                    TempData["GeneralError"] = "You don't have permission to do that.";
                    return RedirectToAction("Index");
                }
                if (await TagIsInListAsync(row.TagsId, row.ListsId))
                {
                    TempData["GeneralError"] = "Tag is already in that group.";
                    return RedirectToAction("Index");
                }
                db.TagListRowModels.Add(new TagListRowModel { ListId = row.ListsId, TagId = row.TagsId });
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            TempData["GeneralError"] = "Something went wrong with adding tag to group. Please try again.";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<ActionResult> Groups()
        {
            ViewBag.Errors = TempData["GeneralGroupsErrors"];
            if (ViewBag.Errors != null)
            {
                foreach (var e in ViewBag.Errors)
                {
                    ModelState.AddModelError(string.Empty, e);
                }
            }
            string userID = User.Identity.GetUserId();
            var tags = new Dictionary<int, RuuviTagModel>();
            foreach (var tag in await GetUserTagsAsync(userID))
            {
                tags.Add(tag.TagId, tag);
            }
            ViewBag.UsersTags = tags;
            List<UserTagListModel> userGroups = await GetUsersGroupsWithRowsAsync(userID);
            return View(userGroups);
        }

        private async Task<List<UserTagListModel>> GetUsersGroupsWithRowsAsync(string userID)
            => await db.UserTagListModels.Where(g => g.UserId == userID).Include(r => r.TagListRowModels).ToListAsync();
        
        private async Task<List<UserTagListModel>> GetUsersGroupsAsync(string userID)
            => await db.UserTagListModels.Where(l => l.UserId == userID).ToListAsync();

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpDelete]
        public async Task<ActionResult> Groups(int? listRowID)
        {
            if (listRowID == null)
            {
                TempData["GeneralGroupsErrors"] = new string[] { "Unable to remove tag from group due to missing ID. Please try again." };
                return RedirectToAction("Groups");
            }
            TagListRowModel row = db.TagListRowModels.Find(listRowID);
            if (row == null)
            {
                TempData["GeneralGroupsErrors"] = new string[] { "Tag was not removed, since it's not in that group." };
                return RedirectToAction("Groups");
            }
            if (row.RuuviTagModel.UserId != User.Identity.GetUserId())
            {
                TempData["GeneralGroupsErrors"] = new string[] { "You do not have access to do that." };
                return RedirectToAction("Groups");
            }
            db.TagListRowModels.Remove(row);
            await db.SaveChangesAsync();
            return RedirectToAction("Groups");
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> AddUserTagList(NewTagListModel list)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(list.ListName))
                {
                    TempData["GeneralGroupsErrors"] = new string[] { "Unable to create group, a name for a group is required." };
                    return RedirectToAction("Groups");
                }
                string userID = User.Identity.GetUserId();
                var userLists = await GetUsersGroupsAsync(userID);
                if (userLists.Select(l => l.ListName).Contains(list.ListName))
                {
                    TempData["GeneralGroupsErrors"] = new string[] { "Unable to create group, since you already have a group with that name." };
                    return RedirectToAction("Groups");
                }
                var newList = db.UserTagListModels.Add(new UserTagListModel { ListName = list.ListName, UserId = userID });
                if (!string.IsNullOrWhiteSpace(list.IdsAsString))
                {
                    var userTags = await GetUserTagsAsync(userID);
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
            TempData["GeneralGroupsErrors"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return RedirectToAction("Groups");
        }

        [Authorize]
        [HttpPost, ActionName("Groups")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteGroup(int? tagListID)
        {
            if (tagListID == null)
            {
                TempData["GeneralGroupsErrors"] = new string[] { "Unable to remove group due to missing ID. Please try again." };
                return RedirectToAction("Groups");
            }
            UserTagListModel group = db.UserTagListModels.Find(tagListID);
            if (group == null)
            {
                TempData["GeneralGroupsErrors"] = new string[] { "Group does not exist." };
                return RedirectToAction("Groups");
            }
            if (group.UserId != User.Identity.GetUserId())
            {
                TempData["GeneralGroupsErrors"] = new string[] { "You do not have access to do that." };
                return RedirectToAction("Groups");
            }
            db.UserTagListModels.Remove(group);
            await db.SaveChangesAsync();
            return RedirectToAction("Groups");
        }
    }
}