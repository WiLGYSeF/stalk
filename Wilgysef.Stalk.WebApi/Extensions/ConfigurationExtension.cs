namespace Wilgysef.Stalk.WebApi.Extensions;

public static class ConfigurationExtension
{
    public static IDisposable RegisterRepeatChangeCallback(
        this IConfiguration configuration,
        Action<object> callback)
    {
        return RegisterRepeatChangeCallback(configuration, callback, null!);
    }

    public static IDisposable RegisterRepeatChangeCallback(
        this IConfiguration configuration,
        Action<object> callback,
        object state)
    {
        return new ChangeCallback(configuration, callback, state);
    }

    private class ChangeCallback : IDisposable
    {
        private IDisposable? Disposable { get; set; }

        private readonly IConfiguration _configuration;
        private readonly Action<object> _callback;
        private readonly object _state;

        public ChangeCallback(
            IConfiguration configuration,
            Action<object> callback,
            object state)
        {
            _configuration = configuration;
            _callback = callback;
            _state = state;

            RegisterChangeCallback();
        }

        public void Dispose()
        {
            Disposable?.Dispose();

            GC.SuppressFinalize(this);
        }

        private IDisposable RegisterChangeCallback()
        {
            var token = _configuration.GetReloadToken();
            Disposable = token.RegisterChangeCallback(state =>
            {
                // TODO: this runs three times per change?
                _callback(state);

                Disposable?.Dispose();
                Disposable = null;
                RegisterChangeCallback();
            }, _state);
            return this;
        }
    }
}
