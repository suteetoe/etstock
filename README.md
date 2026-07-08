# ET Stock

ระบบช่วยออกใบกำกับภาษีอย่างย่อสำหรับร้านค้าปลีก — จัดการสต๊อกรายเดือนและสร้าง/พิมพ์ใบกำกับภาษีอย่างย่อจากยอดขายหน้าร้าน

แอปเดสก์ท็อปด้วย Avalonia (.NET 10, MVVM) เก็บข้อมูลบน PostgreSQL พิมพ์ผ่าน Print Preview ไปยังเครื่องพิมพ์ของระบบ

## เอกสาร

- [ภาพรวมโปรเจกต์ (docs/OVERVIEW.md)](docs/OVERVIEW.md) — scope, workflow, หน้าจอ, business rules
- [Data Model (docs/DATA_MODEL.md)](docs/DATA_MODEL.md) — โครงสร้างตาราง PostgreSQL


## Setup

Database Setup
```
Host=localhost;Port=5432;Database=database;Username=postgres;Password=password
```


## Build & Publish

```
# puslish

dotnet publish ETStock\ETStock.csproj -c Release -r win-x64 --self-contained true -o publish

# build msi

dotnet build ETStock.Installer\ETStock.Installer.wixproj -c Release -p:Platform=x64

```