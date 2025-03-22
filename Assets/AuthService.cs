using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Security.Cryptography.X509Certificates;

public class AuthService : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string baseUrl = "http://localhost:5555/api/";
    [SerializeField] private bool allowSelfSignedCertificate = true;
    
    private string _token;
    private string _username;
    
    public bool IsLoggedIn => !string.IsNullOrEmpty(_token);
    public string Username => _username;
    
    // Singleton pattern
    private static AuthService _instance;
    public static AuthService Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<AuthService>();
                if (_instance == null) {
                    GameObject obj = new GameObject("AuthService");
                    _instance = obj.AddComponent<AuthService>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }
    
    void Awake()
    {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Load saved token if any
        _token = PlayerPrefs.GetString("AuthToken", "");
        _username = PlayerPrefs.GetString("Username", "");
    }
    
    // Register a new user
    public IEnumerator Register(string username, string password, Action<bool, string> callback)
    {
        string url = baseUrl + "auth/register";
        
        // Create request data
        string jsonData = JsonUtility.ToJson(new UserData { 
            username = username, 
            password = password 
        });
        
        using (UnityWebRequest request = CreatePostRequest(url, jsonData))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonResponse);
                callback(true, response.message);
            }
            else
            {
                string errorMessage = "Registration failed";
                try
                {
                    ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                    errorMessage = response.message;
                }
                catch { /* Use default error message */ }
                
                callback(false, errorMessage);
                Debug.LogError($"Registration error: {request.error} - {errorMessage}");
            }
        }
    }
    
    // Login user
    public IEnumerator Login(string username, string password, Action<bool, string> callback)
    {
        string url = baseUrl + "auth/login";
        
        // Create request data
        string jsonData = JsonUtility.ToJson(new UserData { 
            username = username, 
            password = password 
        });
        
        using (UnityWebRequest request = CreatePostRequest(url, jsonData))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(jsonResponse);
                
                // Save token and username
                _token = response.token;
                _username = response.username;
                
                // Save to PlayerPrefs for persistence
                PlayerPrefs.SetString("AuthToken", _token);
                PlayerPrefs.SetString("Username", _username);
                PlayerPrefs.Save();
                
                callback(true, "Login successful");
            }
            else
            {
                string errorMessage = "Login failed";
                try
                {
                    ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                    errorMessage = response.message;
                }
                catch { /* Use default error message */ }
                
                callback(false, errorMessage);
                Debug.LogError($"Login error: {request.error} - {errorMessage}");
            }
        }
    }
    
    // Logout user
    public IEnumerator Logout(Action<bool, string> callback)
    {
        string url = baseUrl + "auth/logout";
        
        // Create request data
        string jsonData = JsonUtility.ToJson(new UserData { 
            username = _username, 
            password = "" 
        });
        
        using (UnityWebRequest request = CreatePostRequest(url, jsonData))
        {
            yield return request.SendWebRequest();
            
            bool success = request.result == UnityWebRequest.Result.Success;
            string message = "Logout failed";
            
            if (success)
            {
                try
                {
                    ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                    message = response.message;
                }
                catch { /* Use default message */ }
                
                // Clear stored credentials
                _token = "";
                _username = "";
                PlayerPrefs.DeleteKey("AuthToken");
                PlayerPrefs.DeleteKey("Username");
                PlayerPrefs.Save();
            }
            else
            {
                try
                {
                    ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                    message = response.message;
                }
                catch { /* Use default error message */ }
                
                Debug.LogError($"Logout error: {request.error} - {message}");
            }
            
            callback(success, message);
        }
    }
    
    // Get online players
    public IEnumerator GetOnlinePlayers(Action<bool, string[], string> callback)
    {
        if (!IsLoggedIn)
        {
            callback(false, new string[0], "Not logged in");
            yield break;
        }
        
        string url = baseUrl + "game/online-players";
        
        using (UnityWebRequest request = CreateGetRequest(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                PlayersResponse response = JsonUtility.FromJson<PlayersResponse>(jsonResponse);
                
                callback(true, response.players, "Success");
            }
            else
            {
                string errorMessage = "Failed to get online players";
                try
                {
                    ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                    errorMessage = response.message;
                }
                catch { /* Use default error message */ }
                
                callback(false, new string[0], errorMessage);
                Debug.LogError($"API error: {request.error} - {errorMessage}");
            }
        }
    }
    
    // Get player info
    public IEnumerator GetPlayerInfo(Action<bool, PlayerInfo, string> callback)
    {
        if (!IsLoggedIn)
        {
            callback(false, null, "Not logged in");
            yield break;
        }
        
        string url = baseUrl + "game/player-info";
        
        using (UnityWebRequest request = CreateGetRequest(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                PlayerInfoResponse response = JsonUtility.FromJson<PlayerInfoResponse>(jsonResponse);
                
                PlayerInfo info = new PlayerInfo
                {
                    Username = response.username,
                    IsLoggedIn = response.isLoggedIn
                };
                
                callback(true, info, "Success");
            }
            else
            {
                string errorMessage = "Failed to get player info";
                try
                {
                    ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                    errorMessage = response.message;
                }
                catch { /* Use default error message */ }
                
                callback(false, null, errorMessage);
                Debug.LogError($"API error: {request.error} - {errorMessage}");
            }
        }
    }
    
    // Helper methods to create requests
    private UnityWebRequest CreatePostRequest(string url, string jsonData)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        if (!string.IsNullOrEmpty(_token))
            request.SetRequestHeader("Authorization", $"Bearer {_token}");
            
        // Handle self-signed certificates if allowed
        if (allowSelfSignedCertificate)
            request.certificateHandler = new BypassCertificateHandler();
            
        return request;
    }
    
    private UnityWebRequest CreateGetRequest(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        
        if (!string.IsNullOrEmpty(_token))
            request.SetRequestHeader("Authorization", $"Bearer {_token}");
            
        // Handle self-signed certificates if allowed
        if (allowSelfSignedCertificate)
            request.certificateHandler = new BypassCertificateHandler();
            
        return request;
    }
}

// Model classes for JSON serialization
[Serializable]
public class UserData
{
    public string username;
    public string password;
}

[Serializable]
public class ApiResponse
{
    public bool success;
    public string message;
}

[Serializable]
public class LoginResponse : ApiResponse
{
    public string token;
    public string username;
}

[Serializable]
public class PlayersResponse : ApiResponse
{
    public string[] players;
    public int count;
}

[Serializable]
public class PlayerInfoResponse : ApiResponse
{
    public string username;
    public bool isLoggedIn;
}

public class PlayerInfo
{
    public string Username { get; set; }
    public bool IsLoggedIn { get; set; }
}

// Certificate handler to allow self-signed certificates for development
public class BypassCertificateHandler : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // Return true to accept any certificate
        return true;
    }
}