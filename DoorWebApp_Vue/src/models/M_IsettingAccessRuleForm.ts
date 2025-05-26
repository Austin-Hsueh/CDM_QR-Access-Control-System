export interface M_IsettingAccessRuleForm {
  Id?: number;
  userId: number;
  datepicker?: string[];
  timepicker?: string[];
  days: number[];
  groupIds: number[];
  datefrom?: string;
  dateto?: string;
  timefrom?: string;
  timeto?: string;
  IsDelete?: boolean;
  displayName?: string;
  courseId?: number;
  teacherId?: number;
}
