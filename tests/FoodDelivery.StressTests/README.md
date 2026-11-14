# Food Delivery Stress Tests (NBomber)

سناریوهای بار روی APIهای لوکال:

| Scenario | نوع بار |
|----------|---------|
| `get_products` | خواندن پرتکرار کاتالوگ |
| `get_customers` | خواندن لیست مشتریان |
| `register_users` | ثبت‌نام همزمان |
| `end_to_end_order` | ثبت‌نام → Customer → Products → Order |
| `mixed_read_burst` | burst همزمان روی products/customers/orders |

## پیش‌نیاز

سرویس‌ها باید بالا باشند:

```powershell
dotnet run --project src/Services/Catalogs/FoodDelivery.Services.Catalogs.Api --launch-profile http
dotnet run --project src/Services/Customers/FoodDelivery.Services.Customers.Api --launch-profile http
dotnet run --project src/Services/Orders/FoodDelivery.Services.Orders.Api --launch-profile http
dotnet run --project src/Services/Identity/FoodDelivery.Services.Identity.Api --launch-profile http
```

و RabbitMQ روی `127.0.0.1:5672`.

## اجرا

```powershell
# از ریشه ریپو
dotnet run --project tests/FoodDelivery.StressTests -c Release
```

یا با اسکریپت:

```powershell
./tests/FoodDelivery.StressTests/run-stress.ps1
```

## تنظیم شدت بار (اختیاری)

```powershell
$env:FD_STRESS_DURATION = "60"      # ثانیه
$env:FD_STRESS_READ_RPS = "60"      # خواندن در ثانیه
$env:FD_STRESS_WRITE_RPS = "10"     # نوشتن در ثانیه
$env:FD_STRESS_COPIES = "30"        # کاربران همزمان
dotnet run --project tests/FoodDelivery.StressTests -c Release
```

گزارش HTML داخل `tests/FoodDelivery.StressTests/stress-reports/` ساخته می‌شود.
