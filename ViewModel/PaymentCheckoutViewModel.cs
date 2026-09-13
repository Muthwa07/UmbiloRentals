namespace UmbiloRentals.ViewModels
{
    public class PaymentCheckoutViewModel
    {
        public int ApplicationID { get; set; }

        public string ApplicantName { get; set; }

        public string RoomNumber { get; set; }

        public decimal Amount { get; set; }

        public string Reference { get; set; }

        public string CheckoutUrl { get; set; }
    }
}