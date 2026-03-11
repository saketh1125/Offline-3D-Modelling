@echo off
REM Unity + Flutter NDK Conflict Fix Script v2
REM This script helps resolve NDK version conflicts between Unity and Flutter

echo ========================================
echo Unity + Flutter NDK Conflict Fix v2
echo ========================================
echo.

REM Check if Unity NDK exists
set UNITY_NDK_PATH=C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data\PlaybackEngines\AndroidPlayer\NDK
if not exist "%UNITY_NDK_PATH%" (
    echo WARNING: Unity NDK not found at default location
    echo Please update the path in unityLibrary\build.gradle
    echo.
)

REM Create/update local.properties
echo Updating local.properties...
if not exist "android\local.properties" (
    if exist "android\local.properties.template" (
        copy "android\local.properties.template" "android\local.properties"
        echo Created local.properties from template
    ) else (
        echo Creating local.properties...
        echo sdk.dir=C:\Users\%USERNAME%\AppData\Local\Android\Sdk > "android\local.properties"
        echo flutter.sdk=C:\flutter >> "android\local.properties"
        echo unity.ndk.path=%UNITY_NDK_PATH% >> "android\local.properties"
    )
)

REM Check if Unity NDK path exists in local.properties
findstr /C:"unity.ndk.path" "android\local.properties" >nul
if %errorlevel% neq 0 (
    echo Adding Unity NDK path to local.properties...
    echo unity.ndk.path=%UNITY_NDK_PATH% >> "android\local.properties"
)

echo.
echo IMPORTANT: Please verify these paths in android\local.properties:
echo - flutter.sdk (should point to your Flutter installation)
echo - sdk.dir (should point to your Android SDK)
echo - unity.ndk.path (should point to Unity's NDK)
echo.

REM Clean all build caches
echo Cleaning build caches...

REM Stop Gradle daemons first
cd android
call gradle --stop

REM Clean project Gradle caches
if exist ".gradle" rmdir /s /q ".gradle"
if exist "build" rmdir /s /q "build"
if exist "unityLibrary\build" rmdir /s /q "unityLibrary\build"
if exist "unityLibrary\.gradle" rmdir /s /q "unityLibrary\.gradle"
if exist "app\build" rmdir /s /q "app\build"
if exist "app\.gradle" rmdir /s /q "app\.gradle"

echo.
echo Cleaning Flutter cache...
cd ..
call flutter clean

echo.
echo Cleaning Gradle user home cache...
if exist "%USERPROFILE%\.gradle\caches" rmdir /s /q "%USERPROFILE%\.gradle\caches"
if exist "%USERPROFILE%\.gradle\daemon" rmdir /s /q "%USERPROFILE%\.gradle\daemon"

echo.
echo ========================================
echo Fix complete! The build should now work with:
echo - Unity Library using NDK 23.x (Unity's bundled)
echo - Flutter app using NDK 27.x (for plugins)
echo.
echo Now run:
echo 1. flutter pub get
echo 2. flutter run
echo ========================================
echo.
pause
