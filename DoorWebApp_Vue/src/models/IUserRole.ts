export default interface IUserRole {
  userId: number;
  username: string;
  displayName: string;
  email: string;

  /** Checkbox多選綁定用 */
  roleNames: string[];
}
