namespace TheAdamsParadigm.Api.Models
{
    // Small key/value store for runtime switches that should change without a redeploy
    // (e.g. ActivePaymentProvider). Never store secrets here.
    public class AppSetting
    {
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }
    }
}
