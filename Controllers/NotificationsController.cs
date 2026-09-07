using System;
using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class NotificationsController : BaseController
    {
        private readonly BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        private bool IsApplicant()
        {
            return Session["UserID"] != null &&
                   Session["RoleID"] != null &&
                   (int)Session["RoleID"] == 1;
        }

        // Makes notification count available on every page
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (Session["UserID"] != null &&
                Session["RoleID"] != null &&
                (int)Session["RoleID"] == 1)
            {
                int userId = (int)Session["UserID"];

                ViewBag.NotificationCount =
                    db.Notifications.Count(n =>
                        n.UserID == userId &&
                        n.IsRead == false);
            }
        }

        // Notification Centre
        public ActionResult Index()
        {
            if (!IsApplicant())
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var notifications = db.Notifications
                                  .Where(n => n.UserID == userId)
                                  .OrderByDescending(n => n.DateSent)
                                  .ToList();

            ViewBag.NotificationCount =
                notifications.Count(n => n.IsRead == false);

            return View(notifications);
        }

        // Mark one notification as read
        public ActionResult MarkAsRead(int id)
        {
            if (!IsApplicant())
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var notification = db.Notifications.FirstOrDefault(n =>
                n.NotificationID == id &&
                n.UserID == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // Mark all notifications as read
        public ActionResult MarkAllRead()
        {
            if (!IsApplicant())
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var unread = db.Notifications
                           .Where(n =>
                               n.UserID == userId &&
                               n.IsRead == false)
                           .ToList();

            foreach (var n in unread)
                n.IsRead = true;

            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}