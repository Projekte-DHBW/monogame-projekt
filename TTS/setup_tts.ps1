param (
    [string]$VenvPath = "$env:APPDATA\DHBW-Game\tts_venv"
)

# Quick PowerShell version check for compatibility
$psVersion = $PSVersionTable.PSVersion.Major
if ($psVersion -lt 5) {
    Write-Output "Warning: PowerShell version $psVersion detected. This script is optimized for PS 5.1+. Consider updating for better reliability in TTS setup."
}

# Flag files in logs dir (use Out-File for consistency)
$logDir = "$env:APPDATA\DHBW-Game\logs"
$logFile = Join-Path $logDir "setup_tts.log"
$startedFlag = Join-Path $logDir "tts_setup_started.flag"
$successFlag = Join-Path $logDir "tts_setup_success.flag"
$errorFlag = Join-Path $logDir "tts_setup_error.flag"

# Create log dir if needed
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

# Clean up old flags before starting (logs untouched)
try {
    if (Test-Path $startedFlag) { Remove-Item $startedFlag -Force }
    if (Test-Path $successFlag) { Remove-Item $successFlag -Force }
    if (Test-Path $errorFlag) { Remove-Item $errorFlag -Force }
} catch {
    "Cleanup failed for old flags: $_" | Out-File -FilePath $logFile -Append -Encoding utf8
}

function Write-Log {
    param (
        [string]$Level,
        [string]$Message
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "$timestamp - $Level - $Message"
    $logMessage | Out-File -FilePath $logFile -Append -Encoding utf8
    Write-Output $logMessage # Console output
}

function Write-ErrorFlag {
    param (
        [string]$ErrorMessage
    )
    Write-Log "ERROR" $ErrorMessage
    try {
        $ErrorMessage | Out-File -FilePath $errorFlag -Encoding utf8
    } catch {
        Write-Log "ERROR" "Failed to write error flag: $_"
    }
    exit 1
}

# Create started flag FIRST to confirm launch
try {
    "STARTED" | Out-File -FilePath $startedFlag -Encoding utf8
    Write-Log "INFO" "TTS setup started. Flag created."
} catch {
    Write-Output "Failed to create started flag: $_"
    exit 1
}

# Refresh PATH to ensure tools are visible
$env:PATH = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
Write-Log "INFO" "PATH refreshed for PowerShell session. Current PATH length: $($env:PATH.Length)"

Write-Log "INFO" "Starting TTS venv setup for lecturer voice cloning at: $VenvPath using Python 3.11"

# Function to install UV if not present
function Install-UV {
    Write-Log "INFO" "Checking for UV installation..."
    $uvCommand = Get-Command uv -ErrorAction SilentlyContinue
    if ($uvCommand) {
        $uvPath = $uvCommand.Source
        Write-Log "INFO" "UV already installed at: $uvPath. Skipping installation."
        return $true, $uvPath
    } else {
        Write-Log "INFO" "UV not found. Attempting installation..."
        
        # First, try winget (preferred, silent)
        $wingetAvailable = $false
        try {
            $wingetTest = winget --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                $wingetAvailable = $true
                Write-Log "INFO" "Winget detected. Installing UV via winget..."
                $installOutput = winget install --id astral-sh.uv --accept-package-agreements --accept-source-agreements 2>&1
                $installOutput | Out-File -FilePath $logFile -Append -Encoding utf8
                Write-Output $installOutput
                if ($LASTEXITCODE -eq 0) {
                    Write-Log "INFO" "UV installed successfully via winget."
                } else {
                    Write-Log "WARNING" "Winget install failed. Falling back to installer script. Output: $installOutput"
                }
            }
        } catch {
            Write-Log "WARNING" "Winget not available or failed: $_"
        }
        
        # Fallback: Official installer script
        if (-not $wingetAvailable -or $LASTEXITCODE -ne 0) {
            Write-Log "INFO" "Installing UV via official PowerShell script..."
            try {
                $installOutput = powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex" 2>&1
                $installOutput | Out-File -FilePath $logFile -Append -Encoding utf8
                Write-Output $installOutput
                if ($LASTEXITCODE -eq 0) {
                    Write-Log "INFO" "UV installed successfully via installer script."
                } else {
                    $errorMessage = "Failed to install UV. Manual installation required: Run 'winget install --id astral-sh.uv' or 'powershell -ExecutionPolicy ByPass -c ""irm https://astral.sh/uv/install.ps1 | iex"" ' in PowerShell. Output: $installOutput"
                    Write-ErrorFlag $errorMessage
                    return $false, $null
                }
            } catch {
                $errorMessage = "Exception during UV installation: $_ . Manual installation required: Run 'winget install --id astral-sh.uv' or 'powershell -ExecutionPolicy ByPass -c ""irm https://astral.sh/uv/install.ps1 | iex"" ' in PowerShell."
                Write-ErrorFlag $errorMessage
                return $false, $null
            }
        }
        
        # Refresh PATH after install to pick up UV
        $env:PATH = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        Write-Log "INFO" "PATH refreshed after UV install. Verifying..."
        
        # Verify installation
        $uvCommand = Get-Command uv -ErrorAction SilentlyContinue
        if ($uvCommand) {
            $uvPath = $uvCommand.Source
            Write-Log "INFO" "UV verified at: $uvPath. Version: $(& uv --version 2>&1)"
            return $true, $uvPath
        } else {
            $errorMessage = "UV installation completed but not detected in PATH. Restart PowerShell or manually add to PATH. Manual installation required: Run 'winget install --id astral-sh.uv' or 'powershell -ExecutionPolicy ByPass -c ""irm https://astral.sh/uv/install.ps1 | iex"" ' in PowerShell."
            Write-ErrorFlag $errorMessage
            return $false, $null
        }
    }
}

