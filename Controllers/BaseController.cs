using System.Web.Mvc;
using UmbiloRentals.Models;
using System.Linq;

namespace UmbiloRentals.Controllers
{
    public class BaseController : Controller
    {
        protected BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            ViewBag.NotificationCount = 0;

            if (Session["UserID"] != null &&
                Session["RoleID"] != null &&
                (int)Session["RoleID"] == 1)
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