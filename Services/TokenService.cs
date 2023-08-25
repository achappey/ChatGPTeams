using System;

public interface ITokenService
{
    string GetToken();
    void SetToken(string token);
}

public class TokenService : ITokenService
{
    private string _token;

    public string GetToken()
    {
        if (_token == null)
        {
        //    throw new Exception("Token not set");
        }
        
        return _token;
    }

    public void SetToken(string token)
    {
        _token = token;
    }
}
