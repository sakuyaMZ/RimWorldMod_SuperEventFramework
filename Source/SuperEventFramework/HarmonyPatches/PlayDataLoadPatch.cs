using HarmonyLib;
using Verse;

namespace SuperEventFramework.HarmonyPatches
{
    /// <summary>
    /// 游戏加载时初始化超事件管理器
    /// 切换语言后必然销毁全部的def且会重新走LoadAllPlayData
    /// </summary>
    [HarmonyPatch(typeof(PlayDataLoader), "DoPlayLoad")]
    public static class PlayDataLoadPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SuperEventManager.Initialize();
        }
    }
}
