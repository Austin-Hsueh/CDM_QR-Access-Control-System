export default interface IReqReportPerformanceSearchCriteriaDTO {
  /** 當前頁數 */
  page: number;

  /** 每頁個數 */
  pageSize: number;

  /** 排序欄位名稱 */
  sortColName?: string;

  /** 排序方向 */
  sortOrder?: string;

  /** 近N期的資料 */
  lastPeriodCnt?: number;

  /** 廠區Id */
  siteId?: number;

  /** 工廠Id */
  factoryId?: number;

  /** 專案代碼關鍵字 */
  projectCodeKeyword?: string;

  /** 改善料號關鍵字 */
  pnKeyword?: string;
}
