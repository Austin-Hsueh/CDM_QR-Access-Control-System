export function formatDate(date: Date): string {
  const year = date.getFullYear();
  const month = ('0' + (date.getMonth() + 1)).slice(-2); // 月份从0开始，所以要加1
  const day = ('0' + date.getDate()).slice(-2);
  return `${year}-${month}-${day}`;
}

// 格式化时间为 HH:MM
export function formatTime(date: Date): string {
  const hours = ('0' + date.getHours()).slice(-2);
  const minutes = ('0' + date.getMinutes()).slice(-2);
  return `${hours}:${minutes}`;
}