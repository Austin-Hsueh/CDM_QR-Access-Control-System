import IReqRoleInfoDTO from "@/models/dto/IReqRoleInfoDTO";
import IReqUserRoleDTO from "@/models/dto/IReqUserRoleDTO";
import IResUserInfoDTO from "@/models/dto/IResUserInfoDTO";
import { IBaseAPIResponse } from "@/models/IBaseAPIResponse";
import IAPIResponse from "@/models/IAPIResponse";
import { useRouter } from "vue-router";
import axios, { AxiosInstance, AxiosRequestConfig } from "axios";
import IReqLoginDTO from "@/models/dto/IReqLoginDTO";
import IResUserAuthInfoDTO from "@/models/dto/IResUserAuthInfoDTO";
import IResUserLoginDTO from "@/models/dto/IResUserLoginDTO";
import IResManufMethodDTO from "@/models/dto/IResManufMethodDTO";
import IReqManufMethodDTO from "@/models/dto/IReqManufMethodDTO";
import IReqManufMethodProjectListDTO from "../models/dto/IReqReportManufKaizenListSearchCriteriaDTO";
import IResRoleInfoDTO from "@/models/dto/IResRoleInfoDTO";
import IReqProjectSearchCriteriaDTO from "@/models/dto/IReqProjectSearchCriteriaDTO";
import IResProjectInfoDTO from "@/models/dto/IResProjectInfoDTO";
import IReqProjectInfoDTO from "@/models/dto/IReqProjectInfoDTO";
import IResProjectDetailDTO from "@/models/dto/IResProjectDetailDTO";
import IResSiteInfoDTO from "@/models/dto/IResSiteInfoDTO";
import IReqSiteInfoDTO from "@/models/dto/IReqSiteInfoDTO";
import IReqPNInfoSearchCriteriaDTO from "@/models/dto/IReqPNInfoSearchCriteriaDTO";
import IResPNInfo2DTO from "@/models/dto/IResPNInfo2DTO";
import IReqKaizenItemDTO from "@/models/dto/IReqKaizenItemDTO";
import IReqKaizenItemSearchCriteriaDTO from "@/models/dto/IReqKaizenItemSearchCriteriaDTO";
import IPagingDTO from "@/models/dto/IPagingDTO";
import IResKaizenItemDTO from "@/models/dto/IResKaizenItemDTO";
import IReqTableColDisplayDTO from "@/models/dto/IReqTableColDisplayDTO";
import IReqPeriodSearchCriteriaDTO from "@/models/dto/IReqPeriodSearchCriteriaDTO";
import IResPeriodInfoDTO from "@/models/dto/IResPeriodInfoDTO";
import IReqPeriodInfoDTO from "@/models/dto/IReqPeriodInfoDTO";
import IResPeriodDetailDTO from "@/models/dto/IResPeriodDetailDTO";
import IReqValidatePeriodNumDTO from "@/models/dto/IReqValidatePeriodNumDTO";
import IReqValidatePeriodDurationDTO from "@/models/dto/IReqValidatePeriodDurationDTO";
import IReqReportManufKaizenListSearchCriteriaDTO from "../models/dto/IReqReportManufKaizenListSearchCriteriaDTO";
import IResKaizenItemsWithManufMethodDTO from "@/models/dto/IResKaizenItemsWithManufMethodDTO";
import IReqTopPNInfoSearchDTO from "@/models/dto/IReqTopPNInfoSearchDTO";
import IResTopPNInfoTableViewModelDTO from "@/models/dto/IResTopPNInfoTableViewModelDTO";
import IResTopPNInfoDetailDTO from "@/models/dto/IResTopPNInfoDetailDTO";
import IReqTopPNInfoDTO from "@/models/dto/IReqTopPNInfoDTO";
import IReqReportPerformanceSearchCriteriaDTO from "@/models/dto/IReqReportPerformanceSearchCriteriaDTO";
import IResTPSPerformanceDataViewDTO from "@/models/dto/IResTPSPerformanceDataViewDTO";
import { useUserInfoStore } from "@/stores/UserInfoStore";

