using Verse;

namespace sakuyaMZLibs
{
    /// <summary>
    /// 带单例缓存的 GameComponent 抽象基类。
    /// 换存档时自动失效并重新获取新实例。
    /// </summary>
    public abstract class InstanceGameComponent<T> : GameComponent where T : InstanceGameComponent<T>
    {
        private static T _instance;
        private static Game _cachedGame;

        public static T Instance
        {
            get
            {
                Game currentGame = Current.Game;
                if (currentGame != _cachedGame)
                {
                    _cachedGame = currentGame;
                    _instance = currentGame?.GetComponent<T>();
                }
                return _instance;
            }
        }

        protected InstanceGameComponent() { }
    }
}
