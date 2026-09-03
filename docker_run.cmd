@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "HOST_PORT=18080"
set "CONTAINER_NAME=file-processing-service"
set "CONTAINER_TAG=default"
set "IMAGE_REPOSITORY=file-processing-service"
set "API_KEY="
set "API_KEY_AUTO_GENERATED=false"

for %%I in ("%~dp0.") do set "REPOSITORY_ROOT=%%~fI"

goto parse_arguments

:parse_arguments
if "%~1"=="" goto validate_arguments

if /I "%~1"=="--host-port" goto set_host_port
if /I "%~1"=="--api-key" goto set_api_key
if /I "%~1"=="--container-name" goto set_container_name
if /I "%~1"=="--container-tag" goto set_container_tag
if /I "%~1"=="--help" goto show_help
if /I "%~1"=="-h" goto show_help

echo Error: Unknown argument "%~1".
echo.
goto show_help_error

:set_host_port
if "%~2"=="" (
    echo Error: --host-port requires a value.
    exit /b 2
)
set "HOST_PORT=%~2"
shift
shift
goto parse_arguments

:set_api_key
if "%~2"=="" (
    echo Error: --api-key requires a value.
    exit /b 2
)
set "API_KEY=%~2"
shift
shift
goto parse_arguments

:set_container_name
if "%~2"=="" (
    echo Error: --container-name requires a value.
    exit /b 2
)
set "CONTAINER_NAME=%~2"
shift
shift
goto parse_arguments

:set_container_tag
if "%~2"=="" (
    echo Error: --container-tag requires a value.
    exit /b 2
)
set "CONTAINER_TAG=%~2"
shift
shift
goto parse_arguments

:validate_arguments
set "RUN_HOST_PORT=%HOST_PORT%"
powershell.exe -NoProfile -Command ^
    "$port = 0; if (-not [int]::TryParse($env:RUN_HOST_PORT, [ref]$port) -or $port -lt 1 -or $port -gt 65535) { exit 1 }"

if errorlevel 1 (
    echo Error: --host-port must be a number from 1 through 65535.
    exit /b 2
)

set "RUN_CONTAINER_NAME=%CONTAINER_NAME%"
powershell.exe -NoProfile -Command ^
    "if ($env:RUN_CONTAINER_NAME -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]*$') { exit 1 }"

if errorlevel 1 (
    echo Error: --container-name contains invalid characters.
    exit /b 2
)

set "RUN_CONTAINER_TAG=%CONTAINER_TAG%"
powershell.exe -NoProfile -Command ^
    "if ($env:RUN_CONTAINER_TAG -notmatch '^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$') { exit 1 }"

if errorlevel 1 (
    echo Error: --container-tag contains invalid characters.
    exit /b 2
)

if defined API_KEY goto validate_api_key

for /f "usebackq delims=" %%K in (`powershell.exe -NoProfile -Command ^
    "$bytes = New-Object byte[] 16; " ^
    "$generator = [Security.Cryptography.RandomNumberGenerator]::Create(); " ^
    "try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }; " ^
    "[BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()"`) do (
    set "API_KEY=%%K"
)

set "API_KEY_AUTO_GENERATED=true"

:validate_api_key
set "RUN_API_KEY=%API_KEY%"
powershell.exe -NoProfile -Command ^
    "$value = $env:RUN_API_KEY; " ^
    "if ([string]::IsNullOrEmpty($value) -or $value.Length -lt 32 -or $value.Length -gt 128 -or $value -notmatch '^[!-~]+$') { exit 1 }"

if errorlevel 1 (
    echo Error: The API key must contain 32 through 128 visible ASCII characters without spaces.
    exit /b 2
)

set "IMAGE_REFERENCE=%IMAGE_REPOSITORY%:%CONTAINER_TAG%"

where docker.exe >nul 2>&1
if errorlevel 1 (
    echo Error: Docker was not found. Install or start Docker Desktop first.
    exit /b 1
)

docker info >nul 2>&1
if errorlevel 1 (
    echo Error: The Docker engine is not available. Start Docker Desktop first.
    exit /b 1
)

set "EXISTING_IMAGE="
for /f "usebackq delims=" %%I in (`docker inspect --format "{{.Config.Image}}" "%CONTAINER_NAME%" 2^>nul`) do (
    set "EXISTING_IMAGE=%%I"
)

if not defined EXISTING_IMAGE goto check_image

if /I not "%EXISTING_IMAGE%"=="%IMAGE_REFERENCE%" (
    echo Error: A container named "%CONTAINER_NAME%" already exists.
    echo Existing image: %EXISTING_IMAGE%
    echo Requested image: %IMAGE_REFERENCE%
    echo.
    echo The existing container was not removed because its image or tag differs.
    exit /b 1
)

set "CONTAINER_RUNNING="
for /f "usebackq delims=" %%R in (`docker inspect --format "{{.State.Running}}" "%CONTAINER_NAME%" 2^>nul`) do (
    set "CONTAINER_RUNNING=%%R"
)

if /I "%CONTAINER_RUNNING%"=="true" (
    echo Stopping the existing matching container...
    docker stop "%CONTAINER_NAME%" >nul
    if errorlevel 1 (
        echo Error: The existing container could not be stopped.
        exit /b 1
    )
)

docker inspect "%CONTAINER_NAME%" >nul 2>&1
if not errorlevel 1 (
    echo Removing the existing matching container...
    docker rm "%CONTAINER_NAME%" >nul
    if errorlevel 1 (
        echo Error: The existing container could not be removed.
        exit /b 1
    )
)

:check_image
docker image inspect "%IMAGE_REFERENCE%" >nul 2>&1
if not errorlevel 1 goto run_container

echo Image "%IMAGE_REFERENCE%" was not found locally.
echo Building the Docker image...

docker build ^
    --file "%REPOSITORY_ROOT%\Dockerfile" ^
    --tag "%IMAGE_REFERENCE%" ^
    "%REPOSITORY_ROOT%"

if errorlevel 1 (
    echo Error: Docker image build failed.
    exit /b 1
)

:run_container
echo.
echo Starting the file-processing service...
echo Container: %CONTAINER_NAME%
echo Image:     %IMAGE_REFERENCE%
echo API URL:   http://localhost:%HOST_PORT%
echo Swagger:   http://localhost:%HOST_PORT%/swagger
echo.

if /I "%API_KEY_AUTO_GENERATED%"=="true" (
    echo The automatically generated API key will not be displayed.
    echo Use docker inspect from another terminal to retrieve it manually.
)

echo Press Ctrl+C to stop the service.
echo.

docker run ^
    --rm ^
    --name "%CONTAINER_NAME%" ^
    --publish "%HOST_PORT%:8080" ^
    --env "Authentication__SharedKey=%API_KEY%" ^
    "%IMAGE_REFERENCE%"

exit /b %errorlevel%

:show_help
echo Usage:
echo   docker_run.cmd [options]
echo.
echo Options:
echo   --host-port VALUE       Host port. Default: 18080
echo   --api-key VALUE         API key. Generated when omitted.
echo   --container-name VALUE  Container name. Default: file-processing-service
echo   --container-tag VALUE   Image tag. Default: default
echo   --help, -h              Display this help.
echo.
echo Example:
echo   docker_run.cmd --host-port 19090 --container-name file-service-demo --container-tag test
exit /b 0

:show_help_error
echo Usage:
echo   docker_run.cmd [--host-port VALUE] [--api-key VALUE] [--container-name VALUE] [--container-tag VALUE]
exit /b 2