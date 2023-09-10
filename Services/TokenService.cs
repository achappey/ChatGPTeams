using System;

public interface ITokenService
{
    string GetToken();
    void SetToken(string token);
    string GetDataverseToken();
    void SetDataverseToken(string token);
}

public class TokenService : ITokenService
{
    private string _token;
    private string _dataverseToken;

    public string GetToken()
    {
        return _token;
    }

    public string GetDataverseToken()
    {
        return _dataverseToken;
    }

    public void SetToken(string token)
    {
        _token = token;
    }

    public void SetDataverseToken(string token)
    {
        _dataverseToken = token;
    }
}

