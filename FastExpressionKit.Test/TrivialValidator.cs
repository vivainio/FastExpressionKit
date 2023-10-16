using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FastExpressionKit.Test
{
    /// <summary>
    /// Use this to register very simple string validator that checks all string properties for "bad" characters
    ///
    /// Minimal customization is offered
    /// - Set your own BadCharacters array (static) to use globally
    /// - Configure the property list to check with AddValidator to exclude some properties you don't want to check
    ///
    /// Design goal: be fast to run
    /// </summary>
    public static class TrivialValidator
    {
        // yes, it's public by design
        
        public static char[] BadCharacters = new [] { '\\', '\'', '<', '>', '"', '&' }; 
        private static Dictionary<Type, Lazy<object>> validatorCache = new Dictionary<Type, Lazy<object>>();
        private  static void ValidateString(string key, string value)
        {
            var loc = value.IndexOfAny(BadCharacters);
            if (loc != -1)
                throw new ArgumentException($"Property {key} contained illegal character '{value[loc]}' at position {loc}");
        }

        public static void AddValidator<T>(Func<PropertyInfo, bool> shouldValidateProperty)
        {
            var props = ReflectionHelper.GetProps<T>()
                .Where(p => p.PropertyType == typeof(string) && shouldValidateProperty(p))
                .Select(p => p.Name).ToList();

            validatorCache[typeof(T)] = new Lazy<object>(() =>
            {
                var validator = new ForEachString<T>(props, typeof(TrivialValidator).GetMethod(nameof(ValidateString)));
                return validator;
            });

        }

        public static void Validate<T>(T obj)
        {
            var validator = validatorCache[typeof(T)].Value;
            var typed = validator as ForEachString<T>;
            typed.Run(obj);
        }
        
    }
}