import {M_IUsers} from "@/models/M_IUser";
import M_IUserinfo from "@/models/M_IUseinfo";
import {M_ICreateRuleForm} from "@/models/M_IRuleForm";


class APIService {
  public axiosInstance: AxiosInstance;

  constructor() {
    //console.log("APIService constructor");
    const router = useRouter();
    //const userInfoStore = useUserInfoStore();

    this.axiosInstance = axios.create({
      baseURL: "http://localhost:8080/api",
    });

    this.axiosInstance.interceptors.request.use((config) => {
      //const token = localStorage.getItem("accessToken");
      const token = useUserInfoStore().token;
      config.headers = {
        "Content-type": "application/json",
        Authorization: `Bearer ${token}`,
      };
      return config;
    });

    this.axiosInstance.interceptors.response.use(
      (response) => {
        // console.log(`response.status:${response.status}`);
        return response;
      },
      (error) => {
        // console.log(`response.status:${error.response.status}`);
        if (error.response.status === 401 || error.response.status === 403) {
          // console.log("redirect to login page");
          router.replace({ name: "Login" });
        }
      }
    );
  }

  private async axiosCall<T>(config: AxiosRequestConfig) {
    try {
      const data = await this.axiosInstance.request<T>(config);
      return [null, data];
    } catch (error) {
      return [error];
    }
  }

  //#region UserController : 使用者相關
  /** 使用者登入 */
  login(secret: IReqLoginDTO) {
    return this.axiosInstance.post<IAPIResponse<IResUserLoginDTO>>("v1/User/login", secret);
  }

  /** 取得使用者基本資訊(使用Bearer Token(JWT)區分使用者) */
  refreshToken() {
    return this.axiosInstance.get<IAPIResponse<IResUserAuthInfoDTO>>("v1/User/RefreshToken");
  }

  /** 取得使用者清單(含角色資訊) */
  getAllUsersWithRoles() {
    return this.axiosInstance.get<IAPIResponse<IResUserInfoDTO[]>>("v1/Users");
  }

  /** 更新單一名使用者的角色 */
  updateUserRoles(userId: number, userRoleDTO: IReqUserRoleDTO) {
    return this.axiosInstance.patch<IBaseAPIResponse>(`v1/User/Role/${userId}`, userRoleDTO);
  }

  /** 取得使用者權限清單  */
  getPermissionList(userId: number) {
    return this.axiosInstance.get<IAPIResponse<number[]>>(`v1/User/Permission/${userId}`);
  }

  /** 使用者登出 */
  logout() {
    //return this.axiosInstance.post<IBaseAPIResponse>("v1/User/signout");
  }
  //#endregion

  //#region RoleController : 角色相關
  /** 取得角色清單(含權限資訊) */
  getAllRolesWithPermissions() {
    return this.axiosInstance.get<IAPIResponse<IResRoleInfoDTO[]>>("v1/Roles");
  }

  /** 新增角色 */
  createRole(roleDTO: IReqRoleInfoDTO) {
    return this.axiosInstance.post<IBaseAPIResponse>("v1/Role", roleDTO);
  }

  /** 更新角色  */
  updateRole(roleId: number, roleDTO: IReqRoleInfoDTO) {
    return this.axiosInstance.put<IBaseAPIResponse>(`v1/Role/${roleId}`, roleDTO);
  }

  /** 刪除角色 */
  deleteRole(roleId: number) {
    return this.axiosInstance.delete<IBaseAPIResponse>(`v1/Role/${roleId}`);
  }
  //#endregion

  //#region ManufMethodController : 工藝類別相關
  /** 取得所有工藝類別 */
  getManufMethods() {
    return this.axiosInstance.get<IAPIResponse<IResManufMethodDTO[]>>("v1/ManufMethods");
  }

  /** 取得單一工藝類別(by Id) */
  getManufMethod(Id: number) {
    return this.axiosInstance.get<IAPIResponse<IResManufMethodDTO>>(`v1/ManufMethod/${Id}`);
  }

