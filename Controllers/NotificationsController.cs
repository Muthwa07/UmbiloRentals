using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class NotificationsController : Controller
    {
        private BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        public ActionResult Index()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var messages = db.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.DateSent)
                .ToList();

            foreach (var item in messages)
                item.IsRead = true;

            db.SaveChanges();

            return View(messages);
        }
    }
}