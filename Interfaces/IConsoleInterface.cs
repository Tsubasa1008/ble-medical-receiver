using System.Threading;
using System.Threading.Tasks;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Interfaces
{
    /// <summary>
    /// 控制台界面接口
    /// </summary>
    public interface IConsoleInterface
    {
        /// <summary>
        /// 顯示歡迎信息
        /// </summary>
        /// <returns>顯示任務</returns>
        Task DisplayWelcomeAsync();

        /// <summary>
        /// 顯示醫療數據
        /// </summary>
        /// <param name="data">要顯示的數據</param>
        /// <returns>顯示任務</returns>
        Task DisplayDataAsync(MedicalData data);

        /// <summary>
        /// 顯示狀態信息
        /// </summary>
        /// <param name="status">狀態信息</param>
        /// <returns>顯示任務</returns>
        Task DisplayStatusAsync(string status);

        /// <summary>
        /// 顯示錯誤信息
        /// </summary>
        /// <param name="error">錯誤信息</param>
        /// <returns>顯示任務</returns>
        Task DisplayErrorAsync(string error);

        /// <summary>
        /// 處理用戶輸入
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>處理任務</returns>
        Task HandleUserInputAsync(CancellationToken cancellationToken);
    }
}