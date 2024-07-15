/** 專案項目搜尋條件 */
export default interface IReqProjectSearchCriteriaDTO {
  /** 專案代碼 */
  pjcode?: string;

  /** 所選廠區Id */
  siteId?: number;

  /** 所選工廠Id */
  factoryId?: number;

  /** 建立者 */
  creator?: string;

  /** 起始時間 */
  startDate?: Date;

  /** 結束時間 */
  endDate?: Date;

  /** 包含此時間點的專案 */
  includeDate?: Date;
}
