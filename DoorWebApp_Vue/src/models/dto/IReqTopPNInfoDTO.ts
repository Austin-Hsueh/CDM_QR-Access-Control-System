export default interface IReqTopPNInfoDTO {
  /** 最上階料號(成品階)，來自ERP(V_TPS_BOM_EXPLOSIONS) */
  topPN: string;

  /** 期數 */
  periodNum: number;
  
  /** 改善前面積(平方公尺) */
  areaBeforeImp?: number;

  /** 改善前製程L/T(日) */
  procLTBeforeImp?: number;

  /** 改善前供應商L/T(日) */
  venderLTBeforeImp?: number;

  /** 改善前PPH(個) */
  pphBeforeImp?: number;

  /** 改善後面積(平方公尺) */
  areaAfterImp?: number;

  /** 改善後供應商L/T(日) */
  venderLTAfterImp?: number;

  /** 改善後製程L/T(日) */
  procLTAfterImp?: number;

  /** 改善後PPH(個) */
  pphAfterImp?: number;
}
