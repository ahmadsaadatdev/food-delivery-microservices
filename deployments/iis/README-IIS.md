# دیپلوی روی IIS با GitHub Actions

با هر `push` روی برنچ‌های `main` / `master` / `Ahmad_Saadat`، ورک‌فلو `.github/workflows/deploy-iis.yml` پروژه را بیلد می‌کند و روی IIS می‌نشاند.

## خروجی روی سرور

| مسیر | نقش | بایندینگ |
|------|-----|----------|
| `C:\FoodDeveryServer\Gateway` | ورودی عمومی (YARP) | `*:5050` |
| `C:\FoodDeveryServer\Catalogs` | کاتالوگ | `127.0.0.1:5100` |
| `C:\FoodDeveryServer\Customers` | مشتریان | `127.0.0.1:5200` |
| `C:\FoodDeveryServer\Orders` | سفارش‌ها | `127.0.0.1:5300` |
| `C:\FoodDeveryServer\Identity` | هویت | `127.0.0.1:5400` |

آدرس عمومی: **http://localhost:5050**  
دمو: **http://localhost:5050/demo**

## پیش‌نیاز سرور IIS

1. **IIS** نصب باشد (Web Server Role).
2. **ASP.NET Core Hosting Bundle** برای .NET 10 نصب باشد:  
   https://dotnet.microsoft.com/download/dotnet/10.0
3. PostgreSQL / MongoDB / RabbitMQ مثل محیط لوکال در دسترس باشند.
4. یک **GitHub Actions Self-hosted Runner** روی همین ویندوز:

```powershell
# از Settings > Actions > Runners > New self-hosted runner
# بعد از دانلود:
.\config.cmd --url https://github.com/ahmadsaadatdev/food-delivery-microservices --token <TOKEN>
# Labels پیشنهادی: self-hosted, Windows
# Runner را با حسابی اجرا کنید که Administrator باشد (برای appcmd/IIS)
.\run.cmd
```

یا به‌صورت سرویس ویندوز:

```powershell
.\svc.cmd install
.\svc.cmd start
```

## تست دستی دیپلوی (بدون GitHub)

```powershell
dotnet publish src/ApiGateway/FoodDelivery.ApiGateway -c Release -o artifacts\iis-publish\Gateway
dotnet publish src/Services/Catalogs/FoodDelivery.Services.Catalogs.Api -c Release -o artifacts\iis-publish\Catalogs
dotnet publish src/Services/Customers/FoodDelivery.Services.Customers.Api -c Release -o artifacts\iis-publish\Customers
dotnet publish src/Services/Orders/FoodDelivery.Services.Orders.Api -c Release -o artifacts\iis-publish\Orders
dotnet publish src/Services/Identity/FoodDelivery.Services.Identity.Api -c Release -o artifacts\iis-publish\Identity

powershell -ExecutionPolicy Bypass -File deployments\iis\deploy.ps1 `
  -SourceRoot "$PWD\artifacts\iis-publish" `
  -DeployRoot "C:\FoodDeveryServer" `
  -PublicPort 5050
```
