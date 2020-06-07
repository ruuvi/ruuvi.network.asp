using RuuviTagApp.Models;
using RuuviTagApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RuuviTagApp.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        public ActionResult Index()
        {
            // tämän pitäisi luoda tietokanta
            //var tag = db.RuuviTagModels.Find(1);
            ViewBag.RenderRegisterModal = TempData["RenderRegisterModal"];
            ViewBag.LoginProvider = TempData["LoginProvider"];
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Help()
        {
            ViewBag.Message = "Your help page.";

            return View();
        }

        [HttpPost]
        public ActionResult SearchTag(MacAddressModel mac)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", mac);
            }
            return RedirectToAction("Index");
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

        public async Task<JsonResult> GetTagData(string macAddress)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}