namespace FxttMonitorNotifier.Droid.Models.Api
{
    using System;

    public class AuthToken
    {
        public AuthToken(Guid token)
        {
            this.Token = token;
        }

        public Guid Token { get; set; }
    }
}