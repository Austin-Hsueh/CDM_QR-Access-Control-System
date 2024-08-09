export default interface CreateStudentPermission {
  userId: number;
  datefrom: string;
  dateto: string;
  timefrom: string;
  timeto: string;
  days: number[];
  groupIds: number[];
}