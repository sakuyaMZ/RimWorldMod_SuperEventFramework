using System.Collections.Generic;
using Verse;

namespace SuperEventFramework
{
    /// <summary>
    /// 游戏组件类，继承自GameComponent
    /// 用于保存每个存档的特定状态（PerSaveOnce模式）
    /// GameComponent会在存档时自动保存，读档时自动恢复
    /// </summary>
    public class SuperEventGameComponent : GameComponent
    {
        public static SuperEventGameComponent Instance => SuperEventManager.CurrentSave?.GetComponent<SuperEventGameComponent>();

        // 当前存档中已触发的事件ID集合
        public HashSet<string> triggeredEventsPerSave = new HashSet<string>();
        
        /// <summary>
        /// 构造函数，GameComponent必须有的构造函数
        /// </summary>
        public SuperEventGameComponent(Game game)
        {
            // 这里可以初始化一些存档特定的数据
        }
        
        /// <summary>
        /// RimWorld的存档/读档回调
        /// 使用Scribe系统来序列化存档特定的数据
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            // 保存当前存档的触发状态
            Scribe_Collections.Look(ref triggeredEventsPerSave, "triggeredEventsPerSave", LookMode.Value);
        }
        
        /// <summary>
        /// 检查事件是否在当前存档中触发过
        /// </summary>
        public bool HasTriggered(string eventId)
        {
            return triggeredEventsPerSave.Contains(eventId);
        }
        
        /// <summary>
        /// 标记事件在当前存档中已触发
        /// </summary>
        public void MarkAsTriggered(string eventId)
        {
            triggeredEventsPerSave.Add(eventId);
        }

        /// <summary>
        /// 标记事件在当前存档中未触发
        /// </summary>
        public void MarkAsNotTriggered(string eventId)
        {
            triggeredEventsPerSave.Remove(eventId);
        }

        /// <summary>
        /// 清除当前存档中所有触发状态
        /// </summary>
        public void ClearTriggeredEvents()
        {
            triggeredEventsPerSave.Clear();
        }
    }
}