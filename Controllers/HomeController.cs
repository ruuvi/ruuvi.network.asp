using RuuviTagApp.Models;
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
            ViewBag.RenderRegisterModal = true;
            //ViewBag.LoginProvider = "Google";
            //ViewBag.RenderRegisterModal = TempData["RenderRegisterModal"];
            //ViewBag.LoginProvider = TempData["LoginProvider"];
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

        public ActionResult AddTag(RuuviTagModel tag)
        {
            throw new NotImplementedException();
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