import { M_ICourseTypeOptions }  from '@/models/M_ICourseTypeOptions'
export interface M_ICourseOptions extends Pick<M_ICourseTypeOptions, 'courseTypeId'> {
  courseId?: number;
  courseName: string;
  IsDelete?: boolean;
  category?: string;
  sortOrder?: number;
  feeCode?: string;
  amount?: number;
  materialFee?: number;
  hours?: number;
  splitRatio?: number;
  openCourseAmount?: number;
  remark?: string;
}
