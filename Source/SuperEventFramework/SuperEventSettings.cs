using System.Collections.Generic;
using Verse;

namespace SuperEventFramework
{
    /// <summary>
    /// Mod设置类，继承自ModSettings
    /// 用于保存玩家的设置和全局状态
    /// RimWorld会自动序列化/反序列化这个类到配置文件
    /// </summary>
    public class SuperEventSettings : ModSettings
    {
        // 全局已触发的事件ID集合
        public HashSet<string> triggeredEventsGlobal = new HashSet<string>();
        
        // 玩家的事件选择：key=triggerEventId, value=选择的SuperEventDef.defName
        public Dictionary<string, string> playerEventChoices = new Dictionary<string, string>();
        
        // 玩家的触发模式覆盖：key=SuperEventDef.defName, value=玩家选择的触发模式
        public Dictionary<string, SuperEventDef.TriggerMode> playerTriggerModes = new Dictionary<string, SuperEventDef.TriggerMode>();

        // 玩家的超事件音量覆盖：key=SuperEventDef.defName, value=玩家选择的音量
        public Dictionary<string, float> superEventVolumes = new Dictionary<string, float>();

        // 玩家强制设置不触发的事件ID集合
        public HashSet<string> blockedEvents = new HashSet<string>();
        
        // 是否在关闭事件窗口时停止背景音乐
        public bool stopBGMOnClose = true;
        
        // 是否全局屏蔽所有超事件触发（默认不屏蔽）
        public bool globalBlocked = false;
        
        /// <summary>
        /// RimWorld的存档/读档系统回调方法
        /// 使用Scribe系统来序列化数据
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            
            // 保存全局触发状态
            Scribe_Collections.Look(ref triggeredEventsGlobal, "triggeredEventsGlobal", LookMode.Value);
            
            // 保存玩家的事件选择
            Scribe_Collections.Look(ref playerEventChoices, "playerEventChoices", LookMode.Value, LookMode.Value);
            
            // 保存玩家的触发模式覆盖
            Scribe_Collections.Look(ref playerTriggerModes, "playerTriggerModes", LookMode.Value, LookMode.Value);

            // 保存玩家的超事件音量覆盖
            Scribe_Collections.Look(ref superEventVolumes, "superEventVolumes", LookMode.Value, LookMode.Value);
            
            // 保存玩家强制设置不触发的事件ID集合
            Scribe_Collections.Look(ref blockedEvents, "blockedEvents", LookMode.Value);
            
            // 保存BGM设置
            Scribe_Values.Look(ref stopBGMOnClose, "stopBGMOnClose", true);
            
            // 保存全局屏蔽设置
            Scribe_Values.Look(ref globalBlocked, "globalBlocked", false);
        }
        
        /// <summary>
        /// 检查事件是否已在全局范围内触发过
        /// </summary>
        public bool HasTriggeredGlobally(string eventId)
        {
            return triggeredEventsGlobal.Contains(eventId);
        }
        
        /// <summary>
        /// 标记事件已在全局范围内触发
        /// </summary>
        public void MarkAsTriggeredGlobally(string eventId)
        {
            triggeredEventsGlobal.Add(eventId);
        }
        
        /// <summary>
        /// 标记事件未在全局范围内触发
        /// </summary>
        public void MarkAsNotTriggeredGlobally(string eventId)
        {
            triggeredEventsGlobal.Remove(eventId);
        }

        /// <summary>
        /// 清除全局触发状态
        /// </summary>
        public void ClearTriggeredEventsGlobally()
        {
            triggeredEventsGlobal.Clear();
        }
    }
}