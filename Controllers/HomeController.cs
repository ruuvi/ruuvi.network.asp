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

                // DECODE DATA HERE ?

                TempData["ApiResponse"] = apiResponse;
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
                bool userHasTag = await UserHasTag(userID, tag.GetAddress());
                bool tagNameTaken = !string.IsNullOrEmpty(tag.AddTagName) && await TagNameTaken(userID, tag.AddTagName);
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

        private async Task<bool> UserHasTag(string userID, string mac) => await (from t in db.RuuviTagModels
                                                                                 where t.UserId == userID && t.TagMacAddress == mac
                                                                                 select t).FirstOrDefaultAsync() != null;

        private async Task<bool> TagNameTaken(string userID, string name) => await (from t in db.RuuviTagModels
                                                                                    where t.UserId == userID && t.TagName == name
                                                                                    select t).FirstOrDefaultAsync() != null;
        
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
                if (!string.IsNullOrWhiteSpace(tag.TagName) && await TagNameTaken(User.Identity.GetUserId(), tag.TagName))
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
            foreach(var tag in await GetUserTagsAsync(userID))
            {
                userTags.Add(new SelectListItem { Value = tag.TagId.ToString(), Text = tag.TagName ?? tag.TagMacAddress });
            }
            ViewBag.UserTagDropdownList = new SelectList(userTags, "Value", "Text");
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