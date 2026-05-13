using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System.EnterpriseServices;

namespace SuperEventFramework
{
    /// <summary>
    /// 超事件管理器 - 核心逻辑处理类
    /// 静态类，负责事件的查找、触发判断和实际触发
    /// </summary>
    public static class SuperEventManager
    {
        // 事件查找表：key=triggerEventId, value=对应的SuperEventDef列表
        private static Dictionary<string, List<SuperEventDef>> eventLookup = new Dictionary<string, List<SuperEventDef>>();
        
        // 事件查找表的本地化翻译匹配，即信件title匹配到本地化后得到事件的key
        private static Dictionary<string, List<string>> eventLookupTranslate = new Dictionary<string, List<string>>();

        // Mod设置实例
        private static SuperEventSettings settings;
        
        // 游戏组件实例（用于PerSaveOnce模式）
        private static SuperEventGameComponent gameComponent;
        
        // 全局设置定义
        private static SuperEventSettingsDef settingsDef;

        /// <summary>
        /// 全局设置定义的静态访问器
        /// </summary>
        public static SuperEventSettingsDef SettingsDef => settingsDef;

        /// <summary>
        /// 避免初次加载时重复初始化
        /// PlayDataLoadPatch会调用一次Initialize，然后才是ModInit的调用，所以加个标记
        /// </summary>
        public static bool CanInitialized = false; // 是否可以初始化
        
        /// <summary>
        /// 初始化管理器，在游戏加载时调用
        /// </summary>
        public static void Initialize()
        {
            if (!CanInitialized)
            {
                return;
            }
            // 构建事件查找表
            BuildEventLookup();

            // 构建事件查找表的翻译版本
            BuildEventLookupTranslate();
            
            // 获取Mod设置
            settings = LoadedModManager.GetMod<SuperEventMod>().GetSettings<SuperEventSettings>();
            
            // 获取全局设置定义
            settingsDef = DefDatabase<SuperEventSettingsDef>.GetNamed("SuperEventFramework_Settings");
        }
        
        /// <summary>
        /// 构建事件查找表，将事件按triggerEventId分组
        /// </summary>
        private static void BuildEventLookup()
        {
            eventLookup.Clear();
            
            // 遍历所有已加载的SuperEventDef
            foreach (var def in DefDatabase<SuperEventDef>.AllDefs)
            {
                // 按triggerEventId分组
                if (!eventLookup.TryGetValue(def.triggerEventId, out var list))
                {
                    list = new List<SuperEventDef>();
                    eventLookup[def.triggerEventId] = list;
                }
                list.Add(def);
            }

            // 移除示例事件
            eventLookup.Remove("ExampleEventId");
        }

        /// <summary>
        /// 构建事件查找表的翻译版本
        /// </summary>
        private static void BuildEventLookupTranslate()
        {
            eventLookupTranslate.Clear();
            
            // 遍历eventLookup
            foreach (var pair in eventLookup)
            {
                string translste = pair.Key.Translate();
                if (!eventLookupTranslate.TryGetValue(translste, out var list))
                {
                    list = new List<string>();
                    eventLookupTranslate[translste] = list;
                }
                list.Add(pair.Key);
            }
        }
        
        /// <summary>
        /// 尝试触发事件 - 这是Harmony补丁调用的主要方法
        /// </summary>
        /// <param name="letter">游戏发出的信件</param>
        public static void TryTriggerEvent(Letter letter)
        {
            // 如果全局屏蔽所有事件，直接返回
            if (settings.globalBlocked)
                return;

            if (letter == null || letter.def == null)
                return;
            
            string eventId = letter.Label;
            
            // 先匹配本地化后的文本
            if(eventLookupTranslate.TryGetValue(eventId, out var defNames))
            {
                eventId = defNames[^1];//[^1]表示最后一个元素
                if (eventId.NullOrEmpty())
                    return;
                // 查找是否有匹配的超事件定义
                if (eventLookup.TryGetValue(eventId, out var eventDefs) && eventDefs.Count > 0)
                {
                    // 选择要触发的事件定义
                    var selectedDef = SelectEventDef(eventId, eventDefs);
                    
                    // 检查是否可以触发
                    if (selectedDef != null && CanTrigger(selectedDef))
                    {
                        // 实际触发事件
                        TriggerEvent(selectedDef);
                    }
                }
            }
        }
        
