/** 物件比較(用於Table排序) */
export function compare(propertyName: string, order: string) {
  return function (obj1: any, obj2: any): number {
    const value1 = obj1[propertyName];
    const value2 = obj2[propertyName];
    if (typeof value1 === "string" && typeof value2 === "string") {
      const res = value1.localeCompare(value2, "zh");
      return order === "ascending" ? res : -res;
    }

    if (value1 <= value2) {
      return order === "ascending" ? -1 : 1;
    } else {
      return order === "ascending" ? 1 : -1;
    }
  };
}

/** 日期格式化輸入 (yyyy-MM-dd) */
export const dateFormat = (originVal: Date) => {
  const dt = new Date(originVal)
  const y = dt.getFullYear()
  const m = (dt.getMonth() + 1 + '').padStart(2, '0');
  const d = (dt.getDate() + '').padStart(2, '0');
  // const hh = (dt.getHours() + '').padStart(2, '0');
  // const mm = (dt.getMinutes() + '').padStart(2, '0');
  // const ss = (dt.getSeconds() + '').padStart(2, '0');
  // return `${y}/${m}/${d} ${hh}:${mm}:${ss}`
  return `${y}-${m}-${d}`
}

/** 日期與時間格式化輸入 (yyyy-MM-dd HH:mm:ss) */
export const dateTimeFormat = (originVal: Date) => {
  const dt = new Date(originVal)
  const y = dt.getFullYear()
  const m = (dt.getMonth() + 1 + '').padStart(2, '0');
  const d = (dt.getDate() + '').padStart(2, '0');
  const hh = (dt.getHours() + '').padStart(2, '0');
  const mm = (dt.getMinutes() + '').padStart(2, '0');
  const ss = (dt.getSeconds() + '').padStart(2, '0');
  return `${y}/${m}/${d} ${hh}:${mm}:${ss}`
}

/** 月份格式化輸入 (yyyy-MM) */
export const monthFormat = (originVal: Date) => {
  const dt = new Date(originVal)
  const y = dt.getFullYear()
  const m = (dt.getMonth() + 1 + '').padStart(2, '0');
  // const d = (dt.getDate() + '').padStart(2, '0');
  // const hh = (dt.getHours() + '').padStart(2, '0');
  // const mm = (dt.getMinutes() + '').padStart(2, '0');
  // const ss = (dt.getSeconds() + '').padStart(2, '0');
  // return `${y}/${m}/${d} ${hh}:${mm}:${ss}`
  return `${y}-${m}`
}

/** 月份格式化輸入 (yyyy-MM) */
export const monthNoSymbolFormat = (originVal: Date) => {
  const dt = new Date(originVal)
  const y = dt.getFullYear()
  const m = (dt.getMonth() + 1 + '').padStart(2, '0');
  // const d = (dt.getDate() + '').padStart(2, '0');
  // const hh = (dt.getHours() + '').padStart(2, '0');
  // const mm = (dt.getMinutes() + '').padStart(2, '0');
  // const ss = (dt.getSeconds() + '').padStart(2, '0');
  // return `${y}/${m}/${d} ${hh}:${mm}:${ss}`
  return `${y}${m}`
}

/** Delay小工具 */
export function delay (time_ms: number) {
  return new Promise((resolve) => setTimeout(resolve, time_ms));
}