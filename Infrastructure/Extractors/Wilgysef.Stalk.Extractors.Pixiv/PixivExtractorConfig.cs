using System.Linq.Expressions;
using Wilgysef.Stalk.Core.Shared.Extensions;

namespace Wilgysef.Stalk.Extractors.Pixiv;

public class PixivExtractorConfig
{
    public static readonly string CookiesKey = "cookies";

    public string? CookieString { get; set; }

    public PixivExtractorConfig() { }

    public PixivExtractorConfig(IDictionary<string, object?>? config)
    {
        TrySetValue(() => CookieString, config, CookiesKey);
    }

    // TODO: move to shared or replace
    private static bool TrySetValue<T>(Expression<Func<T>> property, IDictionary<string, object?>? config, string key)
    {
        if (!(config?.TryGetValueAs<T, string, object?>(key, out var value) ?? false))
        {
            return false;
        }

        Expression.Lambda(Expression.Assign(property.Body, Expression.Constant(value)))
            .Compile()
            .DynamicInvoke();
        return true;
    }
}