        /// <summary>
        /// 选择要触发的事件定义
        /// </summary>
        private static SuperEventDef SelectEventDef(string eventId, List<SuperEventDef> defs)
        {
            // 优先使用玩家选择
            if (settings.playerEventChoices.TryGetValue(eventId, out var chosenDefName))
            {
                return defs.Find(d => d.defName == chosenDefName);
            }
            
            // 默认使用最后加载的（Mod加载顺序中排在最后的）
            // 这样后安装的Mod可以覆盖前面Mod的事件
            return defs[^1];
        }
        
        /// <summary>
        /// 检查事件是否可以触发（根据触发模式）
        /// </summary>
        private static bool CanTrigger(SuperEventDef def)
        {
            // 如果事件被个别屏蔽，不可触发
            if (settings.blockedEvents.Contains(def.triggerEventId))
                return false;
            
            // 获取实际的触发模式（玩家覆盖或默认）
            var triggerMode = GetCurrentTriggerMode(def);
            
            // 根据不同的触发模式进行检查
            switch (triggerMode)
            {
                case SuperEventDef.TriggerMode.Unlimited:
                    return true; // 无限次，总是可以触发

                case SuperEventDef.TriggerMode.None:
                    return false; // 不触发
                    
                case SuperEventDef.TriggerMode.PerSaveOnce:
                    // 每个存档只能触发一次
                    if (gameComponent == null)
                        gameComponent = CurrentSave.GetComponent<SuperEventGameComponent>();
                    return !gameComponent.HasTriggered(def.defName);
                    
                case SuperEventDef.TriggerMode.GlobalOnce:
                    // 全局只能触发一次
                    return !settings.HasTriggeredGlobally(def.defName);

                default:
                    return true;
            }
        }
        
        /// <summary>
        /// 实际触发事件
        /// </summary>
        private static void TriggerEvent(SuperEventDef def)
        {
            // 获取实际的触发模式
            var triggerMode = GetCurrentTriggerMode(def);
            
            // 根据触发模式标记为已触发
            switch (triggerMode)
            {
                case SuperEventDef.TriggerMode.PerSaveOnce:
                    if (gameComponent == null)
                        gameComponent = CurrentSave.GetComponent<SuperEventGameComponent>();
                    gameComponent.MarkAsTriggered(def.defName);
                    break;
                    
                case SuperEventDef.TriggerMode.GlobalOnce:
                    settings.MarkAsTriggeredGlobally(def.defName);
                    break;
            }
            
            // 打开事件对话框
            Find.WindowStack.Add(new Dialog_SuperEvent(def));
        }

        public static Texture2D GetEventImage(string imgPath)
        {
            //传入的图片路径为空时，使用默认图片路径
            string path = imgPath.NullOrEmpty() ? SettingsDef.defaultImgPath : imgPath;
            return path.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(path);
        }

        public static AudioClip GetEventBGM(string bgmPath)
        {
            return bgmPath.NullOrEmpty() ? null : ContentFinder<AudioClip>.Get(bgmPath);
        }

        public static float GetEventVolume(string defName)
        {
            if (settings.superEventVolumes.TryGetValue(defName, out float vol))
                return vol;
            return 1f;
        }

        private static GameObject _bgmGameObject;
        private static AudioSource _bgmSource;

        public static void PlayBGM(string bgmPath, float volume)
        {
            var clip = GetEventBGM(bgmPath);
            if (clip == null)
                return;
            if (_bgmGameObject == null)
            {
                _bgmGameObject = new GameObject("SuperEventBGM");
                Object.DontDestroyOnLoad(_bgmGameObject);
                _bgmSource = _bgmGameObject.AddComponent<AudioSource>();
            }
            _bgmSource.clip = clip;
            _bgmSource.volume = volume;
            _bgmSource.Play();
        }

        public static void StopBGM()
        {
            _bgmSource?.Stop();
        }

