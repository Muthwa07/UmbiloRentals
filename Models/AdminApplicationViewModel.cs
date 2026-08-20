using System;

namespace UmbiloRentals.Models
{
    public class AdminApplicationViewModel
    {
        public int ApplicationID { get; set; }
        public string ApplicantName { get; set; }
        public string RoomNumber { get; set; }
        public string Status { get; set; }
        public DateTime? DateApplied { get; set; }
        public string DocumentPath { get; set; }
        public int? RoomID { get; set; }
    }
}