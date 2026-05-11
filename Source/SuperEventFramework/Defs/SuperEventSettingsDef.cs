using Verse;

namespace SuperEventFramework
{
    /// <summary>
    /// 超事件框架全局设置定义
    /// 这个Def存储整个框架的全局配置，所有事件共享这些设置
    /// </summary>
    public class SuperEventSettingsDef : Def
    {
        // 默认图片路径（当事件没有指定图片或图片加载失败时使用）
        public string defaultImgPath;
        
        // 图片显示配置（全局默认值）
        public int imgWidth = 800;
        public int imgHeight = 600;
    }
}