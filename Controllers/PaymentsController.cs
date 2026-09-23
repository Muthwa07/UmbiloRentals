using System;
using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class BaseController : Controller
    {
        protected BuildingManagementDBEntities db = new BuildingManagementDBEntities();

        protected bool IsAuthenticated()
        {
            return Session["UserID"] != null;
        }

        protected bool IsUserInRole(int roleId)
        {
            return IsAuthenticated() &&
                   Session["RoleID"] != null &&
                   (int)Session["RoleID"] == roleId;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            ViewBag.NotificationCount = 0;

            if (IsAuthenticated() && Session["RoleID"] != null)
            {
                int userId = (int)Session["UserID"];
                ViewBag.NotificationCount = db.Notifications.Count(n =>
                    n.UserID == userId &&
                    n.IsRead == false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
