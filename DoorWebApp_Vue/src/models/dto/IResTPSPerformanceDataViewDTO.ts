export default interface IResTPSPerformanceDataViewDTO {
  implementDay: string;
  siteName: string;
  factoryName: string;
  customerName: string;

  periodNum: number;
  startMonth: string;
  endMonth: string;

  projectCode: string;

  /** 最上階料號 */
  topPN: string;

  /** 工資率 */
  workRate: string;

  /** 改善前面積(平方公尺) */
  areaBeforeImp: string;

  /** 改善前製程L/T(日) */
  procLTBeforeImp: string;

  /** 改善前供應商L/T(日) */
  venderLTBeforeImp: string;

  /** 改善前PPH(個) */
  pphBeforeImp: string;

  /** 改善後面積(平方公尺) */
  areaAfterImp: string;

  /** 改善後供應商L/T(日) */
  venderLTAfterImp: string;

  /** 改善後製程L/T(日) */
  procLTAfterImp: string;

  /** 改善後PPH(個) */
  pphAfterImp: string;

  /** 原系統標準工時 */
  stdProcTimeOrg: number;

  /** 系統標準工時 */
  stdProcTime: number;

  /** 改善工時(sec, 原系統標工(stdProcTimeOrg)-系統標工(stdProcTime)) */
  stdProcTimeDiff: number;

  /** 系統標準工時改善比例 (改善工時(stdProcTimeDiff)/系統標工(stdProcTime)) */
  stdProcTimeDiffRatio: number;

  /** 材料總成本 */
  materialCosts: string;

  /** 入庫數量 */
  stockInNum: number;

  /** 總報工工時(hr) */
  totalWorkHour: number;

  /** 實際PPH */
  currentPPH: number;

  /** 製程L/T */
  procLT: number;

  /** 工時改善效益 */
  workHourPerf: number;

  /** PPH效益 */
  pphPerf: number;

  /** 製程L/T減少效益 */
  ltPerf: number;

  /** 供應商L/T減少效益 */
  venderLTPref: number;

  /** 碳排放 */
  co2Emission: number;
}
