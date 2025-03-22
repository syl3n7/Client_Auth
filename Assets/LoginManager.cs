using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [Header("Login Panel")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField usernameInputLogin;
    [SerializeField] private TMP_InputField passwordInputLogin;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerTabButton;
    [SerializeField] private TextMeshProUGUI loginErrorText;
    
    [Header("Register Panel")]
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private TMP_InputField usernameInputRegister;
    [SerializeField] private TMP_InputField passwordInputRegister;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button loginTabButton;
    [SerializeField] private TextMeshProUGUI registerErrorText;
    
    [Header("Authenticated Panel")]
    [SerializeField] private GameObject authenticatedPanel;
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button refreshPlayersButton;
    [SerializeField] private TextMeshProUGUI playersListText;
    
    private bool _isAuthenticating = false;
    
    private void Start()
    {
        // Set up UI event handlers
        loginButton.onClick.AddListener(OnLoginClick);
        registerButton.onClick.AddListener(OnRegisterClick);
        logoutButton.onClick.AddListener(OnLogoutClick);
        refreshPlayersButton.onClick.AddListener(OnRefreshPlayersClick);
        
        // Tab switching
        registerTabButton.onClick.AddListener(() => {
            loginPanel.SetActive(false);
            registerPanel.SetActive(true);
        });
        
        loginTabButton.onClick.AddListener(() => {
            registerPanel.SetActive(false);
            loginPanel.SetActive(true);
        });
        
        // Clear error messages
        loginErrorText.text = "";
        registerErrorText.text = "";
        
        // Check if already logged in
        if (AuthService.Instance.IsLoggedIn)
        {
            OnAuthenticationSuccess();
        }
        else
        {
            loginPanel.SetActive(true);
            registerPanel.SetActive(false);
            authenticatedPanel.SetActive(false);
        }
    }
    
    private void OnLoginClick()
    {
        if (_isAuthenticating) return;
        
        string username = usernameInputLogin.text;
        string password = passwordInputLogin.text;
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            loginErrorText.text = "Please enter both username and password";
            return;
        }
        
        _isAuthenticating = true;
        loginButton.interactable = false;
        loginErrorText.text = "Logging in...";
        
        StartCoroutine(AuthService.Instance.Login(username, password, (success, message) => {
            _isAuthenticating = false;
            loginButton.interactable = true;
            
            if (success)
            {
                loginErrorText.text = "";
                usernameInputLogin.text = "";
                passwordInputLogin.text = "";
                OnAuthenticationSuccess();
            }
            else
            {
                loginErrorText.text = message;
            }
        }));
    }
    
    private void OnRegisterClick()
    {
        if (_isAuthenticating) return;
        
        string username = usernameInputRegister.text;
        string password = passwordInputRegister.text;
        string confirmPassword = confirmPasswordInput.text;
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            registerErrorText.text = "Please enter both username and password";
            return;
        }
        
        if (password != confirmPassword)
        {
            registerErrorText.text = "Passwords do not match";
            return;
        }
        
        if (password.Length < 8)
        {
            registerErrorText.text = "Password must be at least 8 characters long";
            return;
        }
        
        _isAuthenticating = true;
        registerButton.interactable = false;
        registerErrorText.text = "Registering...";
        
        StartCoroutine(AuthService.Instance.Register(username, password, (success, message) => {
            _isAuthenticating = false;
            registerButton.interactable = true;
            
            if (success)
            {
                registerErrorText.text = "Registration successful! You can now log in.";
                
                // Clear fields
                usernameInputRegister.text = "";
                passwordInputRegister.text = "";
                confirmPasswordInput.text = "";
                
                // Switch to login panel after a delay
                StartCoroutine(SwitchToLoginAfterDelay(2f));
            }
            else
            {
                registerErrorText.text = message;
            }
        }));
    }
    
    private void OnLogoutClick()
    {
        if (_isAuthenticating) return;
        
        _isAuthenticating = true;
        logoutButton.interactable = false;
        
        StartCoroutine(AuthService.Instance.Logout((success, message) => {
            _isAuthenticating = false;
            logoutButton.interactable = true;
            
            // Return to login panel regardless of success
            loginPanel.SetActive(true);
            authenticatedPanel.SetActive(false);
        }));
    }
    
    private void OnRefreshPlayersClick()
    {
        if (_isAuthenticating) return;
        
        refreshPlayersButton.interactable = false;
        playersListText.text = "Loading players...";
        
        StartCoroutine(AuthService.Instance.GetOnlinePlayers((success, players, message) => {
            refreshPlayersButton.interactable = true;
            
            if (success && players != null)
            {
                if (players.Length == 0)
                {
                    playersListText.text = "No players online";
                }
                else
                {
                    playersListText.text = "Online Players:\n" + string.Join("\n", players);
                }
            }
            else
            {
                playersListText.text = "Error: " + message;
            }
        }));
    }
    
    private void OnAuthenticationSuccess()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        authenticatedPanel.SetActive(true);
        welcomeText.text = $"Welcome, {AuthService.Instance.Username}!";
        
        // Initial fetch of online players
        OnRefreshPlayersClick();
    }
    
    private IEnumerator SwitchToLoginAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
    }
}