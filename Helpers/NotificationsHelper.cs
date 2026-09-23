using System;
using UmbiloRentals.Models;

namespace UmbiloRentals.Helpers
{
    public static class NotificationHelper
    {
        public static void CreateNotification(
            BuildingManagementDBEntities db,
            int userId,
            string message)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));

            if (userId <= 0)
                return;

            if (string.IsNullOrWhiteSpace(message))
                return;

            Notification notification = new Notification
            {
                UserID = userId,
                Message = message.Trim(),
                DateSent = DateTime.Now,
                IsRead = false
            };

            db.Notifications.Add(notification);
        }
    }
}