  /** 新增一個工藝類別 */
  addManufMethod(newManufMethod: IReqManufMethodDTO) {
    return this.axiosInstance.post<IAPIResponse<IResManufMethodDTO>>(`v1/ManufMethod`, newManufMethod);
  }

  /** 修改一個工藝類別 */
  modifyManufMethod(Id: number, modifiedManufMethod: IReqManufMethodDTO) {
    return this.axiosInstance.put<IAPIResponse<IResManufMethodDTO>>(`v1/ManufMethod/${Id}`, modifiedManufMethod);
  }

  /** 刪除一個工藝類別 */
  deleteManufMethod(Id: number) {
    return this.axiosInstance.delete<IAPIResponse<string[]>>(`v1/ManufMethod/${Id}`);
  }
  //#endregion

  //#region SiteController : 廠區相關
  /** 取得所有廠區清單 */
  getAllSites() {
    return this.axiosInstance.get<IAPIResponse<IResSiteInfoDTO[]>>("v1/Sites");
  }

  /** 取得可供選擇建檔的廠區清單 */
  getSiteOptions() {
    return this.axiosInstance.get<IAPIResponse<string[]>>("v1/Site/Options");
  }

  /** 取得單一廠區(by SiteId) */
  getSiteById(siteId: number) {
    return this.axiosInstance.get<IAPIResponse<IResSiteInfoDTO>>(`v1/Site/${siteId}`);
  }

  /** 新增一個廠區(含工廠資訊) */
  addSite(newSite: IReqSiteInfoDTO) {
    return this.axiosInstance.post<IBaseAPIResponse>(`v1/Site`, newSite);
  }

  /** 修改一個廠區(連同工廠資訊) */
  modifySite(siteId: number, modifiedSite: IReqSiteInfoDTO) {
    return this.axiosInstance.put<IBaseAPIResponse>(`v1/Site/${siteId}`, modifiedSite);
  }

  /** 刪除一個廠區 */
  deleteSite(siteId: number) {
    return this.axiosInstance.delete<IBaseAPIResponse>(`v1/Site/${siteId}`);
  }
  //#endregion

  //#region PeriodController : 專案期數維護
  /** 搜尋期數 */
  searchPeriod(criteria: IReqPeriodSearchCriteriaDTO) {
    return this.axiosInstance.post<IAPIResponse<IResPeriodInfoDTO[]>>(`v1/Period/Search`, criteria);
  }

  /** 取得期數細項內容(by Id) */
  getPeriodDetailById(periodId: number) {
    return this.axiosInstance.get<IAPIResponse<IResPeriodDetailDTO>>(`v1/Period/${periodId}`);
  }

  /** 取得期數簡易內容(by PeriodNum) */
  getPeriodInfoByNum(periodNum: number) {
    return this.axiosInstance.get<IAPIResponse<IResPeriodInfoDTO>>(`v1/Period/Num/${periodNum}`);
  }


  /** 取得尚未過期的專案期數(於新增專案代碼可供選擇的期數) */
  getAvailablePeriodItems() {
    return this.axiosInstance.get<IAPIResponse<IResPeriodInfoDTO[]>>(`v1/Period/Available`);
  }
  /** 建立期數 */
  createPeriod(newPeriod: IReqPeriodInfoDTO) {
    return this.axiosInstance.post<IBaseAPIResponse>(`v1/Period`, newPeriod);
  }

  /** 更新期數內的部分資訊 */
  updatePeriod(PeriodId: number, PeriodInfo: IReqPeriodInfoDTO) {
    return this.axiosInstance.patch<IBaseAPIResponse>(`v1/Period/${PeriodId}`, PeriodInfo);
  }

  /** 刪除期數 */
  deletePeriod(PeriodId: number) {
    return this.axiosInstance.delete<IAPIResponse<string[]>>(`v1/Period/${PeriodId}`);
  }

  //#endregion

