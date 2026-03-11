@echo off
REM Gradle Cache Corruption Fix Script
REM This script completely cleans Gradle caches to resolve metadata corruption

echo ========================================
echo Gradle Cache Corruption Fix
echo ========================================
echo.

echo Stopping all Gradle daemons...
call gradle --stop

echo.
echo Cleaning Gradle caches...

REM Clean project Gradle caches
if exist "android\.gradle" (
    echo Removing android\.gradle...
    rmdir /s /q "android\.gradle"
)

if exist "android\build" (
    echo Removing android\build...
    rmdir /s /q "android\build"
)

if exist "android\unityLibrary\build" (
    echo Removing android\unityLibrary\build...
    rmdir /s /q "android\unityLibrary\build"
)

if exist "android\unityLibrary\.gradle" (
    echo Removing android\unityLibrary\.gradle...
    rmdir /s /q "android\unityLibrary\.gradle"
)

if exist "android\app\build" (
    echo Removing android\app\build...
    rmdir /s /q "android\app\build"
)

if exist "android\app\.gradle" (
    echo Removing android\app\.gradle...
    rmdir /s /q "android\app\.gradle"
)

REM Clean user Gradle caches
if exist "%USERPROFILE%\.gradle\caches" (
    echo Removing user Gradle caches...
    rmdir /s /q "%USERPROFILE%\.gradle\caches"
)

if exist "%USERPROFILE%\.gradle\daemon" (
    echo Removing Gradle daemon...
    rmdir /s /q "%USERPROFILE%\.gradle\daemon"
)

REM Clean Flutter cache
echo.
echo Cleaning Flutter cache...
call flutter clean

REM Clean Kotlin daemon cache (optional but helps)
echo.
echo Cleaning Kotlin daemon cache...
if exist "%USERPROFILE%\.kotlin\daemon" (
    rmdir /s /q "%USERPROFILE%\.kotlin\daemon"
)

echo.
echo ========================================
echo Cache cleanup complete!
echo.
echo Now run:
echo 1. flutter pub get
echo 2. flutter run
echo.
echo If you still get errors, try running:
echo gradlew clean --refresh-keys
echo ========================================
echo.
pause
