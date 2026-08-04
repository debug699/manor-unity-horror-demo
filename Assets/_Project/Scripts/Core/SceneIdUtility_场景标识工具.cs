using System.Text;

namespace Manor.Core
{
    public static class SceneIdUtility
    {
        public static string FromSceneName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return string.Empty;
            int suffixSeparator = sceneName.IndexOf('_', 4);
            string englishName = suffixSeparator > 0 ? sceneName.Substring(0, suffixSeparator) : sceneName;
            StringBuilder builder = new StringBuilder(englishName.Length + 8);

            for (int i = 0; i < englishName.Length; i++)
            {
                char character = englishName[i];
                if (char.IsUpper(character) && i > 0 && englishName[i - 1] != '_' && !char.IsUpper(englishName[i - 1]))
                    builder.Append('_');
                builder.Append(char.ToUpperInvariant(character));
            }

            return builder.ToString();
        }
    }
}
