export default interface IReqKaizenItemSearchCriteriaDTO {
  /** 當前頁數 */
  page:number;

  /** 每頁個數 */
  pageSize: number;

  /** 排序欄位名稱 */
  sortColName?: string;

  /** 排序方向 */
  sortOrder?: string;

  /** 專案代碼 */
  pjcode?: string;

  /** 改善料號 */
  pn? : string;

  /** 小組 */
  teamNum? : number;

  /** 建立者帳號 */
  creator?: string;

  /** 專案開始時間 */
  startDate?: Date;

  /** 專案結束時間 */
  endDate?: Date;

  /** 工藝類別Id清單 */
  manufMethodIds: number[];
}