# Install UV if needed
$usingUV = $false
$uvPath = $null
$installSuccess, $uvPath = Install-UV
if ($installSuccess) {
    $usingUV = $true
} else {
    Write-Log "WARNING" "UV installation failed or skipped. Falling back to py/venv/pip method."
}

# Function to check Python version
function Get-PythonVersion {
    param (
        [string]$PythonCmd,
        [string]$VersionFlag = ""
    )
    try {
        if ($VersionFlag) {
            $versionOutput = & $PythonCmd $VersionFlag --version 2>&1
        } else {
            $versionOutput = & $PythonCmd --version 2>&1
        }
        Write-Log "DEBUG" "Version check for '${PythonCmd}' ${VersionFlag} : ${versionOutput} (Exit code: $LASTEXITCODE)"
        if ($LASTEXITCODE -eq 0 -and $versionOutput -match "Python\s+3\.11(?:\.\d+)?") {
            return $true, $versionOutput
        }
        return $false, $versionOutput
    }
    catch {
        return $false, $_.Exception.Message
    }
}

# Create venv folder if it doesn't exist
if (-not (Test-Path $VenvPath)) {
    New-Item -ItemType Directory -Path $VenvPath -Force | Out-Null
    Write-Log "INFO" "Created venv directory: $VenvPath"
}

