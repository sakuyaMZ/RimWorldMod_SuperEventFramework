using HarmonyLib;
using RimWorld;
using Verse;

namespace SuperEventFramework.HarmonyPatches
{
    /// <summary>
    /// Harmony补丁类 - 监听游戏信件事件
    /// 这个补丁会拦截RimWorld中LetterStack.ReceiveLetter方法的调用
    /// </summary>
    [HarmonyPatch(typeof(LetterStack), "ReceiveLetter")] // 指定要修补的类和方法
    public static class LetterStackPatch
    {
        /// <summary>
        /// 前缀补丁（Prefix） - 在原方法执行前运行
        /// </summary>
        /// <param name="let">游戏发出的信件对象</param>
        /// <returns>
        /// true: 继续执行原方法和其他补丁
        /// false: 跳过原方法和其他补丁（不要轻易返回false）
        /// </returns>
        [HarmonyPrefix]
        public static bool Prefix(Letter let)
        {
            // 安全检查：如果信件为空，直接返回让其他逻辑继续
            if (let == null)
                return true;
            
            // 核心逻辑：尝试触发超事件
            // 这里会检查信件是否匹配任何配置的超事件条件
            SuperEventManager.TryTriggerEvent(let);
            
            // 重要：必须返回true，让其他Mod的补丁和游戏原方法继续执行
            // 返回false会阻止其他Mod的补丁，可能导致兼容性问题
            return true;
        }
    }
}