# SQL Server 資料字典產生器

這是一個使用 .NET 9 Razor Pages 開發的 Web 應用程式，可以連線至 SQL Server 資料庫並自動產生資料表的 Data Dictionary (DD) 文件（Markdown 格式）。

## 功能特色

- ✅ 支援 **Windows 驗證** 和 **SQL Server 驗證 (SA)** 兩種連線方式
- ✅ 自動列出所有可用的資料庫
- ✅ 顯示資料庫中的所有資料表
- ✅ 支援單選或全選資料表
- ✅ 自動產生 Markdown 格式的 DD 文件
- ✅ 包含完整的欄位資訊：
  - 欄位名稱
  - 資料類型與長度
  - 是否允許 NULL
  - 主鍵標記
  - Identity 標記
  - 預設值
  - 欄位說明 (MS_Description)

## 系統需求

- .NET 9.0 SDK
- SQL Server (任何版本)
- Windows 作業系統（用於 Windows 驗證）

## 安裝與執行

### 1. 安裝相依套件

```bash
cd SqlServerDDGenerator
dotnet restore
```

### 2. 執行應用程式

```bash
dotnet run
```

### 3. 開啟瀏覽器

預設會在 `https://localhost:7285` 或 `http://localhost:5066` 開啟

## 使用步驟

### 步驟 1: 資料庫連線設定

1. 輸入 **伺服器名稱**（例如：`localhost`、`127.0.0.1`、`.\SQLEXPRESS`）
2. 選擇 **驗證方式**：
   - **Windows 驗證**：使用當前 Windows 使用者身份
   - **SQL Server 驗證 (SA)**：需要輸入使用者名稱和密碼
3. 如果選擇 **SQL Server 驗證**，可以勾選 **信任伺服器憑證 (TrustServerCertificate)**
   - 適用於開發環境或使用自簽憑證的情況
   - 正式環境建議使用有效的 SSL 憑證
4. 點擊「測試連線並載入資料庫」

### 步驟 2: 選擇資料庫

1. 從下拉選單中選擇要產生 DD 的資料庫
2. 點擊「載入資料表」

### 步驟 3: 選擇資料表

1. 勾選要產生 DD 的資料表（可以使用「全選」或「取消全選」按鈕）
2. 點擊「產生 DD 文件」

### 步驟 4: 完成

- 系統會顯示產生成功的訊息
- DD 文件會儲存在 `wwwroot/output/` 目錄下
- 檔案命名格式：`{資料庫名稱}_DD_{日期時間}.md`
- 頁面上會顯示預覽內容

## 輸出範例

產生的 Markdown 文件格式如下：

```markdown
# Database: YourDatabase
**Server:** localhost
**Generated:** 2025-11-11 13:46:00

---

## Table: dbo.Users

| Column Name | Data Type | Length | Nullable | Primary Key | Identity | Default | Description |
|-------------|-----------|--------|----------|-------------|----------|---------|-------------|
| UserID | int |  | NO | YES | YES |  | 使用者ID |
| UserName | nvarchar(50) | 50 | NO | NO | NO |  | 使用者名稱 |
| Email | nvarchar(100) | 100 | YES | NO | NO |  | 電子郵件 |
| CreatedDate | datetime |  | NO | NO | NO | (getdate()) | 建立日期 |

---
```

## 專案結構

```
SqlServerDDGenerator/
├── Models/
│   ├── ConnectionInfo.cs      # 連線資訊模型
│   ├── TableInfo.cs           # 資料表資訊模型
│   └── ColumnInfo.cs          # 欄位資訊模型
├── Services/
│   └── SqlServerService.cs    # SQL Server 操作服務
├── Pages/
│   ├── Index.cshtml           # 主頁面 UI
│   └── Index.cshtml.cs        # 主頁面邏輯
├── wwwroot/
│   └── output/                # DD 文件輸出目錄
└── Program.cs                 # 應用程式進入點
```

## 技術棧

- **框架**: ASP.NET Core 9.0 Razor Pages
- **資料庫**: Microsoft.Data.SqlClient 6.1.2
- **語言**: C# 13

## 注意事項

1. **Windows 驗證**：需要執行應用程式的帳號對 SQL Server 有適當的存取權限
2. **SQL Server 驗證**：請確保 SQL Server 已啟用混合驗證模式
3. **防火牆**：確保 SQL Server 的連接埠（預設 1433）可以存取
4. **權限**：需要對目標資料庫有讀取權限（SELECT 權限）

## 安全建議

- 不要在正式環境中儲存資料庫密碼
- 建議使用 Windows 驗證
- 可考慮將輸出目錄加入 `.gitignore`

## 授權

本專案為開源專案，可自由使用和修改。

## 作者

SQL Server DD Generator Team
