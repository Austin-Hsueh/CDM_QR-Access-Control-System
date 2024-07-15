export default interface IAPIResponse<T> {
  result: number,
  msg: string,
  content: T,
}
