@echo off
:: Chuyen thu muc lam viec ve noi chua file .bat nay
cd /d "%~dp0"

echo ========================================================
echo   KHOI DONG TAILWIND CSS CLI (Watch Mode)
echo ========================================================

:: 1. Kiem tra Node.js/NPM co ton tai khong
where npm >nul 2>nul
if %errorlevel% neq 0 (
    echo [LOI] Khong tim thay lenh 'npm'. Vui long cai dat Node.js tu https://nodejs.org/
    pause
    exit /b
)

:: 2. Kiem tra thu vien @tailwindcss/cli
:: Kiem tra nhanh bang cach xem thu muc trong node_modules co ton tai khong
if exist "node_modules\@tailwindcss\cli" (
    echo [TRANG THAI] Da tim thay thu vien @tailwindcss/cli.
) else (
    echo [THONG BAO] Khong tim thay thu vien @tailwindcss/cli.
    echo Dang tien hanh cai dat tu dong...
    
    :: Tao package.json neu chua co de tranh loi
    if not exist "package.json" (
        echo [INFO] Khoi tao package.json mac dinh...
        call npm init -y >nul
    )
    
    :: Cai dat thu vien
    call npm install -D @tailwindcss/cli
    
    if %errorlevel% neq 0 (
        echo [LOI] Cai dat that bai. Vui long kiem tra ket noi internet.
        pause
        exit /b
    )
    echo [THANH CONG] Da cai dat xong.
)

echo.
echo Input File:  ./wwwroot/css/input.css
echo Output File: ./wwwroot/app.min.css
echo.

:: 3. Thuc thi lenh npx
:: Them co --yes de dam bao npx khong hoi xac nhan (y/n)
call npx --yes @tailwindcss/cli -i ./wwwroot/css/input.css -o ./wwwroot/app.min.css --watch --verbose

:: Giu man hinh neu tien trinh bi dung dot ngot
if %errorlevel% neq 0 (
    echo.
    echo --------------------------------------------------------
    echo [CANH BAO] Tien trinh da dung lai hoac gap loi.
    echo --------------------------------------------------------
    pause
)