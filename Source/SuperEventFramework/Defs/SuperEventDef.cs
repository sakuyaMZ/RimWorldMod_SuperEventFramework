using Verse;

namespace SuperEventFramework
{
    /// <summary>
    /// 超事件定义类，继承自RimWorld的Def基类
    /// Def是RimWorld中所有可定义内容的基础类，会被XML系统自动加载
    /// </summary>
    public class SuperEventDef : Def
    {
        public string triggerEventId;   // 触发事件的ID，对应游戏内信件的defName
        public string imagePath;        // 事件图片路径，支持PNG/JPG格式
        public string bgmPath;          // 背景音乐路径，支持OGG/WAV格式
        public string title;            // 事件标题
        public string desc;             // 事件描述
        public string btnText;          // 按钮文本

        // 默认触发模式，默认为每个存档仅触发一次
        public TriggerMode defaultTriggerMode = TriggerMode.PerSaveOnce;
        
        // 可选：依赖的Mod PackageId或DLC ID，如果指定则只有该Mod/DLC存在时才会加载此事件
        // 如果为空则无视依赖关系，事件始终会加载到DefDatabase中
        // 注意：加载到DefDatabase不代表一定会触发，触发还取决于其他条件（如触发模式、玩家选择等）
        public string requiredModOrDlc;
        
        /// <summary>
        /// 触发模式枚举
        /// </summary>
        public enum TriggerMode
        {
            Unlimited,      // 无限次触发
            PerSaveOnce,    // 每个存档仅触发一次
            GlobalOnce,     // 全局仅触发一次（所有存档）
            None           // 从不触发，仅用于显示
        }
    }
}