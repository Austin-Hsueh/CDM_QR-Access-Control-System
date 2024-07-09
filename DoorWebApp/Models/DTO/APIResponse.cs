namespace DoorWebApp.Models.DTO
{
    public enum APIResultCode
    {
        success = 1,
        unknow_error = 0,
        missing_parameter = 2,
        delete_forbidden = 3,
        invalid_parameter = 4,
        csv_gen_fail = 5,
        file_not_found = 6,
        item_is_being_used = 7,


        authentication_failed = 100,
        user_not_found = 101,
        duplicate_user_id = 102,
        invalid_user_roles = 103,
        reached_logon_attempts = 104,
        already_login = 105,
        account_suspend = 106,
        ldap_error = 107,
        user_has_been_deleted = 108,

        role_not_found = 301,
        duplicate_role_name = 302,
        role_delete_is_not_allowed = 303,
        duplicate_method_name = 304,

        site_not_found = 401,
        duplicate_site_name = 402,
        site_name_is_required = 403,
        site_has_been_deleted = 404,
        invalid_site_name = 405,

        factory_not_found  = 501,
        factory_has_been_deleted = 502,


        period_not_found = 601,
        period_has_been_deleted = 602,
        period_name_conflict = 603,
        period_duratiuon_conflict = 604,
        duplicate_period_name = 605,


        project_not_found = 611,
        project_has_been_deleted = 612,
        project_conflict = 613,
        kaizen_item_not_found = 614,
        topPNInfo_not_fount = 615,
        duplicate_kaizen_item = 617,
        
        


        pn_not_found = 701,
        route_not_found = 702,

        out_of_time_range = 801,

        db_operate_failed = 999,
    }


    public class APIResponse
    {
        public APIResultCode result { set; get; }
        
        public string msgI18n { get => result.ToString();  }

        public string msg { set; get; }
    }

    public class APIResponse<T> : APIResponse
    {
        public T content { set; get; }
    }
}
