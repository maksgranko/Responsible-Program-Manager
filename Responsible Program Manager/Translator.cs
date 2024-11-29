using System.Globalization;
using System.Resources;
using System.Threading;

namespace Responsible_Program_Manager
{
    public static class Translator
    {
        private static readonly ResourceManager ResourceManager =
            new ResourceManager("Responsible_Program_Manager.Language", typeof(Translator).Assembly);

        public static CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

        public static string Translate(string key)
        {
                return ResourceManager.GetString(key, CurrentCulture) ?? $"[{key}]";
        }
        
        public static void SetLanguage(string cultureName)
        {
            CurrentCulture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentUICulture = CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CurrentCulture;
        }
    }
}
