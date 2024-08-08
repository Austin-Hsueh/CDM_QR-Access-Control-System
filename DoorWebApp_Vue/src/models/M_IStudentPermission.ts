export default interface StudentPermission {
  id: number;
  userId: number;
  datefrom: string;
  dateto: string;
  timefrom: string;
  timeto: string;
  days: number[];
  groupIds: number[];
}
