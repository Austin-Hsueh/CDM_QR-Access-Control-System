import { dateFormat } from "./../plugins/utility";
import IResManufMethodDTO from "@/models/dto/IResManufMethodDTO";
import IResRoutingInfoDTO from "@/models/dto/IResRoutingInfoDTO";
import IResPNInfo2DTO from "@/models/dto/IResPNInfo2DTO";
import IResProjectInfoDTO from "@/models/dto/IResProjectInfoDTO";
import IReqKaizenItemDTO from "./dto/IReqKaizenItemDTO";
import IResKaizenItemDTO from "./dto/IResKaizenItemDTO";

export default class KaizenItemViewModel {
  /** 所屬專案代碼
   *  不允許直接綁定project?，因其為可undefined的物件，故將project code獨立出來供綁定
   */
  projectCode?: string;

  /** 所屬專案 */
  project?: IResProjectInfoDTO;

  /** 改善料號 */
  apsPNInfo?: IResPNInfo2DTO;

  /** Routing(含作業編號資訊) */
  apsRoutingInfo?: IResRoutingInfoDTO;

  /** 工業類別 */
  manufMethod?: IResManufMethodDTO;

  /** 改善方案 */
  impPlan?: string;

  /** 改善對策 */
  impMethod?: string;

  /** 對策內容 */
  impMethodDetail?: string;

  /** 工時調整日期 */
  implementDay: Date = new Date();

  /** 超連結 */
  hyperLink?: string;

  /** 改善前工時(秒) */
  procTimeBeforeImp?: number;

  /** 改善後工時(秒) */
  procTimeAfterImp?: number;

  ToIReqKaizenItemDTO(): IReqKaizenItemDTO {
    if (!this.project) throw new Error("project is required");
    if (!this.apsPNInfo) throw new Error("apsPNInfo is required");
    if (!this.apsRoutingInfo) throw new Error("apsRoutingInfo is required");
    if (!this.manufMethod) throw new Error("manufMethod is required");
    if (!this.procTimeAfterImp) throw new Error("procTimeAfterImp is required");
    if (!this.impPlan) throw new Error("impPlan is required");
    if (!this.impMethod) throw new Error("impMethod is required");
    if (!this.impMethodDetail) throw new Error("impMethodDetail is required");
    if (!this.implementDay) throw new Error("implementDay is required");

    
    const dto: IReqKaizenItemDTO = {
      projectCode: this.project.pjCode,
      pn: this.apsPNInfo.pn,
      customerName: this.apsPNInfo.custName,
      workNum: this.apsRoutingInfo.workNum,
      manufMethodId: this.manufMethod.id,

      impPlan: this.impPlan,
      impMethod: this.impMethod,
      impMethodDetail: this.impMethodDetail,
      implementDay: dateFormat(this.implementDay),
      hyperlink: this.hyperLink,
      
      //procTimeBeforeImp: this.procTimeBeforeImp,
      procTimeAfterImp: this.procTimeAfterImp,
    };
    return dto;
  }
}
