namespace SoyalQRGen.Entities
{
    public class UserQRCode
    {
        public int userAddr { get; set; }
        public uint userTag { get; set; }
        public string qrcodeTxt { get; set; } = null!;
        public string qrcodeImg { get; set; } = null!;
    }
}