  //#region ProjectController : 專案代碼維護
  // 搜尋專案
  searchProject(criteria: IReqProjectSearchCriteriaDTO) {
    return this.axiosInstance.post<IAPIResponse<IResProjectInfoDTO[]>>(`v1/Project/Search`, criteria);
  }

  /** 取得專案細項內容(by Id) */
  getProjectById(projectId: number) {
    return this.axiosInstance.get<IAPIResponse<IResProjectDetailDTO>>(`v1/Project/${projectId}`);
  }

  /** 建立專案 */
  createProject(newProject: IReqProjectInfoDTO) {
    return this.axiosInstance.post<IBaseAPIResponse>(`v1/Project`, newProject);
  }

  /** 更新專案內的部分資訊 */
  updateProject(projectId: number, projectInfo: IReqProjectInfoDTO) {
    return this.axiosInstance.patch<IBaseAPIResponse>(`v1/Project/${projectId}`, projectInfo);
  }

  /** 刪除專案 */
  deleteProject(projectId: number) {
    return this.axiosInstance.delete<IAPIResponse<string[]>>(`v1/Project/${projectId}`);
  }
  // #endregion

  //#region KaizenController : 改善事項相關
  /** 新增改善事項 */
  createKaizenItem(newKaizenItem: IReqKaizenItemDTO) {
    return this.axiosInstance.post<IBaseAPIResponse>(`v1/Kaizen`, newKaizenItem);
  }

  /** 取得一筆改善事項(by Id) */
  getKaizenItemById(kaizenId: number) {
    //先將回傳的結果設定成與searchKaizenItem一致，保留往後有需求變更的狀況
    return this.axiosInstance.get<IAPIResponse<IResKaizenItemDTO>>(`v1/Kaizen/${kaizenId}`); 
  }

  /** 更新改善事項 */
  patchKaizenItem(kaizenId:number, kaizenItem:IReqKaizenItemDTO) {
    return this.axiosInstance.patch<IBaseAPIResponse>(`v1/Kaizen/${kaizenId}`, kaizenItem);
  }

  /** 搜尋改善事項(一般搜尋) */
  searchKaizenItem(criteria: IReqKaizenItemSearchCriteriaDTO) {
    return this.axiosInstance.post<IAPIResponse<IPagingDTO<IResKaizenItemDTO>>>("v1/Kaizen/Search", criteria);
  }

  /** 匯出改善事項搜尋結果(一般搜尋) */
  exportKaizenItemsBySearchCriteria(criteria: IReqKaizenItemSearchCriteriaDTO) {
    return this.axiosInstance.post<IAPIResponse<string>>("v1/Kaizen/Export/Search", criteria)
  }


  /** 搜尋改善事項(依工藝類別與期數) */
  searchKaizenItemByManufMethods(criteria: IReqReportManufKaizenListSearchCriteriaDTO) {
    return this.axiosInstance.post<IAPIResponse<IPagingDTO<IResKaizenItemsWithManufMethodDTO>>>("v1/Kaizen/Search/ManufMethod", criteria);
  }

  /** 匯出改善事項搜尋結果(依工藝類別與期數) */
  exportKaizenItemsByManufMethods(criteria: IReqReportManufKaizenListSearchCriteriaDTO) {
    return this.axiosInstance.post<IAPIResponse<string>>("v1/Kaizen/Export/ManufMethod", criteria)
  }


  // #endregion

  //#region PNController : 成品料號維護
  /** 搜尋成品料號相關改善資訊 */
  searchTopPNInfo(criteria: IReqTopPNInfoSearchDTO) {
    return this.axiosInstance.post<IAPIResponse<IPagingDTO<IResTopPNInfoTableViewModelDTO>>>(`v1/PNInfos/Search`, criteria);
  }

  /** 取得成品料號改善資訊細部項目(by Id) */
  getTopPNInfoDetailById(topPNInfoId: number){
    return this.axiosInstance.get<IAPIResponse<IResTopPNInfoDetailDTO>>(`v1/PNInfo/${topPNInfoId}`);
  }

