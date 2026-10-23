using OefenApi.Models;

namespace OefenApi.Auth;

internal static class AccountContext
{
    public static Account Get(HttpContext http)
    {
        if (http.Items[RequestItems.Account] is Account account)
        {
            return account;
        }

        throw new InvalidOperationException("Geen account op dit request.");
    }
}
