/** 報表: 依工藝類別查詢 */ 
export default interface IReqReportManufKaizenListSearchCriteriaDTO {
  /** 當前頁數 */
  page:number;

  /** 每頁個數 */
  pageSize: number;

  /** 排序欄位名稱 */
  sortColName?: string;

  /** 排序方向 */
  sortOrder?: string;

  /** 所選的工藝類別Id清單 */
  manufMethodIds: number[];

  /** 近N期的資料 */
  lastPeriodCnt?: number;
}
