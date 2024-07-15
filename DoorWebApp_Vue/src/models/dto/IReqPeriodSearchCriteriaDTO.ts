/** 期數項目搜尋條件 */
export default interface IReqPeriodSearchCriteriaDTO {
  /** 期數 */
  periodNum?: number;

  /** 建立者 */
  creatorKeyword?: string;

  /** 起始時間 */
  startDate?: Date;

  /** 結束時間 */
  endDate?: Date;
}
