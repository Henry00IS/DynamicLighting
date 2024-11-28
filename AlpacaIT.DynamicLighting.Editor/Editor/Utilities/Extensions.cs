using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting.Editor
{
    internal static class Extensions
    {
        /// <summary>Checks whether the keyword is disabled.</summary>
        /// <param name="shaderKeywordSet">The keyword set to be checked.</param>
        /// <param name="keyword">The keyword to find.</param>
        /// <returns>True when the keyword is disabled else false.</returns>
        public static bool IsDisabled(this ShaderKeywordSet shaderKeywordSet, ShaderKeyword keyword)
        {
            return !shaderKeywordSet.IsEnabled(keyword);
        }

        /// <summary>Checks whether all keywords are enabled.</summary>
        /// <param name="shaderKeywordSet">The keyword set to be checked.</param>
        /// <param name="keywords">The keywords to match.</param>
        /// <returns>True when all keywords are enabled else false.</returns>
        public static bool AllEnabled(this ShaderKeywordSet shaderKeywordSet, params ShaderKeyword[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
                if (!shaderKeywordSet.IsEnabled(keywords[i]))
                    return false;
            return true;
        }

        /// <summary>Checks whether any keyword is enabled.</summary>
        /// <param name="shaderKeywordSet">The keyword set to be checked.</param>
        /// <param name="keywords">The keywords to match.</param>
        /// <returns>True when any of the keywords are enabled else false.</returns>
        public static bool AnyEnabled(this ShaderKeywordSet shaderKeywordSet, params ShaderKeyword[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
                if (shaderKeywordSet.IsEnabled(keywords[i]))
                    return true;
            return false;
        }
    }
}