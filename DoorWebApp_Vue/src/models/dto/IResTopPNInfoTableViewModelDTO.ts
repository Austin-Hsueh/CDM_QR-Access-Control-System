export default interface IResTopPNInfoTableViewModelDTO {
  id: number;

  /** 最上階料號(成品階)，來自ERP(V_TPS_BOM_EXPLOSIONS) */
  topPN: string;

  /** 期數 */
  periodNum: number;

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

  /** 建立時間 */
  createTime: string;
}
