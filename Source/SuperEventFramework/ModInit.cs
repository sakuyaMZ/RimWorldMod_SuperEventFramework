using Verse;

namespace SuperEventFramework
{
    /// <summary>
    /// Mod 初始化入口，在所有 Mod 实例创建完毕后执行。
    /// 
    /// [StaticConstructorOnStartup] 由 RimWorld 的 StaticConstructorOnStartupUtility.CallAll() 调用，
    /// 该调用发生在 PlayDataLoader 的 LongEventHandler.ExecuteWhenFinished 回调中，
    /// 此时所有 Mod 的 Activator.CreateInstance 已完成，DefDatabase 已填充，GetMod<T>() 可用。
    /// 
    /// 之所以不用 SuperEventMod 自身的静态构造函数：CLR 在 Activator.CreateInstance 时就会触发
    /// SuperEventMod 的 .cctor()，而此时 runningModClasses 尚未赋值，GetMod<T>() 返回 null。
    /// 新建一个独立的空类挂 [StaticConstructorOnStartup] 可以绕过 CLR 的提前触发，
    /// 让 CallAll() 在正确的时机首次调用此静态构造函数。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ModInit
    {
        /// <summary>
        /// 所有 Mod 实例创建完毕、Def 加载完成后执行。
        /// 此时 LoadedModManager.GetMod<T>() 和 DefDatabase<T>.GetNamed() 均可安全调用。
        /// </summary>
        static ModInit()
        {
            SuperEventManager.CanInitialized = true;
            SuperEventManager.Initialize();
        }
    }
}