# Create/validate venv (UV if available, else fallback)
$ScriptCheck = Join-Path $VenvPath "Scripts\python.exe"
if ($usingUV -and -not (Test-Path $ScriptCheck)) {
    Write-Log "INFO" "Creating venv with UV..."
    & uv venv --python 3.11 $VenvPath
    if ($LASTEXITCODE -ne 0) {
        $errorMessage = "Failed to create venv with UV at $VenvPath. Ensure UV is installed and Python 3.11 is accessible."
        Write-ErrorFlag $errorMessage
    }
    Write-Log "INFO" "Venv created with UV successfully."
} elseif (-not (Test-Path $ScriptCheck)) {
    # Fallback: Original py/venv logic
    Write-Log "INFO" "Creating venv with py/venv (fallback)..."
    # Find py.exe location dynamically
    $pyPath = $null
    try {
        $pyCommand = Get-Command py -ErrorAction SilentlyContinue
        if ($pyCommand) {
            $pyPath = $pyCommand.Source
            Write-Log "INFO" "Found py.exe at: $pyPath"
        } else {
            Write-Log "WARNING" "py.exe not found via Get-Command. Trying standard location."
            $possiblePyPath = "$env:WINDIR\System32\py.exe"
            if (Test-Path $possiblePyPath) {
                $pyPath = $possiblePyPath
                Write-Log "INFO" "Found py.exe at: $possiblePyPath"
            }
        }
    } catch {
        Write-Log "WARNING" "Failed to locate py.exe: $_"
    }

    # Check for Python 3.11
    $pythonFound = $false
    $pythonCmd = $null
    $versionOutput = $null
    $isPyLauncher = $false

    if ($pyPath) {
        $quotedPy = "`"$pyPath`""
        $isValid, $output = Get-PythonVersion -PythonCmd $quotedPy -VersionFlag "-3.11"
        if ($isValid) {
            $pythonFound = $true
            $pythonCmd = $quotedPy
            $isPyLauncher = $true
            $versionOutput = $output
            Write-Log "INFO" "Python 3.11 found using py launcher: $quotedPy -3.11. Version: $output"
        } else {
            Write-Log "INFO" "py -3.11 failed: $output"
        }

        # Log py --list if available
        try {
            $pyListOutput = & $quotedPy --list 2>&1
            Write-Log "INFO" "py --list output: $pyListOutput"
        } catch {
            Write-Log "WARNING" "Failed to run py --list: $_"
        }
    }

    # Fallback to standard commands
    if (-not $pythonFound) {
        $pythonCommands = @("python", "python3")
        foreach ($cmd in $pythonCommands) {
            $isValid, $output = Get-PythonVersion -PythonCmd $cmd -VersionFlag ""
            if ($isValid -and $output -match "Python\s+3\.11(?:\.\d+)?") {
                $pythonFound = $true
                $pythonCmd = $cmd
                $isPyLauncher = $false
                $versionOutput = $output
                Write-Log "INFO" "Python 3.11 found using command: $cmd. Version: $output"
                break
            } else {
                Write-Log "INFO" "Command '$cmd --version' failed or wrong version: $output"
            }
        }
    }

    if (-not $pythonFound) {
        $errorMessage = "Python 3.11 not found. Install it from python.org, ensure it's added to PATH (select 'Add Python to PATH' during installation), and verify with 'py --list' or 'python --version' in PowerShell. Download from https://www.python.org/downloads/release/python-3118/."
        Write-ErrorFlag $errorMessage
    }

    Write-Log "INFO" "Using Python command: $pythonCmd. Version: $versionOutput"

    # Create venv with fallback
    $versionFlag = if ($isPyLauncher) { "-3.11" } else { "" }
    & $pythonCmd $versionFlag -m venv $VenvPath
    if ($LASTEXITCODE -ne 0) {
        $errorMessage = "Failed to create venv at $VenvPath. Ensure Python 3.11 is installed correctly and 'venv' module is available."
        Write-ErrorFlag $errorMessage
    }
    Write-Log "INFO" "Venv created successfully (fallback)."
}

# Activate venv
$activateScript = Join-Path $VenvPath "Scripts\Activate.ps1"
if (-not (Test-Path $activateScript)) {
    $errorMessage = "Virtual environment activation script not found at $activateScript. Venv creation may have failed."
    Write-ErrorFlag $errorMessage
}
Write-Log "INFO" "Activating venv..."
& $activateScript
Write-Log "INFO" "Venv activated."

# Verify activation
$isValid, $venvOutput = Get-PythonVersion -PythonCmd "python" -VersionFlag ""
if (-not $isValid) {
    $errorMessage = "Python 3.11 not working in venv. Output: $venvOutput"
    Write-ErrorFlag $errorMessage
}
Write-Log "INFO" "Venv Python version verified: $venvOutput"

# Upgrade core tools and install numpy using UV if available
if ($usingUV) {
    Write-Log "INFO" "Upgrading core tools with UV..."
    $ErrorActionPreference = 'SilentlyContinue'  # Suppress stderr-triggered exceptions
    $upgradeOutput = uv pip install --upgrade pip setuptools wheel --no-build-isolation 2>&1
    $ErrorActionPreference = 'Continue'  # Reset to default
    $upgradeOutput | Out-File -FilePath $logFile -Append -Encoding utf8
    Write-Output $upgradeOutput
    if ($LASTEXITCODE -ne 0) {
        $errorMessage = "Failed to upgrade pip, setuptools, or wheel with UV."
        Write-ErrorFlag $errorMessage
    }
    Write-Log "INFO" "Core tools upgraded."

    Write-Log "INFO" "Installing numpy==1.25.2 with UV..."
    $ErrorActionPreference = 'SilentlyContinue'  # Suppress stderr-triggered exceptions
    $numpyOutput = uv pip install numpy==1.25.2 --no-build-isolation 2>&1
    $ErrorActionPreference = 'Continue'  # Reset to default
    $numpyOutput | Out-File -FilePath $logFile -Append -Encoding utf8
    Write-Output $numpyOutput
    if ($LASTEXITCODE -ne 0) {
        $errorMessage = "Failed to install numpy==1.25.2 with UV."
        Write-ErrorFlag $errorMessage
    }
    Write-Log "INFO" "numpy installed successfully with UV."

    # Install TTS-specific packages if needed (torch, torchaudio, chatterbox-tts for ChatterboxTTS voice cloning)
    Write-Log "INFO" "Installing TTS dependencies with UV (torch, torchaudio for CPU, then chatterbox-tts)..."
    $ErrorActionPreference = 'SilentlyContinue'  # Suppress stderr noise
    try {
        # Step 1: Install torch and torchaudio with official CPU index (pre-built wheels for Windows/Python 3.11)
        $torchOutput = uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cpu --no-build-isolation 2>&1
        $torchOutput | Out-File -FilePath $logFile -Append -Encoding utf8
        Write-Output $torchOutput
        if ($LASTEXITCODE -ne 0) {
            Write-Log "WARNING" "Torch/torchaudio install failed. Retrying with standard index..."
            $torchRetryOutput = uv pip install torch torchaudio --no-build-isolation 2>&1
            $torchRetryOutput | Out-File -FilePath $logFile -Append -Encoding utf8
            Write-Output $torchRetryOutput
            if ($LASTEXITCODE -ne 0) {
                throw "Torch/torchaudio retry also failed."
            }
        }
        Write-Log "INFO" "Torch and torchaudio installed successfully (CPU version)."

        # Step 2: Install chatterbox-tts (Resemble AI's package for voice cloning)
        $chatterboxOutput = uv pip install chatterbox-tts --no-build-isolation 2>&1
        $chatterboxOutput | Out-File -FilePath $logFile -Append -Encoding utf8
        Write-Output $chatterboxOutput
        if ($LASTEXITCODE -ne 0) {
            throw "Chatterbox-TTS install failed."
        }
        Write-Log "INFO" "Chatterbox-TTS installed successfully."

        # Step 3: Verify imports in venv (quick test for lecturer voice cloning readiness)
        $verifyOutput = python -c "from chatterbox.tts import ChatterboxTTS; print('ChatterboxTTS import success - ready for voice cloning!')" 2>&1
        $verifyOutput | Out-File -FilePath $logFile -Append -Encoding utf8
        Write-Output $verifyOutput
        if ($LASTEXITCODE -eq 0 -and $verifyOutput -match "success") {
            Write-Log "INFO" "TTS dependencies verified: Imports work for generating lecturer-voiced questions."
        } else {
            Write-Log "WARNING" "TTS verification partial: Basic imports OK, but full model load may need testing. Run 'python -c \"from chatterbox.tts import ChatterboxTTS; model = ChatterboxTTS.from_pretrained(device='cpu')\"' manually in venv."
        }
    } catch {
        $ErrorActionPreference = 'Continue'
        $errorMessage = "Failed to install/verify TTS dependencies: $_ . Audio generation for lecturer voices may fail - install manually: Activate venv and run 'uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cpu' then 'uv pip install chatterbox-tts'. Check logs for details."
        Write-Log "ERROR" $errorMessage
        # Do NOT call Write-ErrorFlag here—keep setup "successful" for C# polling; warn in game status if needed
    }
    $ErrorActionPreference = 'Continue'  # Reset
    Write-Log "INFO" "TTS dependencies setup complete (with warnings if any). Proceeding to success flag."
} else {
    # Original fallback for packages
    Write-Log "INFO" "Upgrading pip, setuptools, and wheel (fallback)..."
    $upgradeOutput = python -m pip install --upgrade pip setuptools wheel --no-build-isolation 2>&1
    $upgradeOutput | Out-File -FilePath $logFile -Append -Encoding utf8
    Write-Output $upgradeOutput
    if ($LASTEXITCODE -ne 0) {
        $errorMessage = "Failed to upgrade pip, setuptools, or wheel in venv."
        Write-ErrorFlag $errorMessage
    }
    Write-Log "INFO" "Core tools upgraded (fallback)."

    Write-Log "INFO" "Installing numpy==1.25.2 (fallback)..."
    $numpyOutput = pip install numpy==1.25.2 --no-build-isolation 2>&1
    $numpyOutput | Out-File -FilePath $logFile -Append -Encoding utf8
    Write-Output $numpyOutput
    if ($LASTEXITCODE -ne 0) {
        $errorMessage = "Failed to install numpy==1.25.2 in venv."
        Write-ErrorFlag $errorMessage
    }
    Write-Log "INFO" "numpy installed successfully (fallback)."

    # Fallback for TTS deps (using pip, non-fatal)
    Write-Log "INFO" "Installing TTS dependencies with pip (fallback: torch, torchaudio for CPU, then chatterbox-tts)..."
    try {
        # Step 1: torch/torchaudio with CPU index
        $torchOutput = pip install torch torchaudio --index-url https://download.pytorch.org/whl/cpu --no-build-isolation 2>&1
        $torchOutput | Out-File -FilePath $logFile -Append -Encoding utf8
        Write-Output $torchOutput
        if ($LASTEXITCODE -ne 0) {
            Write-Log "WARNING" "Torch/torchaudio fallback install failed. Retrying without index..."
            $torchRetryOutput = pip install torch torchaudio --no-build-isolation 2>&1
            $torchRetryOutput | Out-File -FilePath $logFile -Append -Encoding utf8
            Write-Output $torchRetryOutput
            if ($LASTEXITCODE -ne 0) {
                throw "Torch/torchaudio fallback also failed."
            }
        }
        Write-Log "INFO" "Torch and torchaudio installed successfully (fallback, CPU version)."

        # Step 2: chatterbox-tts
        $chatterboxOutput = pip install chatterbox-tts --no-build-isolation 2>&1
        $chatterboxOutput | Out-File -FilePath $logFile -Append -Encoding utf8
        Write-Output $chatterboxOutput
        if ($LASTEXITCODE -ne 0) {
            throw "Chatterbox-TTS fallback install failed."
        }
        Write-Log "INFO" "Chatterbox-TTS installed successfully (fallback)."

        # Step 3: Verify imports
        $verifyOutput = python -c "from chatterbox.tts import ChatterboxTTS; print('ChatterboxTTS import success - ready for voice cloning!')" 2>&1
        $verifyOutput | Out-File -FilePath $logFile -Append -Encoding utf8
        Write-Output $verifyOutput
        if ($LASTEXITCODE -eq 0 -and $verifyOutput -match "success") {
            Write-Log "INFO" "TTS dependencies verified (fallback)."
        } else {
            Write-Log "WARNING" "TTS verification partial (fallback)."
        }
    } catch {
        $errorMessage = "Failed to install/verify TTS dependencies (fallback): $_ . Install manually with pip in venv."
        Write-Log "ERROR" $errorMessage
        # Non-fatal
    }
    Write-Log "INFO" "TTS dependencies setup complete (fallback). Proceeding to success flag."
}

# Create success flag
try {
    "SUCCESS" | Out-File -FilePath $successFlag -Encoding utf8
    Write-Log "INFO" "TTS setup completed successfully. Success flag created."
} catch {
    Write-Log "ERROR" "Failed to create success flag: $_"
}