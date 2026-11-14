# Hệ thống quản lý nhân sự 5S Fashion

**Đề tài** — Phân tích, thiết kế và xây dựng hệ thống quản lý nhân sự cho Công ty cổ phần 5S Fashion.

**Sinh viên thực hiện** — Vũ Hoàng Long · **Mã sinh viên** — 11235624

**Giảng viên hướng dẫn** — ThS. Trần Thanh Hải

## Bản xem trước demo web

[**Mở demo web HRMS 5S Fashion**](https://hrm-5s-fashion-demo.mewing-toque.workers.dev)

Link mở trang đăng nhập của bản demo trên Cloudflare. Bạn có thể xem trực tiếp bằng trình duyệt, không cần cài Visual Studio hoặc SQL Server.

## Chức năng

- Quản lý hồ sơ nhân viên và các danh mục dùng chung.
- Quản lý hợp đồng lao động, quyết định điều chuyển, thôi việc và khen thưởng – kỷ luật.
- Tra cứu lịch sử công tác theo khoảng thời gian để xác định đơn vị và chức vụ của nhân viên tại từng giai đoạn.
- In hợp đồng lao động và danh sách nghiệp vụ.

## Công nghệ và môi trường

Ứng dụng sử dụng ASP.NET Core MVC, Razor Views, Bootstrap, Entity Framework Core 8 và SQL Server. Bản demo chạy cục bộ trên Windows.

- Visual Studio 2022 có workload **ASP.NET and web development**.
- .NET 8 SDK.
- SQL Server và SQL Server Management Studio (SSMS).
- Kết nối Internet ở lần đầu nếu Visual Studio cần tải các gói NuGet.

## Cài đặt và chạy bản demo

### Bước 1. Chuẩn bị môi trường

- Cài đặt Visual Studio 2022 với workload **ASP.NET and web development** và **.NET 8 SDK**.
- Cài đặt SQL Server Express và SQL Server Management Studio (SSMS). Mở **Windows + R**, nhập `services.msc` và kiểm tra dịch vụ **SQL Server** đang ở trạng thái **Running**.
- Trên GitHub, chọn **Code → Download ZIP** rồi giải nén. Trong thư mục giải nén, `HRMS.sln` và `HRMS_DB.bacpac` nằm cùng cấp với thư mục `HRMS.Web`.

### Bước 2. Nhập cơ sở dữ liệu vào SQL Server

1. Mở SSMS, chọn **Windows Authentication** rồi kết nối đến SQL Server. Ghi lại tên ở ô **Server name**.
2. Trong **Object Explorer**, nhấp chuột phải vào **Databases** và chọn **Import Data-tier Application**.
3. Chọn tệp `HRMS_DB.bacpac` đi kèm bản demo. Tại ô **New database name**, nhập tên cơ sở dữ liệu bạn muốn sử dụng.
4. Chọn **Next → Finish**. Sau khi nhập thành công, làm mới mục **Databases** và kiểm tra cơ sở dữ liệu vừa tạo.

### Bước 3. Cấu hình chuỗi kết nối

Mở `HRMS.Web/appsettings.json` và cập nhật giá trị `Hrms` trong mục `ConnectionStrings`. Giữ các mục cấu hình khác trong tệp.

```json
{
  "ConnectionStrings": {
    "Hrms": "Server=TEN_MAY_CHU_SQL;Database=TEN_CO_SO_DU_LIEU;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=False"
  }
}
```

- Thay `TEN_MAY_CHU_SQL` bằng tên đã dùng để kết nối trong SSMS. Trong JSON, mỗi dấu gạch chéo ngược `\` trong tên máy chủ cần viết thành `\\`. Ví dụ, `.\SQLEXPRESS` trong SSMS được ghi là `.\\SQLEXPRESS` trong tệp JSON.
- Thay `TEN_CO_SO_DU_LIEU` bằng tên cơ sở dữ liệu đã tạo ở Bước 2 rồi lưu tệp.
- Bản demo dùng **Windows Authentication**, vì vậy tài khoản Windows chạy Visual Studio cần có quyền truy cập cơ sở dữ liệu.

### Bước 4. Mở và biên dịch dự án

1. Mở `HRMS.sln` bằng Visual Studio 2022.
2. Trong **Solution Explorer**, nhấp chuột phải vào project **HRMS.Web** và chọn **Set as Startup Project**.
3. Nếu Visual Studio chưa tự khôi phục các gói thư viện, nhấp chuột phải vào solution và chọn **Restore NuGet Packages**.
4. Chọn **Build → Build Solution** để biên dịch dự án.

### Bước 5. Thiết lập tài khoản đăng nhập demo

Ở lần chạy đầu tiên trên máy hoặc tài khoản Windows chưa được cấu hình, mở **View → Terminal** trong Visual Studio. Chuyển đến thư mục chứa `HRMS.sln` rồi chạy lệnh sau.

```powershell
dotnet run --project .\HRMS.Web\HRMS.Web.csproj --no-launch-profile -- --setup-demo-account
```

Tự chọn tên tài khoản và mật khẩu. Khi Terminal lần lượt yêu cầu **Tên tài khoản**, **Mật khẩu** và **Nhập lại mật khẩu**, nhập thông tin rồi nhấn **Enter** sau mỗi mục. Cấu hình được lưu cục bộ và dùng lại cho các lần chạy sau trên cùng tài khoản Windows.

### Bước 6. Khởi chạy và kiểm tra hệ thống

1. Chọn cấu hình chạy **HRMS.Web** trên thanh công cụ của Visual Studio rồi nhấn **Ctrl + F5**.
2. Trên trang đăng nhập, nhập tên tài khoản và mật khẩu đã thiết lập ở Bước 5. Nếu trình duyệt chưa tự mở, truy cập địa chỉ ứng dụng hiển thị trong cửa sổ chạy.
3. Mở **Hồ sơ nhân viên** và kiểm tra danh sách nhân viên. Nếu danh sách hiển thị dữ liệu mẫu, ứng dụng đã kết nối và đọc được dữ liệu từ SQL Server.

## Cấu trúc mã nguồn

| Tệp hoặc thư mục | Nội dung |
| --- | --- |
| [`HRMS.sln`](HRMS.sln) | Solution mở trong Visual Studio |
| [`HRMS_DB.bacpac`](HRMS_DB.bacpac) | Cơ sở dữ liệu nhập vào SQL Server bằng SSMS |
| [`HRMS.Web/Controllers/`](HRMS.Web/Controllers/) | Tiếp nhận và xử lý yêu cầu từ giao diện |
| [`HRMS.Web/Services/`](HRMS.Web/Services/) | Quy tắc nghiệp vụ, truy vấn và cập nhật dữ liệu |
| [`HRMS.Web/Data/`](HRMS.Web/Data/), [`HRMS.Web/Models/`](HRMS.Web/Models/) | `HrmsContext` và các thực thể ánh xạ tới SQL Server |
| [`HRMS.Web/Views/`](HRMS.Web/Views/), [`HRMS.Web/wwwroot/`](HRMS.Web/wwwroot/) | Giao diện Razor và tài nguyên tĩnh |
