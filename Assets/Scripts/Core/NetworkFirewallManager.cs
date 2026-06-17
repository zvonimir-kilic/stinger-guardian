using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

// Upravlja Windows Firewall pravilima za multiplayer
public class NetworkFirewallManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int port = 7777;
    [SerializeField] private string ruleName = "StingerGuardian";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private static NetworkFirewallManager instance;
    private string scriptsFolder;
    private string setupScriptPath;
    private string cleanupScriptPath;
    
    // Konstante za PlayerPrefs
    private const string PREF_FIRST_RUN = "NetworkFirewall_FirstRun";
    private const string PREF_ORIGINAL_PROFILE = "NetworkFirewall_OriginalProfile";
    
    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Postavi putanje do skripti
        scriptsFolder = Path.Combine(Application.persistentDataPath, "NetworkScripts");
        setupScriptPath = Path.Combine(scriptsFolder, "NetworkSetup.ps1");
        cleanupScriptPath = Path.Combine(scriptsFolder, "NetworkCleanup.ps1");
        
        // Registriraj event za gašenje aplikacije
        Application.quitting += OnApplicationQuitting;
    }
    
    private void Start()
    {
        // Samo na Windows platformi
        if (Application.platform != RuntimePlatform.WindowsPlayer && 
            Application.platform != RuntimePlatform.WindowsEditor)
        {
            Log("NetworkFirewallManager radi samo na Windows platformi.");
            return;
        }
        
        // Kreiraj skripte ako ne postoje
        CreateScriptsIfNeeded();
        
        // Pokreni setup
        RunSetup();
    }
    
    private void OnDestroy()
    {
        Application.quitting -= OnApplicationQuitting;
    }
    
    private void OnApplicationQuitting()
    {
        Log("Aplikacija se gasi - pokrećem cleanup...");
        RunCleanup();
    }
    
    // Kreira PowerShell skripte ako ne postoje
    private void CreateScriptsIfNeeded()
    {
        // Kreiraj folder ako ne postoji
        if (!Directory.Exists(scriptsFolder))
        {
            Directory.CreateDirectory(scriptsFolder);
            Log($"Kreiran folder: {scriptsFolder}");
        }
        
        // Dohvati putanju do exe-a
        string exePath = GetExecutablePath();
        
        // Kreiraj setup skriptu
        CreateSetupScript(exePath);
        
        // Kreiraj cleanup skriptu
        CreateCleanupScript();
    }
    
    // Vraća putanju do executable-a
    private string GetExecutablePath()
    {
        #if UNITY_EDITOR
        // U editoru koristi placeholder
        return Path.Combine(Application.dataPath, "..", "Build", "StingerGuardian.exe");
        #else
        // U ugrađenoj igri koristi stvarnu putanju
        return Process.GetCurrentProcess().MainModule.FileName;
        #endif
    }
    
    // Kreira PowerShell setup skriptu
    private void CreateSetupScript(string exePath)
    {
        string script = $@"
# NetworkSetup.ps1 - Postavlja Firewall pravila za Stinger Guardian
# Pokreni kao Administrator

param(
    [string]$ExePath = '{exePath.Replace("\\", "\\\\")}',
    [int]$Port = {port},
    [string]$RuleName = '{ruleName}',
    [string]$StateFile = '{Path.Combine(scriptsFolder, "network_state.txt").Replace("\\", "\\\\")}'
)

$ErrorActionPreference = 'Stop'

try {{
    # Provjeri admin prava
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {{
        Write-Host 'GRESKA: Skripta zahtijeva administratorska prava!'
        exit 1
    }}

    # Dohvati trenutni mrežni profil i spremi ga
    $networkProfile = Get-NetConnectionProfile | Select-Object -First 1
    if ($networkProfile) {{
        $originalProfile = $networkProfile.NetworkCategory
        
        # Spremi originalni profil samo ako nije već spremljen
        if (-not (Test-Path $StateFile)) {{
            $originalProfile | Out-File -FilePath $StateFile -Encoding UTF8
            Write-Host ""Spremljen originalni mrežni profil: $originalProfile""
        }}
        
        # Postavi mrežu na Private ako nije već
        if ($networkProfile.NetworkCategory -ne 'Private') {{
            Set-NetConnectionProfile -InterfaceIndex $networkProfile.InterfaceIndex -NetworkCategory Private
            Write-Host 'Mrežni profil postavljen na Private'
        }} else {{
            Write-Host 'Mrežni profil je već Private'
        }}
    }}

    # Provjeri postoje li pravila
    $tcpRuleExists = Get-NetFirewallRule -DisplayName ""$RuleName TCP $Port"" -ErrorAction SilentlyContinue
    $udpRuleExists = Get-NetFirewallRule -DisplayName ""$RuleName UDP $Port"" -ErrorAction SilentlyContinue
    $exeRuleExists = Get-NetFirewallRule -DisplayName ""$RuleName Application"" -ErrorAction SilentlyContinue

    # TCP pravilo
    if ($tcpRuleExists) {{
        Enable-NetFirewallRule -DisplayName ""$RuleName TCP $Port""
        Write-Host ""Omogućeno postojeće TCP pravilo""
    }} else {{
        New-NetFirewallRule -DisplayName ""$RuleName TCP $Port"" `
            -Direction Inbound `
            -Protocol TCP `
            -LocalPort $Port `
            -Action Allow `
            -Profile Private `
            -EdgeTraversalPolicy Allow `
            -Enabled True
        Write-Host ""Kreirano novo TCP pravilo""
    }}

    # UDP pravilo
    if ($udpRuleExists) {{
        Enable-NetFirewallRule -DisplayName ""$RuleName UDP $Port""
        Write-Host ""Omogućeno postojeće UDP pravilo""
    }} else {{
        New-NetFirewallRule -DisplayName ""$RuleName UDP $Port"" `
            -Direction Inbound `
            -Protocol UDP `
            -LocalPort $Port `
            -Action Allow `
            -Profile Private `
            -EdgeTraversalPolicy Allow `
            -Enabled True
        Write-Host ""Kreirano novo UDP pravilo""
    }}

    # Pravilo za executable
    if (Test-Path $ExePath) {{
        if ($exeRuleExists) {{
            Enable-NetFirewallRule -DisplayName ""$RuleName Application""
            Write-Host ""Omogućeno postojeće pravilo za aplikaciju""
        }} else {{
            New-NetFirewallRule -DisplayName ""$RuleName Application"" `
                -Direction Inbound `
                -Program $ExePath `
                -Action Allow `
                -Profile Private `
                -EdgeTraversalPolicy Allow `
                -Enabled True
            Write-Host ""Kreirano novo pravilo za aplikaciju: $ExePath""
        }}
    }} else {{
        Write-Host ""UPOZORENJE: Exe nije pronađen na putanji: $ExePath""
    }}

    Write-Host 'Setup uspješno završen!'
    exit 0
}}
catch {{
    Write-Host ""GRESKA: $_""
    exit 1
}}
";
        
        File.WriteAllText(setupScriptPath, script);
        Log($"Kreirana setup skripta: {setupScriptPath}");
    }
    
    // Kreira PowerShell cleanup skriptu
    private void CreateCleanupScript()
    {
        string script = $@"
# NetworkCleanup.ps1 - Vraća Firewall pravila i mrežni profil
# Pokreni kao Administrator

param(
    [int]$Port = {port},
    [string]$RuleName = '{ruleName}',
    [string]$StateFile = '{Path.Combine(scriptsFolder, "network_state.txt").Replace("\\", "\\\\")}'
)

$ErrorActionPreference = 'SilentlyContinue'

try {{
    # Provjeri admin prava
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {{
        Write-Host 'GRESKA: Skripta zahtijeva administratorska prava!'
        exit 1
    }}

    # Onemogući pravila (ne briši ih)
    Disable-NetFirewallRule -DisplayName ""$RuleName TCP $Port"" -ErrorAction SilentlyContinue
    Write-Host ""Onemogućeno TCP pravilo""
    
    Disable-NetFirewallRule -DisplayName ""$RuleName UDP $Port"" -ErrorAction SilentlyContinue
    Write-Host ""Onemogućeno UDP pravilo""
    
    Disable-NetFirewallRule -DisplayName ""$RuleName Application"" -ErrorAction SilentlyContinue
    Write-Host ""Onemogućeno pravilo za aplikaciju""

    # Vrati originalni mrežni profil
    if (Test-Path $StateFile) {{
        $originalProfile = Get-Content -Path $StateFile -Raw
        $originalProfile = $originalProfile.Trim()
        
        $networkProfile = Get-NetConnectionProfile | Select-Object -First 1
        if ($networkProfile -and $originalProfile) {{
            Set-NetConnectionProfile -InterfaceIndex $networkProfile.InterfaceIndex -NetworkCategory $originalProfile
            Write-Host ""Mrežni profil vraćen na: $originalProfile""
        }}
    }}

    Write-Host 'Cleanup uspješno završen!'
    exit 0
}}
catch {{
    Write-Host ""GRESKA: $_""
    exit 1
}}
";
        
        File.WriteAllText(cleanupScriptPath, script);
        Log($"Kreirana cleanup skripta: {cleanupScriptPath}");
    }
    
    // Pokreće setup skriptu s admin pravima
    private void RunSetup()
    {
        Log("Pokrećem network setup...");
        RunPowerShellScript(setupScriptPath);
    }
    
    // Pokreće cleanup skriptu s admin pravima
    private void RunCleanup()
    {
        Log("Pokrećem network cleanup...");
        RunPowerShellScript(cleanupScriptPath, waitForExit: true);
    }
    
    // Pokreće PowerShell skriptu s admin pravima
    private void RunPowerShellScript(string scriptPath, bool waitForExit = false)
    {
        try
        {
            if (!File.Exists(scriptPath))
            {
                Log($"Skripta ne postoji: {scriptPath}");
                return;
            }
            
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                Verb = "runas", // Zahtijeva admin prava (UAC prompt)
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            
            Process process = Process.Start(startInfo);
            
            if (waitForExit && process != null)
            {
                process.WaitForExit(5000); // Max 5 sekundi
            }
            
            Log($"Skripta pokrenuta: {scriptPath}");
        }
        catch (Exception ex)
        {
            Log($"Greška pri pokretanju skripte: {ex.Message}");
        }
    }
    
    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[NetworkFirewallManager] {message}");
        }
    }
    
    // Ručno pokreni setup
    [ContextMenu("Run Setup")]
    public void ManualRunSetup()
    {
        CreateScriptsIfNeeded();
        RunSetup();
    }
    
    // Ručno pokreni cleanup
    [ContextMenu("Run Cleanup")]
    public void ManualRunCleanup()
    {
        RunCleanup();
    }
}
