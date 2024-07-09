using Microsoft.AspNetCore.Authorization;

namespace DoorWebApp
{
    public class DoorAuthorize : AuthorizeAttribute
    {
        public DoorAuthorize(params string[] roles) : base()
        {
            
        }

       
    }

    //public class EntitlementAttritube 
    //{

    //    public EntitlementAttritube(params EntitlementType[] roles) : base()
    //    {

    //        //#if DEBUG
    //        //            //Debug時權限全開
    //        //            roles = Enum.GetValues(typeof(EntitlementType)).Cast<EntitlementType>().ToArray();
    //        //#endif

    //        //取出Entitlement的Enum值並轉為文字 (於產生JWT時候，其Roles值便是使用Enum的數值文字)
    //        //由於不想要把權限類別的文字給使用者知道，所以Role的部分只用代號顯示，
    //        //而又因產生JWT是，Claim的Value只允許輸入文字，因此才會將Enum做兩次轉換
    //        var EntitlementNumList = roles.Select(x => ((int)x).ToString()).ToList();

    //        base.Roles = string.Join(",", EntitlementNumList);
    //    }
    //}
}