  /** 更新面積改善、Vender L/T, 產品L/T, PPH等資訊 */
  patchTopPNInfo(topPNInfoId: number, newTopPNInfo: IReqTopPNInfoDTO) {
    return this.axiosInstance.patch<IBaseAPIResponse>(`v1/PNInfo/${topPNInfoId}`, newTopPNInfo);
  }

  //#endregion

  //#region ReportController: 報表相關
  /** 搜尋TSP改善效益追蹤表 */
  searchTPSPerformanceReport(criteria: IReqReportPerformanceSearchCriteriaDTO) {
    return this.axiosInstance.post<IAPIResponse<IPagingDTO<IResTPSPerformanceDataViewDTO>>>(`v1/Report/TPSPerformance/Search`, criteria)
  }

  /** 匯出改善效益追蹤表 */
  exportTPSPerformanceReport(criteria: IReqReportPerformanceSearchCriteriaDTO) {
    return this.axiosInstance.post<IAPIResponse<string>>("v1/Report/TPSPerformance/Export", criteria)
  }

  //#endregion

  //#region SBERPController : 向門禁的ERP查詢資料
  /** 依給定的關鍵字搜尋料號，並回傳指定數量的結果供使用者選擇，
   *  v2版本於回傳資料中除了基本的PN資訊外，同時也將一併傳回所有可選的WorkNum與其相關的routing資訊
   */
  searchPartInfoV2(searchCriteria: IReqPNInfoSearchCriteriaDTO) {
    return this.axiosInstance.post<IAPIResponse<IResPNInfo2DTO[]>>(`v2/SBERP/PNInfo/Search`, searchCriteria);
  }
  //#endregion

  //#region PreferenceController : 使用者偏好設置相關
  /** 取得Table欄位顯示偏好設置 */
  getTableDisplayPreference(tableType: string) {
    return this.axiosInstance.get<IAPIResponse<boolean[]>>(`v1/Preference/TblDisplay/${tableType}`);
  }

  /** 更新Table欄位顯示偏好設置 */
  patchTableDisplayPreference(tableType: string, prefDTO: IReqTableColDisplayDTO) {
    return this.axiosInstance.patch<IBaseAPIResponse>(`v1/Preference/TblDisplay/${tableType}`, prefDTO);
  }
  //#endregion

  //#region ValidateController : 表單輸入驗證相關(實驗中)
  /** 檢查專案期數是否有重複 */
  validatePeriodNum(inputPeriodNum: number, excludeItemId?: number) {
    const input: IReqValidatePeriodNumDTO = {
      periodNum: inputPeriodNum,
      excludeItemId: excludeItemId
    }
    return this.axiosInstance.post<IAPIResponse<number>>(`v1/Validate/Period/Num`, input);
  }

  /** 檢查專案期數起訖月份是否有重疊 */
  validatePeriodDuration(inputMonth1: Date, inputMonth2: Date,  excludeItemId?: number) {

    const input: IReqValidatePeriodDurationDTO = {
      startMon:  inputMonth1 > inputMonth2 ? inputMonth2 : inputMonth1,
      endMon: inputMonth1 < inputMonth2 ? inputMonth2 : inputMonth1,
      excludeItemId: excludeItemId
    }
    return this.axiosInstance.post<IAPIResponse<number>>(`v1/Validate/Period/Duration`, input);  }
  //#endregion

  //#region DownloadController : 下載
  /** 下載報表 */
  downloadReportFile(filename: string) {
    return this.axiosInstance.get(`v1/Download/Report/${filename}`, { responseType: "blob" });
  }
  //#endregion
  
  //#region Music 使用者相關
  /** 取得使用者清單 */
  getAllUsers(){
    return this.axiosInstance.get<IAPIResponse<M_IUsers>>("v1/Users")
  }

  getUserPermission(){    
    return this.axiosInstance.get<IAPIResponse<M_IUserinfo>>("v1/User/Permission")
  }

  addUser(data: M_ICreateRuleForm){
    return this.axiosInstance.post<IAPIResponse<number>>("v1/User", data)
  }

  //#endregion
}

const API = new APIService();
export default API;