        /// <summary>
        /// 获取事件分组（用于设置界面）
        /// </summary>
        public static Dictionary<string, List<SuperEventDef>> GetEventGroups()
        {
            return eventLookup;
        }

        /// <summary>
        /// 获取当前触发模式（考虑玩家覆盖）
        /// </summary>
        public static SuperEventDef.TriggerMode GetCurrentTriggerMode(SuperEventDef eventDef)
        {
            if (settings.playerTriggerModes.TryGetValue(eventDef.defName, out var mode))
                return mode;
            return eventDef.defaultTriggerMode;
        }

        /// <summary>
        /// 获取触发模式的显示文本
        /// </summary>
        public static string GetTriggerModeText(SuperEventDef.TriggerMode mode)
        {
            switch (mode)
            {
                case SuperEventDef.TriggerMode.Unlimited:
                    return "SuperEventFramework.TriggerMode_Unlimited".Translate();
                case SuperEventDef.TriggerMode.PerSaveOnce:
                    return "SuperEventFramework.TriggerMode_PerSaveOnce".Translate();
                case SuperEventDef.TriggerMode.GlobalOnce:
                    return "SuperEventFramework.TriggerMode_GlobalOnce".Translate();
                case SuperEventDef.TriggerMode.None:
                    return "SuperEventFramework.TriggerMode_None".Translate();
                default:
                    return mode.ToString();
            }
        }

        /// <summary>
        /// 检查事件是否在当前存档中触发过
        /// </summary>
        public static bool HasTriggeredInSave(string triggerEventId)
        {
            if (gameComponent == null)
                gameComponent = CurrentSave?.GetComponent<SuperEventGameComponent>();
            
            return gameComponent?.HasTriggered(triggerEventId) ?? false;
        }

        /// <summary>
        /// 切换存档中的触发状态
        /// </summary>
        public static void ToggleSaveTriggerState(string triggerEventId, bool hasTriggered)
        {
            if (gameComponent == null)
                gameComponent = CurrentSave?.GetComponent<SuperEventGameComponent>();
            
            if (gameComponent != null)
            {
                if (hasTriggered)
                    gameComponent.MarkAsNotTriggered(triggerEventId);
                else
                    gameComponent.MarkAsTriggered(triggerEventId);
            }
        }

        public static void ToggleGlobalTriggerState(string triggerEventId, bool hasTriggered)
        {
            if (hasTriggered)
                settings.MarkAsNotTriggeredGlobally(triggerEventId);
            else
                settings.MarkAsTriggeredGlobally(triggerEventId);
        }

        /// <summary>
        /// 测试触发事件（不记录触发状态）
        /// </summary>
        public static void TestTriggerEvent(SuperEventDef eventDef)
        {
            // 直接打开事件对话框，不进行触发检查
            Find.WindowStack.Add(new Dialog_SuperEvent(eventDef));
        }

        public static bool CheckSuperEventSelected(string eventId, string superEventId)
        {
            if (settings.playerEventChoices.TryGetValue(eventId, out string chosenDefName))
                return chosenDefName == superEventId;
            
            if (eventLookup.TryGetValue(eventId, out var defs) && defs.Count > 0)
                return defs[^1].defName == superEventId;
            
            return false;
        }


        /// <summary>
        /// 获取超事件文本的翻译（考虑玩家覆盖）
        /// </summary>
        public static string GetSuperEventTextTranslate(string text)
        {
            if (text.NullOrEmpty())
                return "";
            
            if (text.TryTranslate(out var result))
                return result.NullOrEmpty() ? "" : result;
            
            return text;
        }

        /// <summary>
        /// 获取Mod或DLC的名称（考虑玩家覆盖）
        /// </summary>
        public static string GetModOrDlcName(string packageId)
        {
            if (packageId.NullOrEmpty())
                return "";
            
            ModMetaData mod = ModLister.GetActiveModWithIdentifier(packageId);
            return mod?.Name ?? packageId;
        }

        /// <summary>
        /// 获取当前存档
        /// </summary>
        public static Game CurrentSave => Current.Game;
    }
}