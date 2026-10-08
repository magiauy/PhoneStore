# Cấu hình riêng và public repository

Không commit mật khẩu, API key, chứng chỉ ký có private key hoặc bản dump chứa dữ liệu cá nhân.

## Chạy local

- Web: sao chép `PhoneStoreUser/appsettings.example.json` thành `PhoneStoreUser/appsettings.json` và điền cấu hình riêng. ASP.NET Core cũng hỗ trợ biến môi trường như `Database__Password`, `Cloudinary__ApiSecret` và `PayOS__ChecksumKey`; biến môi trường ghi đè cấu hình JSON.
- Admin: sao chép `PhoneStoreAdmin/appsettings.example.json` thành `PhoneStoreAdmin/appsettings.json` và điền cấu hình riêng. Admin hiện đọc file JSON này; không tự đọc `.env`.
- Docker: sao chép `.env.example` thành `.env`, điền mật khẩu MySQL và các khóa dịch vụ. Compose tự đọc `.env`; chạy `docker compose up --build`. File `.env` không được ASP.NET Core tự nạp khi chạy trực tiếp bằng `dotnet run`.
- Schema public ở `PhoneStoreUser/Data/Mysql/schema.sql` chỉ chứa cấu trúc bảng. Docker áp dụng schema khi volume MySQL còn trống. Database đã có volume sẽ được giữ nguyên. Không có tài khoản mặc định hay dữ liệu giao dịch trong schema.

Các file cấu hình riêng, `.env`, chứng chỉ và dump local được ignore bởi Git và loại khỏi Docker build context. Có thể giữ dump riêng ở đường dẫn local đã ignore, nhưng không thêm lại bằng `git add -f`.

## Trước khi chuyển repo sang public

1. Thu hồi và tạo lại các khóa Cloudinary, PayOS cùng mật khẩu database đã từng được commit. Viết lại lịch sử không vô hiệu hóa khóa cũ hoặc xóa bản clone/image đã tồn tại.
2. Nếu từng dùng chứng chỉ `.pfx` trong repo để ký bản phát hành, thay chứng chỉ ký. Xóa hoặc thay Docker image/build artifact cũ có thể chứa cấu hình và dump.
3. Đẩy các nhánh và tag đã làm sạch bằng force push có kiểm soát. Mọi nhánh còn trên remote cũng phải được thay thế hoặc xóa trước khi public. Các clone cũ nên được clone lại để tránh đưa lịch sử cũ trở lại.
4. Kiểm tra pull request, fork, cache và artifact trên dịch vụ Git đang sử dụng; việc sửa repo local không tự xóa các bản này.

Thư mục `.security-private/` là dữ liệu phục hồi và công cụ audit riêng trên máy; không chia sẻ hay đưa vào repository public.

Git LFS: bản dump đã xóa khỏi lịch sử có thể vẫn còn trong kho LFS trên server. Cần xóa các object đó theo cơ chế của dịch vụ hosting trước khi public. Không dùng git push --mirror, vì lệnh này có thể đẩy cả ref nội bộ của công cụ.
