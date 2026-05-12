using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SuperEventFramework
{
    /// <summary>
    /// 超事件对话框 - 继承自RimWorld的Window类
    /// 负责显示事件界面，包括图片、文字和背景音乐
    /// </summary>
    public class Dialog_SuperEvent : Window
    {
        // 事件定义
        private SuperEventDef eventDef;
        
        // 缓存的图片（懒加载）
        private Texture2D cachedImage;
        
        private float titleHeight = 40f; // 标题高度
        private float buttonHeight = 40f; // 按钮高度
        private float spacing = 20f; // 间距
        private float imageWidthSpacing = 100f; // 图片宽度和窗口预留空间
        private float textWidthSpacing = 40f; // 描述宽度和窗口预留空间

        /// <summary>
        /// 构造函数
        /// </summary>
        public Dialog_SuperEvent(SuperEventDef eventDef)
        {
            this.eventDef = eventDef;
            
            // Window配置
            // 根据btnText是否存在决定关闭按钮显示位置
            bool noBtnText = SuperEventManager.GetSuperEventTextTranslate(eventDef.btnText).NullOrEmpty();
            this.doCloseButton = noBtnText; // 无btnText时显示底部关闭按钮
            this.doCloseX = noBtnText;     // 无btnText时显示右上角X按钮
            this.absorbInputAroundWindow = false; // 不阻挡输入，允许玩家操作其他界面
            this.forcePause = false;           // false=仅TickManager.Pause暂停tick，不锁定WASD/拖拽/暂停按钮
            this.preventCameraMotion = false;  // 允许WASD移动镜头和鼠标拖拽屏幕
            
            // 计算自适应窗口大小
            CalculateWindowSize(noBtnText);
            
            // 暂停游戏右下角tick时间
            if (Current.ProgramState == ProgramState.Playing)
            {
                //Find是RimWorld的静态类，用于访问游戏状态和组件
                Find.TickManager.Pause();
            }
        }
        
        // 缓存计算好的窗口大小
        private Vector2 calculatedWindowSize;
        
        /// <summary>
        /// 设置自适应窗口大小
        /// 宽度比imgWidth宽一些，高度要能容纳标题、图片、描述和按钮
        /// </summary>
        private void CalculateWindowSize(bool noBtnText)
        {
            float configWidth = SuperEventManager.SettingsDef.imgWidth;
            float configHeight = SuperEventManager.SettingsDef.imgHeight;
            
            string desc = SuperEventManager.GetSuperEventTextTranslate(eventDef.desc);
            
            // 判断是否有图片需要显示（自定义图片或默认图至少一个存在）
            bool hasImage = !eventDef.imagePath.NullOrEmpty()
                || !SuperEventManager.SettingsDef.defaultImgPath.NullOrEmpty();
            
            // 内容区宽度 = 图片宽度 + 左右预留空间
            float contentWidth = configWidth + imageWidthSpacing;
            float descWidth = contentWidth - textWidthSpacing - Margin * 2f;
            float descHeight = Text.CalcHeight(desc, descWidth);
            
            // 内容区高度 = 上间距 + 标题 + 间距 [+ 图片 + 间距] + 描述 + 间距 [+ 按钮]
            float contentHeight = spacing + titleHeight + spacing;
            if (hasImage)
                contentHeight += configHeight + spacing;
            contentHeight += descHeight + spacing;
            if (!noBtnText)
                contentHeight += buttonHeight;
            
            // 窗口外框宽度 = 内容区宽度
            float windowWidth = contentWidth;
            // 窗口外框高度 = 内容区 + 上下Margin(各18px) [+ 底部关闭按钮栏(55px)]
            float windowHeight = contentHeight + Margin * 2f;
            if (noBtnText)
                windowHeight += FooterRowHeight;
            
            calculatedWindowSize = new Vector2(windowWidth, windowHeight);
        }
        
        /// <summary>
        /// 窗口初始大小
        /// </summary>
        public override Vector2 InitialSize => calculatedWindowSize;
        
        /// <summary>
        /// 窗口打开后的回调
        /// </summary>
        public override void PostOpen()
        {
            base.PostOpen();
            
            // 懒加载图片——GetEventImage内部有空路径fallback
            cachedImage = SuperEventManager.GetEventImage(eventDef.imagePath);
            
            //先停止bgm，然后播放bgm
            SuperEventManager.StopBGM();
            float volume = SuperEventManager.GetEventVolume(eventDef.defName);
            SuperEventManager.PlayBGM(eventDef.bgmPath, volume);
        }
        
        /// <summary>
        /// 绘制窗口内容
        /// </summary>
        public override void DoWindowContents(Rect inRect)
        {
            // 绘制背景
            //GUI.DrawTexture(inRect, TexUI.FastFillTex);
            
            float currentY = inRect.y + spacing; // 起始Y位置
            
            // 绘制标题（宽度自适应文本）
            using (new TextBlock(GameFont.Medium, TextAnchor.UpperCenter))
            {
                string titleText = SuperEventManager.GetSuperEventTextTranslate(eventDef.title);
                float titleTextWidth = Text.CalcSize(titleText).x + 10f; // 文本宽度 + 10px
                float maxTitleWidth = inRect.width - textWidthSpacing; // 最大宽度：窗口宽度 - 边距
                float titleWidth = Mathf.Min(titleTextWidth, maxTitleWidth);
                Rect titleRect = new Rect(inRect.center.x - titleWidth / 2f, currentY, titleWidth, titleHeight);
                Widgets.Label(titleRect, titleText);
                currentY += titleHeight + spacing;
            }
            
            
            // 绘制图片（如果已加载）
            if (cachedImage != null)
            {
                // 计算图片显示区域（居中显示）
                float imageWidth = SuperEventManager.SettingsDef.imgWidth;
                float imageHeight = SuperEventManager.SettingsDef.imgHeight;
                Rect imageRect = new Rect(inRect.center.x - imageWidth / 2f, currentY, imageWidth, imageHeight);
                GUI.DrawTexture(imageRect, cachedImage, ScaleMode.ScaleToFit);
                currentY += imageHeight + spacing;
            }
            
            // 绘制描述（支持多行，居中显示）
            using (new TextBlock(GameFont.Small, TextAnchor.UpperCenter))
            {
                string descText = SuperEventManager.GetSuperEventTextTranslate(eventDef.desc);
                float descWidth = inRect.width - textWidthSpacing; // 左右留边距
                float descHeight = Text.CalcHeight(descText, descWidth);
                Rect descRect = new Rect(inRect.center.x - descWidth / 2f, currentY, descWidth, descHeight);
                Widgets.Label(descRect, descText);
                currentY += descHeight + spacing;
            }
            
            // 绘制自定义关闭按钮（如果有btnText，宽度自适应文本）
            if (!eventDef.btnText.NullOrEmpty())
            {
                using (new TextBlock(GameFont.Small, TextAnchor.UpperCenter))
                {
                    string btnText = SuperEventManager.GetSuperEventTextTranslate(eventDef.btnText);
                    float buttonTextWidth = Text.CalcSize(btnText).x + 20f; // 文本宽度 + 20px（按钮需要更多内边距）
                    float maxButtonWidth = inRect.width - textWidthSpacing; // 最大宽度：窗口宽度 - 边距
                    float buttonWidth = Mathf.Min(buttonTextWidth, maxButtonWidth);
                    Rect buttonRect = new Rect(inRect.center.x - buttonWidth / 2f, currentY, buttonWidth, buttonHeight);
                    
                    if (Widgets.ButtonText(buttonRect, btnText))
                    {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        this.Close(); // 点击按钮关闭窗口
                    }
                }
            }
        }
        
        /// <summary>
        /// 窗口关闭后的回调
        /// </summary>
        public override void PostClose()
        {
            base.PostClose();
            
            var settings = LoadedModManager.GetMod<SuperEventMod>().GetSettings<SuperEventSettings>();
            if (settings.stopBGMOnClose)
                SuperEventManager.StopBGM();
            
            if (cachedImage != null)
            {
                Resources.UnloadAsset(cachedImage);
                cachedImage = null;
            }
        }

    }
}