namespace Schedule.Core.Entities
{
    public class JWTOptions
    {
        public string Issuer { get; init; } = "Schedule.Api";
        public string Audience { get; init; } = "Schedule.Web";
        public string SecretKey { get; init; } = "rikpCERqSatZ4iLKc9Vffai6T/BUVcNS5RM5KNG/Y9w=";
        public int ExpiresMinutes { get; init; } = 60;
        public int ExpireHours { get; set; }
    }
}
