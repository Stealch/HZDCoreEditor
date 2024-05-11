namespace HZDCoreEditorUI.Util;

using System;

/// <summary>
/// Provides extension methods for the Type class.
/// </summary>
public static class Types
{
    /// <summary>
    /// Returns a friendly name for the given type.
    /// </summary>
    /// <param name="type">The type to get the friendly name for.</param>
    /// <returns>The friendly name of the type.</returns>
    public static string GetFriendlyName(this Type type)
    {
        // Initialize the friendly name with the type's name
        string friendlyName = type.Name;

        // If the type is generic, modify the friendly name to include the generic parameters
        if (type.IsGenericType)
        {
            // Remove the backtick and opening angle bracket from the type name
            int iBacktick = friendlyName.IndexOf('`');
            if (iBacktick > 0)
            {
                friendlyName = friendlyName.Remove(iBacktick);
            }

            // Add the opening angle bracket and generic parameter names
            friendlyName += "<";
            Type[] typeParameters = type.GetGenericArguments();
            for (int i = 0; i < typeParameters.Length; ++i)
            {
                // Get the friendly name of the generic parameter
                string typeParamName = GetFriendlyName(typeParameters[i]);
                friendlyName += (i == 0) ? typeParamName : "," + typeParamName;
            }

            // Add the closing angle bracket
            friendlyName += ">";
        }

        // Return the friendly name of the type
        return friendlyName;
    }
}
