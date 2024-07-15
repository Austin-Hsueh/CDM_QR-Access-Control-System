export default interface IReqTopPNInfoSearchDTO {
  /** 當前頁數 */
  page: number;

  /** 每頁個數 */
  pageSize: number;

  /** 排序欄位名稱 */
  sortColName?: string;

  /** 排序方向 */
  sortOrder?: string;

  /** 專案代碼keyword */
  periodNum?: number;

  /** 最上階料號keyword */
  topPNKeyword?: string;
}
