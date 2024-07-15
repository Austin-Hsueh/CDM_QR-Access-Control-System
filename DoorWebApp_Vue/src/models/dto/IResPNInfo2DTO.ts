import IResRoutingInfoDTO from '@/models/dto/IResRoutingInfoDTO';
/** 回傳料件相關資訊(目前只回傳料號、事業部、客戶名稱) */
export default interface IResPNInfo2DTO {
  /** 料號 */
  pn: string;

  /** 事業部 */
  bu: string;

  /** 客戶名稱 */
  custName: string;

  /** Routing Info */
  routings: IResRoutingInfoDTO[];
}

