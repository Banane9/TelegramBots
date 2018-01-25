using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace TelegramBotLib
{
    public static class MethodInfoExtension
    {
        public static T Invoke<T>(this MethodInfo methodInfo, object obj, params object[] parameters)
        {
            return (T)methodInfo.Invoke(obj, parameters);
        }

        public static object Invoke(this MethodInfo methodInfo, object obj, params object[] parameters)
        {
            return methodInfo.Invoke(obj, parameters);
        }
    }
}