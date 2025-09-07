param (
    [string]$VenvPath = "$env:APPDATA\DHBW-Game\tts_venv"
)

$PythonExe = "py"  # The base executable
$PythonVersionArg = "-3.11"  # The version argument to force 3.11

# Check Python version to ensure compatibility
$PythonVersion = & $PythonExe $PythonVersionArg --version 2>&1
if ($PythonVersion -notmatch "3.11") {
    Write-Error "Python 3.11 not found or mismatched version. Install from python.org and ensure 'py -3.11' works."
    exit 1
}

Write-Output "Setting up TTS venv for lecturer voice cloning at: $VenvPath using Python 3.11"

# Create the venv folder if it doesn't exist (New-Item handles parents)
if (-not (Test-Path $VenvPath)) {
    New-Item -ItemType Directory -Path $VenvPath -Force | Out-Null
}

# Create venv if Scripts\python.exe doesn't exist
$ScriptCheck = Join-Path $VenvPath "Scripts\python.exe"
if (-not (Test-Path $ScriptCheck)) {
    & $PythonExe $PythonVersionArg -m venv $VenvPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create venv. Ensure Python 3.11 is installed and in PATH."
        exit 1
    }
}

# Activate venv
& "$VenvPath\Scripts\Activate.ps1"

# Upgrade core tools (using venv's python)
python -m pip install --upgrade pip setuptools wheel --no-build-isolation

# Pre-install numpy to fix pkuseg build issue
pip install numpy==1.25.2 --no-build-isolation

# Install Chatterbox TTS
pip install chatterbox-tts --no-build-isolation

# Test import (using venv's python)
python -c "from chatterbox.tts import ChatterboxTTS; print('Chatterbox TTS ready for lecturer voices!')"

deactivate

Write-Output "TTS venv setup complete! Next up: tts_controller.py for cloning prof samples."