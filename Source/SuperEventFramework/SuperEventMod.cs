using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace SuperEventFramework
{
    public class SuperEventMod : Mod
    {
        private static bool harmonyPatched = false;
        
        // 折叠状态——已展开的事件组ID集合
        private HashSet<string> expandedEventGroups = new HashSet<string>();
        private Vector2 scrollPosition = Vector2.zero; //滚动条位置
        
        public SuperEventMod(ModContentPack content) : base(content)
        {
            // 这里可以放置不需要等待其他Mod加载的简单初始化
            if (!harmonyPatched)
            {
                harmonyPatched = true;
                new Harmony("sakuyaMZ.SuperEvent").PatchAll();
            }
        }
        
        #region 设置窗口

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            
            var settings = GetSettings<SuperEventSettings>();
            
            Rect remaining = DrawGlobalSettings(inRect, settings);
            DrawEventSettings(remaining, settings);
        }
        
        /// <summary>
        /// 绘制全局设置
        /// </summary>
        private Rect DrawGlobalSettings(Rect rect, SuperEventSettings settings)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);
            
            // 全局屏蔽开关（默认勾=正常激活）
            listing.Label("SuperEventFramework.GlobalBlocker".Translate() + ":");
            Rect blockRowRect = listing.GetRect(30f);
            WidgetRow blockRow = new WidgetRow(blockRowRect.x, blockRowRect.y, UIDirection.RightThenUp, blockRowRect.width);
            blockRow.Label("SuperEventFramework.GlobalBlockerDesc".Translate(), blockRowRect.width - 40f);
            Texture2D blockIcon = settings.globalBlocked ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex;
            if (blockRow.ButtonIcon(blockIcon, "SuperEventFramework.ToggleGlobalBlock".Translate()))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                bool wasBlocked = !settings.globalBlocked;
                settings.globalBlocked = wasBlocked;
                if (wasBlocked)
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                else
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            }
            listing.Gap();
            
            // BGM设置
            Rect bgmRowRect = listing.GetRect(30f);
            WidgetRow bgmRow = new WidgetRow(bgmRowRect.x, bgmRowRect.y, UIDirection.RightThenUp, bgmRowRect.width);
            bgmRow.Label("SuperEventFramework.BgmText".Translate(), bgmRowRect.width - 40f);
            Texture2D bgmIcon = settings.stopBGMOnClose ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
            if (bgmRow.ButtonIcon(bgmIcon, "SuperEventFramework.StopBGMOnCloseDesc".Translate()))
            {
                settings.stopBGMOnClose = !settings.stopBGMOnClose;
                if (settings.stopBGMOnClose)
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                else
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
            }
            
            listing.Gap(12f);
            listing.GapLine();
            
            // 清除本存档记录
            listing.Label("SuperEventFramework.SaveRecords".Translate() + ":");
            Rect clearSaveRowRect = listing.GetRect(30f);
            WidgetRow clearSaveRow = new WidgetRow(clearSaveRowRect.x, clearSaveRowRect.y, UIDirection.RightThenUp, clearSaveRowRect.width);
            clearSaveRow.Label("SuperEventFramework.SaveRecordsDesc".Translate(), clearSaveRowRect.width - 140f);
            if (clearSaveRow.ButtonText("SuperEventFramework.Clear".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "SuperEventFramework.ConfirmClearSaveRecords".Translate(),
                    () =>
                    {
                        if (SuperEventGameComponent.Instance == null)
                        {
                            Messages.Message("SuperEventFramework.NeedInSave".Translate(), MessageTypeDefOf.RejectInput);
                            return;
                        }
                        SuperEventGameComponent.Instance.ClearTriggeredEvents();
                        Messages.Message("SuperEventFramework.SaveRecordsCleared".Translate(), MessageTypeDefOf.NeutralEvent);
                    },
                    destructive: true
                ));
            }
            
            // 清除全局记录
            listing.Label("SuperEventFramework.GlobalRecords".Translate() + ":");
            Rect clearGlobalRowRect = listing.GetRect(30f);
            WidgetRow clearGlobalRow = new WidgetRow(clearGlobalRowRect.x, clearGlobalRowRect.y, UIDirection.RightThenUp, clearGlobalRowRect.width);
            clearGlobalRow.Label("SuperEventFramework.GlobalRecordsDesc".Translate(), clearGlobalRowRect.width - 140f);
            if (clearGlobalRow.ButtonText("SuperEventFramework.Clear".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "SuperEventFramework.ConfirmClearGlobalRecords".Translate(),
                    () =>
                    {
                        settings.ClearTriggeredEventsGlobally();
                        Messages.Message("SuperEventFramework.GlobalRecordsCleared".Translate(), MessageTypeDefOf.NeutralEvent);
                    },
                    destructive: true
                ));
            }
            
            float usedHeight = listing.CurHeight;
            listing.End();
            return new Rect(rect.x, rect.y + usedHeight + 10f, rect.width, rect.height - usedHeight - 10f);
        }
        
        /// <summary>
        /// 绘制事件设置（二级折叠列表）
        /// </summary>
        private void DrawEventSettings(Rect rect, SuperEventSettings settings)
        {
            Widgets.DrawMenuSection(rect);//在给定区域上绘制一个 菜单区块的背景框
            
            var eventGroups = SuperEventManager.GetEventGroups();
            if (eventGroups.Count == 0)
            {
                Widgets.Label(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 30f), "SuperEventFramework.NoEventsAvailable".Translate());
                return;
            }
            
            // 计算视图总高度
            float totalHeight = 0f;
            foreach (var kvp in eventGroups)
            {
                totalHeight += 30f; // 一级行
                if (expandedEventGroups.Contains(kvp.Key))
                    totalHeight += kvp.Value.Count * 30f; // 二级行
            }
            
            Rect viewRect = new Rect(0f, 0f, rect.width - 20f, totalHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            
            // 绘制一级事件行
            float y = 0f;
            foreach (var kvp in eventGroups)
            {
                string eventId = kvp.Key;
                var eventDefs = kvp.Value;
                if (eventDefs.Count == 0) continue;
                
                var firstDef = eventDefs[0];
                var triggerMode = SuperEventManager.GetCurrentTriggerMode(firstDef);
                bool isExpanded = expandedEventGroups.Contains(eventId);
                
                Rect rowRect = new Rect(0f, y, viewRect.width, 28f);
                DrawFirstLevelRow(rowRect, eventId, triggerMode, isExpanded, settings, eventDefs);
                y += 30f;
                
                if (isExpanded)
                {
                    foreach (var subDef in eventDefs)
                    {
                        Rect subRowRect = new Rect(12f, y, viewRect.width - 12f, 28f);
                        DrawSecondLevelRow(subRowRect, eventId, subDef, settings);
                        y += 30f;
                    }
                }
            }
            
            Widgets.EndScrollView();
        }
        
        /// <summary>
        /// 绘制一级事件行
        /// </summary>
        private void DrawFirstLevelRow(Rect rect, string eventId, SuperEventDef.TriggerMode triggerMode,
            bool isExpanded, SuperEventSettings settings, List<SuperEventDef> eventDefs)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            
            // 展开/收起箭头（▶=收起，▼=展开）
            string arrowStr = isExpanded ? "▼" : "▶";
            float arrowWidth = Text.CalcSize(arrowStr).x;
            Rect arrowRect = new Rect(rect.x + 4f, rect.y + 2f, arrowWidth + 8f, 22f);
            if (Widgets.ButtonText(arrowRect, arrowStr))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                if (isExpanded)
                    expandedEventGroups.Remove(eventId);
                else
                    expandedEventGroups.Add(eventId);
            }
            
            float curX = arrowRect.xMax + 4f;
            
            // 事件标识
            string displayText = SuperEventManager.GetTranslate(eventId);

            Rect labelRect = new Rect(curX, rect.y, 280f, 24f);
            Widgets.Label(labelRect, displayText);
            TooltipHandler.TipRegion(labelRect, eventId);//鼠标悬停时显示原始的key
            curX = labelRect.xMax + 10f;
            
            // 存档触发状态
            string saveLabel = "SuperEventFramework.SaveStateLabel".Translate();
            float saveLabelW = Text.CalcSize(saveLabel).x + 4f;
            Rect saveLabelRect = new Rect(curX, rect.y, saveLabelW, 24f);
            Widgets.Label(saveLabelRect, saveLabel);
            curX = saveLabelRect.xMax;
            if (SuperEventManager.CurrentSave != null)
            {
                bool saveTriggered = SuperEventManager.HasTriggeredInSave(eventId);
                Texture2D saveIcon = saveTriggered ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
                Rect saveIconRect = new Rect(curX, rect.y + 2f, 20f, 20f);
                if (Widgets.ButtonImage(saveIconRect, saveIcon))
                {
                    SuperEventManager.ToggleSaveTriggerState(eventId, saveTriggered);
                    if (saveTriggered)
                        SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                    else
                        SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                }
                TooltipHandler.TipRegion(saveIconRect, "SuperEventFramework.ToggleSaveState".Translate());
                curX = saveIconRect.xMax + 20f;
            }
            else
            {
                curX += 44f;
            }
            
            // 全局触发状态
            string globalLabel = "SuperEventFramework.GlobalStateLabel".Translate();
            float globalLabelW = Text.CalcSize(globalLabel).x + 4f;
            Rect globalLabelRect = new Rect(curX, rect.y, globalLabelW, 24f);
            Widgets.Label(globalLabelRect, globalLabel);
            curX = globalLabelRect.xMax;
            bool globalTriggered = settings.HasTriggeredGlobally(eventId);
            Texture2D globalIcon = globalTriggered ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
            Rect globalIconRect = new Rect(curX, rect.y + 2f, 20f, 20f);
            if (Widgets.ButtonImage(globalIconRect, globalIcon))
            {
                SuperEventManager.ToggleGlobalTriggerState(eventId, globalTriggered);
                if (globalTriggered)
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                else
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            }
            TooltipHandler.TipRegion(globalIconRect, "SuperEventFramework.ToggleGlobalState".Translate());
            curX = globalIconRect.xMax + 20f;
            
            // 屏蔽状态
            string blockLabel = "SuperEventFramework.BlockLabel".Translate();
            float blockLabelW = Text.CalcSize(blockLabel).x + 4f;
            Rect blockLabelRect = new Rect(curX, rect.y, blockLabelW, 24f);
            Widgets.Label(blockLabelRect, blockLabel);
            curX = blockLabelRect.xMax;
            bool isBlocked = settings.blockedEvents.Contains(eventId);
            Texture2D blockIcon = isBlocked ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex;
            Rect blockIconRect = new Rect(curX, rect.y + 2f, 20f, 20f);
            if (Widgets.ButtonImage(blockIconRect, blockIcon))
            {
                if (isBlocked)
                {
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                    settings.blockedEvents.Remove(eventId);
                }
                else
                {
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                    settings.blockedEvents.Add(eventId);
                }
            }
            TooltipHandler.TipRegion(blockIconRect, "SuperEventFramework.ToggleBlock".Translate());
        }
        
        /// <summary>
        /// 绘制二级超事件行
        /// </summary>
        private void DrawSecondLevelRow(Rect rect, string eventId, SuperEventDef subDef, SuperEventSettings settings)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            
            WidgetRow row = new WidgetRow(rect.x, rect.y, UIDirection.RightThenUp, rect.width, 2f);
            
            // 超事件标题
            row.Label(subDef.title.Translate(), 160f);
            
            // 选择按钮
            bool isSelected = SuperEventManager.CheckSuperEventSelected(eventId, subDef.defName);
            Texture2D selectIcon = isSelected ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
            if (row.ButtonIcon(selectIcon, "SuperEventFramework.SelectEvent".Translate()))
            {
                if (!isSelected)
                {
                     settings.playerEventChoices[eventId] = subDef.defName;
                     SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                }
                else
                {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }   
            }
            
            // 触发模式
            var subTriggerMode = SuperEventManager.GetCurrentTriggerMode(subDef);
            if (row.ButtonText(SuperEventManager.GetTriggerModeText(subTriggerMode)))
            {
                //SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption(SuperEventManager.GetTriggerModeText(SuperEventDef.TriggerMode.Unlimited), () =>
                        settings.playerTriggerModes[subDef.defName] = SuperEventDef.TriggerMode.Unlimited),
                    new FloatMenuOption(SuperEventManager.GetTriggerModeText(SuperEventDef.TriggerMode.PerSaveOnce), () =>
                        settings.playerTriggerModes[subDef.defName] = SuperEventDef.TriggerMode.PerSaveOnce),
                    new FloatMenuOption(SuperEventManager.GetTriggerModeText(SuperEventDef.TriggerMode.GlobalOnce), () =>
                        settings.playerTriggerModes[subDef.defName] = SuperEventDef.TriggerMode.GlobalOnce),
                    new FloatMenuOption(SuperEventManager.GetTriggerModeText(SuperEventDef.TriggerMode.None), () =>
                        settings.playerTriggerModes[subDef.defName] = SuperEventDef.TriggerMode.None)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }
            
            // 来源子Mod
            row.Label("SuperEventFramework.SourceLabel".Translate(), 40f);
            string sourceName = subDef.modContentPack?.Name ?? "";
            row.Label(sourceName, 90f, sourceName);
            
            // 依赖
            row.Label("SuperEventFramework.DependencyLabel".Translate(), 40f);
            string depName = SuperEventManager.GetModOrDlcName(subDef.requiredModOrDlc) ?? "";
            row.Label(depName, 90f, depName);
            
            // 音量标签
            row.Gap(4f);
            row.Label("SuperEventFramework.VolumeLabel".Translate(), 32f);
            
            // 音量滑块 + 数值（手动绘制，接在 WidgetRow 之后）
            float currentVolume = SuperEventManager.GetEventVolume(subDef.defName);
            float sliderWidth = 80f;
            float valueWidth = 28f;
            Rect sliderRect = new Rect(row.FinalX, rect.y + 4f, sliderWidth, 20f);
            float newVolume = Widgets.HorizontalSlider(sliderRect, currentVolume, 0f, 2f, roundTo: 0.1f);
            if (Mathf.Abs(newVolume - currentVolume) > 0.001f)
            {
                settings.superEventVolumes[subDef.defName] = newVolume;
            }
            Rect valueRect = new Rect(sliderRect.xMax + 4f, rect.y + 2f, valueWidth, 24f);
            float displayVol = settings.superEventVolumes.TryGetValue(subDef.defName, out float dv) ? dv : 1f;
            Widgets.Label(valueRect, displayVol.ToString("F1"));
            
            // 测试按钮（手动绘制，放在音量后面）
            string testLabel = "SuperEventFramework.TestTrigger".Translate();
            float testBtnWidth = Text.CalcSize(testLabel).x + 16f;
            Rect testRect = new Rect(valueRect.xMax + 8f, rect.y + 1f, testBtnWidth, 24f);
            if (Widgets.ButtonText(testRect, testLabel))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                SuperEventManager.TestTriggerEvent(subDef);
            }
        }
        
        #endregion
        
        /// <summary>
        /// 超事件框架设置分类
        /// 在mod设置列表里显示的文本
        /// </summary>
        public override string SettingsCategory()
        {
            return "Super Event Framework";
        }
    }
}
