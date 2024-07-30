export interface M_IsettingAccessRuleForm {
  userId: number;
  datepicker: string;
  timepicker: string;
  days: number[];
  groupIds: number[];
  datefrom?: string;
  dateto?: string;
  timefrom?: string;
  timeto?: string;
}
