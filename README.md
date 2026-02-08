
# 🇵🇱 JPK VAT-7 Generator from XLSX / CSV (Blazor WASM + gRPC + .NET 9)

**jpk_vat_7_from_xlsx_csv** is a modern **.NET 9** solution for generating official Polish **JPK_V7M / JPK_V7K VAT XML files** directly from structured **Excel (.xlsx)** or **CSV** input.

This project provides:

✅ gRPC-based backend API  
✅ Blazor WebAssembly frontend client  
✅ Automatic mapping of VAT sections  
✅ XML generation + XSD validation  
✅ Ready-to-use sample Excel template  
✅ Clean modular architecture (Core / Input / Xml / Api / Web)

---

## 🚀 Why This Project?

Generating **JPK VAT-7 (JPK_V7)** files is required for every VAT-registered business in Poland.

Most solutions are expensive, closed-source, or difficult to integrate.

This project is built to be:

- **Open-source**
- **Developer-friendly**
- **Modern (.NET 9 + gRPC)**
- **Extensible for accounting systems**
- **Easy to use with Excel templates**

Perfect for:

- ERP integrations  
- Accounting automation  
- VAT reporting tools  
- Polish tax compliance systems  

---

## ✨ Key Features

### 📥 Input Support
- Excel `.xlsx` files
- CSV files
- Directory-based batch loading

### 🧠 Automatic Section Mapping
The system maps spreadsheet sections into official JPK structures:

- Naglowek
- Podmiot
- Deklaracja
- SprzedazWiersz / SprzedazCtrl
- ZakupWiersz / ZakupCtrl

### 📄 XML Generation
- Generates valid **JPK_V7 XML output**
- Supports schema-driven structure
- Modular writer implementation

### ✅ Validation
- Built-in **XSD validation**
- Ensures compliance with MF (Ministerstwo Finansów) schema

### 🌐 Web UI (Blazor WASM)
- User-friendly frontend
- Runs fully in the browser
- Upload Excel → Download XML

### ⚡ gRPC API Backend
- High-performance service layer
- gRPC-Web enabled for browser support

---

## 🏗️ Tech Stack

| Layer | Technology |
|------|------------|
| Backend API | ASP.NET Core (.NET 9) + gRPC |
| Frontend | Blazor WebAssembly + MudBlazor |
| Communication | gRPC-Web |
| Input Parsing | CSV + XLSX Readers |
| Output | XML Writer + XSD Validator |
| Architecture | Clean modular solution |

---

## 📂 Project Structure

```bash
src/
 ├── JpkVat7.Api        # gRPC API backend
 ├── JpkVat7.Core       # Domain models + mapping + abstractions
 ├── JpkVat7.Input      # XLSX/CSV readers + parsers + loaders
 ├── JpkVat7.Xml        # XML generation + validation
 ├── JpkVat7.Web        # Blazor WebAssembly frontend
samples-przyklazy-wejscia/
 └── exmpla.xlsx        # Example VAT input template
````

---

## 📊 Example Input File

Sample Excel template is included:

```bash
samples-przyklazy-wejscia/exmpla.xlsx
```

You can fill it with:

* VAT sales rows
* VAT purchase rows
* Company header data

Then generate valid JPK XML output.

---

## ▶️ Running the Project

### 1️⃣ Requirements

* .NET SDK **9.0 Preview / RC**
* Node.js (optional for frontend tooling)
* Any IDE (Rider / Visual Studio / VS Code)

---

### 2️⃣ Run gRPC API Backend

```bash
cd src/JpkVat7.Api
dotnet run
```

API will start on:

```
http://localhost:5000
```

Health check:

```bash
GET /health
```

---

### 3️⃣ Run Blazor WebAssembly Client

```bash
cd src/JpkVat7.Web/JpkVat7.Web.Wasm
dotnet run
```

Frontend runs in browser and connects via gRPC-Web.

---

## 🔌 gRPC Service Architecture

Backend uses:

* `Grpc.AspNetCore.Web`
* `GrpcWebOptions`
* Browser-enabled gRPC-Web middleware

```csharp
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.MapGrpcService<JpkGrpcService>()
   .EnableGrpcWeb();
```

Client connects using:

```csharp
var handler = new GrpcWebHandler(
    GrpcWebMode.GrpcWebText,
    new HttpClientHandler()
);

var channel = GrpcChannel.ForAddress(apiBaseUrl,
    new GrpcChannelOptions { HttpHandler = handler });
```

---

## 📌 Use Cases

This solution can be used for:

* Polish VAT automation systems
* Exporting JPK_V7 for accountants
* ERP integration with MF XML schema
* Modern SaaS tax reporting apps
* Internal compliance tooling

---

## 🤝 Contributing

Contributions are welcome!

If you want to add features or improve schema support:

1. Fork the repo
2. Create a feature branch
3. Submit a Pull Request

---

## 📜 License

MIT License — free to use in commercial and open-source projects.

---

## 🔍 SEO Keywords (Search Visibility)

JPK VAT-7 generator, JPK_V7M XML export, Polish VAT reporting tool,
Excel to JPK converter, .NET 9 gRPC VAT system, Blazor WASM tax app,
Ministerstwo Finansów JPK schema validation, VAT automation Poland

---

## ⭐ Support

If this project helps you, consider giving it a ⭐ on GitHub
and sharing it with other .NET developers working with Polish tax systems.

```
