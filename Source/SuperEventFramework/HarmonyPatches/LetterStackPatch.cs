using HarmonyLib;
using RimWorld;
using Verse;

namespace SuperEventFramework.HarmonyPatches
{
    /// <summary>
    /// Harmony补丁类 - 监听游戏信件事件
    /// 这个补丁会拦截RimWorld中LetterStack.ReceiveLetter方法的调用
    /// 拦截 LetterStack.ReceiveLetter(Letter, string, int, bool) 重载。
    /// 
    /// LetterStack.ReceiveLetter 共有 3 个重载：
    ///   1. ReceiveLetter(TaggedString label, TaggedString text, LetterDef, LookTargets, ...)
    ///      工厂方法，内部创建 Letter 对象后调用重载 3。
    ///   2. ReceiveLetter(TaggedString label, TaggedString text, LetterDef, string, int, bool)
    ///      同上，工厂方法，创建 Letter 后调用重载 3。
    ///   3. ReceiveLetter(Letter let, string debugInfo, int delayTicks, bool playSound)
    ///      最终处理入口，负责播放音效、暂停、加入信件堆栈等。所有调用路径最终汇集于此。
    /// 
    /// 只 Patch 重载 3 即可覆盖所有信件来源：
    ///   - 通过 Storyteller 触发的 Incident 信件
    ///   - 通过 Quest 系统触发的任务信件
    ///   - 其他 Mod 直接调用 LetterMaker.MakeLetter + ReceiveLetter 的信件
    ///   - 精神崩溃、死亡通知、派系关系变化等各种游戏内信件
    /// </summary>
    [HarmonyPatch(typeof(LetterStack), nameof(LetterStack.ReceiveLetter), typeof(Letter), typeof(string), typeof(int), typeof(bool))]
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