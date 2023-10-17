using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FastExpressionKit.Test
{
    public class ValidationError : Exception
    {
        public ValidationError(string s) : base(s)
        {
        }

    }
    /// <summary>
    /// Use this to register very simple string validator that checks all string properties for "bad" characters
    ///
    /// Minimal customization is offered
    /// - Set your own BadCharacters array (static) to use globally
    /// - Configure the property list to check with AddValidator to exclude some properties you don't want to check
    ///
    /// Design goal: be fast to run
    /// </summary>
    /// 
    public static class TrivialValidator
    {
        // yes, it's public by design
        
        public static char[] BadCharacters = new [] { '\\', '\'', '<', '>', '"', '&' }; 
        private static Dictionary<Type, Lazy<object>> validatorCache = new Dictionary<Type, Lazy<object>>();
        public static void ValidateString(Type t, string key, string value, char[] badChars)
        {
            var loc = value.IndexOfAny(badChars);
            if (loc != -1)
                throw new ValidationError($"Type: {t.ToString()}, Property {key} contained illegal character '{value[loc]}' at position {loc}");
        }

        public static void SetValidator<T>(Func<PropertyInfo, bool> shouldValidateProperty, char[] badChars, bool overwrite = false, bool compileNow = false)
        {
            if (!overwrite && validatorCache.ContainsKey(typeof(T)))
                return;
            var props = ReflectionHelper.GetProps<T>()
                .Where(p => p.PropertyType == typeof(string) && shouldValidateProperty(p))
                .Select(p => p.Name).ToList();

            var lazyEntry = new Lazy<object>(() =>
            {
                var validator =
                    new RunMethodForEachProperty<T, char[]>(props, 
                        typeof(TrivialValidator).GetMethod(nameof(ValidateString)), 
                        badChars);
                return validator;
            });
            validatorCache[typeof(T)] = lazyEntry;
            if (compileNow)
            {
                _ = lazyEntry.Value;
            }

        }

        public static void Validate<T>(T obj)
        {
            var validator = validatorCache[typeof(T)].Value;
            var typed = validator as RunMethodForEachProperty<T, char[]>;
            typed.Run(obj);
        }

        public static void ValidateMany<T>(IEnumerable<T> items)
        {
            int idx = 0;
            foreach (var item in items)
            {
                try
                {
                    Validate(item);
                    idx++;
                } catch (ValidationError err)
                {
                    throw new ValidationError($"ValidateMany failed at index {idx}: {err.Message}");
                }
                
            }
        }
        
    }
}