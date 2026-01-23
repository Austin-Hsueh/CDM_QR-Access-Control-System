using DoorDB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DoorWebApp.Services
{
    /// <summary>
    /// 統一管理 Payment 和 Refund 的收據編號生成
    /// 確保兩者單號順序一致且不重複
    /// </summary>
    public class ReceiptNumberService
    {
        private readonly DoorDbContext ctx;
        private static readonly object _lock = new object();

        public ReceiptNumberService(DoorDbContext ctx)
        {
            this.ctx = ctx;
        }

        /// <summary>
        /// 產生收據編號：{年次:03}B{月份:02}{編號:04}
        /// 例如：114B060091
        /// Payment 和 Refund 共用同一序列，互相檢查以確保單號唯一且連續
        /// </summary>
        public async Task<string> GenerateReceiptNumber()
        {
            // 使用鎖確保在高並發情況下單號不會重複
            lock (_lock)
            {
                return GenerateReceiptNumberInternal().GetAwaiter().GetResult();
            }
        }

        private async Task<string> GenerateReceiptNumberInternal()
        {
            var now = DateTime.Now;
            int rocYear = now.Year - 1911; // 民國年
            string yearMonth = $"{rocYear:000}B{now.Month:00}";

            // 同時查詢 Payment 和 Refund 表中本月的最大編號
            var lastPaymentReceipt = await ctx.TblPayment
                .Where(p => !p.IsDelete && p.ReceiptNumber != null && p.ReceiptNumber.StartsWith(yearMonth))
                .OrderByDescending(p => p.ReceiptNumber)
                .Select(p => p.ReceiptNumber)
                .FirstOrDefaultAsync();

            var lastRefundReceipt = await ctx.TblRefund
                .Where(r => !r.IsDelete && r.ReceiptNumber != null && r.ReceiptNumber.StartsWith(yearMonth))
                .OrderByDescending(r => r.ReceiptNumber)
                .Select(r => r.ReceiptNumber)
                .FirstOrDefaultAsync();

            // 從兩個表中找出最大的編號
            int maxNumber = 0;

            if (!string.IsNullOrEmpty(lastPaymentReceipt) && lastPaymentReceipt.Length >= 10)
            {
                if (int.TryParse(lastPaymentReceipt.Substring(6, 4), out int paymentNumber))
                {
                    maxNumber = Math.Max(maxNumber, paymentNumber);
                }
            }

            if (!string.IsNullOrEmpty(lastRefundReceipt) && lastRefundReceipt.Length >= 10)
            {
                if (int.TryParse(lastRefundReceipt.Substring(6, 4), out int refundNumber))
                {
                    maxNumber = Math.Max(maxNumber, refundNumber);
                }
            }

            int nextNumber = maxNumber + 1;

            return $"{yearMonth}{nextNumber:0000}";
        }

        /// <summary>
        /// 驗證收據編號格式是否正確
        /// </summary>
        /// <param name="receiptNumber">收據編號</param>
        /// <returns>是否有效</returns>
        public bool ValidateReceiptNumber(string receiptNumber)
        {
            if (string.IsNullOrEmpty(receiptNumber) || receiptNumber.Length != 10)
                return false;

            // 格式: {rocYear:000}B{month:02}{number:0000}
            // 例如: 114B060091
            if (receiptNumber[3] != 'B')
                return false;

            // 驗證年份部分 (位置 0-2)
            if (!int.TryParse(receiptNumber.Substring(0, 3), out int year) || year < 100 || year > 999)
                return false;

            // 驗證月份部分 (位置 4-5)
            if (!int.TryParse(receiptNumber.Substring(4, 2), out int month) || month < 1 || month > 12)
                return false;

            // 驗證編號部分 (位置 6-9)
            if (!int.TryParse(receiptNumber.Substring(6, 4), out int number) || number < 1)
                return false;

            return true;
        }

        /// <summary>
        /// 檢查收據編號是否已存在於 Payment 或 Refund 表中
        /// </summary>
        public async Task<bool> IsReceiptNumberExists(string receiptNumber)
        {
            var existsInPayment = await ctx.TblPayment
                .AnyAsync(p => !p.IsDelete && p.ReceiptNumber == receiptNumber);

            if (existsInPayment)
                return true;

            var existsInRefund = await ctx.TblRefund
                .AnyAsync(r => !r.IsDelete && r.ReceiptNumber == receiptNumber);

            return existsInRefund;
        }
    }
}
