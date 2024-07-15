export default interface IReqPeriodInfoDTO {
  /** 期數 */
  periodNum: number;

  /** 起始時間 */
  startMonth?: Date;

  /** 結束時間 */
  endMonth?: Date;

  /** 備註 */
  remark: string;
}
