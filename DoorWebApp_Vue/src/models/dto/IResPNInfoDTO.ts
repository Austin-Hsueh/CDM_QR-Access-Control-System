/** 回傳料件相關資訊(目前只回傳料號、事業部、客戶名稱) */
export default interface IResPNInfoDTO {
  /** 料號 */
  pn: string;

  /** 事業部 */
  bu: string;

  /** 客戶名稱 */
  custName: string;
}

