/** 回傳料件相關資訊(目前只回傳料號、作業編號、改善前工時、作業說明、原PPH) */
export default interface IResRoutingInfoDTO {
  /** 料號 */
  pn: string;

  /** 作業編號 */
  workNum: number;

  /** 作業說明 */
  workRemark: string;

  /** 改善前工時 */
  stdTime: number;

  /** 原PPH */
  pph: string;
}