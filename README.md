<h1>🇵🇱 JPK VAT-7 Generator (Excel/CSV → JPK_V7 XML)</h1>

<p>
This repository contains a working <strong>.NET 9 + gRPC + Blazor WebAssembly</strong> solution that generates official Polish
<strong>JPK_V7M / JPK_V7K VAT XML</strong> files from structured <strong>Excel (.xlsx)</strong> or <strong>CSV</strong> input files.
</p>

<p>
Goal:
Upload VAT data in an Excel/CSV template → generate a valid JPK XML file → download it.
</p>

---

<h2>✅ What This Project Does</h2>

<p>
This app automates the creation of <strong>JPK VAT-7 (JPK_V7)</strong> files required for Polish VAT reporting.
</p>

It includes:

- gRPC backend API for processing input files  
- Blazor WebAssembly frontend for upload/download  
- Excel + CSV parsing  
- Automatic mapping into official JPK sections  
- XML generation + XSD schema validation  

---

<h2>📌 When Is This Useful?</h2>

<p>
You can use this project if you are building:
</p>

- Accounting automation tools  
- ERP integrations  
- Internal VAT reporting systems  
- Polish tax compliance SaaS platforms  

<p>
Instead of manually filling government tools, you generate XML directly from spreadsheets.
</p>

---

<h2>✨ Main Features</h2>

- Excel (.xlsx) input support  
- CSV input support  
- Batch loading from directories  
- Clean modular architecture  
- Generates full official XML structure  
- Built-in XSD validation (MF schema)

<p><strong>JPK sections covered:</strong></p>

- Naglowek  
- Podmiot  
- Deklaracja  
- SprzedazWiersz / SprzedazCtrl  
- ZakupWiersz / ZakupCtrl  

---

<h2>🏗️ Solution Structure</h2>

```bash
src/
 ├── JpkVat7.Api        # Backend gRPC API (.NET)
 ├── JpkVat7.Core       # Domain models + mapping logic
 ├── JpkVat7.Input      # XLSX/CSV parsing + loaders
 ├── JpkVat7.Xml        # XML writer + XSD validator
 ├── JpkVat7.Web        # Blazor WebAssembly frontend
samples-przyklazy-wejscia/
 └── example_sectioned.xlsx
````

---

<h2>📂 Example Input Template</h2>

<p>
A ready-to-use Excel template is included here:
</p>

```bash
samples-przyklazy-wejscia/example_sectioned.xlsx
```

Fill in:

* Company header info
* VAT sales rows
* VAT purchase rows

<p>
Then upload it in the frontend to generate XML.
</p>

---

<h2>▶️ How to Run the Project (Step-by-Step)</h2>

<h3>1️⃣ Requirements</h3>

* .NET SDK 9.0 Preview/RC
* Visual Studio / Rider / VS Code

Check:

```bash
dotnet --version
```

You should see:

```bash
9.0.x
```

---

<h2>🚀 Running Backend + Frontend</h2>

<p>
The system has two parts:
</p>

* Backend API → `JpkVat7.Api`
* Frontend UI → `JpkVat7.Web.Wasm`

<p>
You need both running.
</p>

---

<h3>2️⃣ Start the gRPC Backend API</h3>

```bash
cd src/JpkVat7.Api
dotnet run
```

Backend starts on:

```text
http://localhost:5000
https://localhost:7000
```

<p><strong>Health check:</strong></p>

```text
GET http://localhost:5000/health
```

If it returns `"Healthy"` → backend works.

---

<h3>3️⃣ Start the Blazor WebAssembly Client</h3>

```bash
cd src/JpkVat7.Web/JpkVat7.Web.Wasm
dotnet run
```

Frontend runs at:

```text
https://localhost:xxxx
```

Open it in your browser.

---

<h2>🔌 How Frontend Connects to Backend</h2>

<p>
Browsers do not support raw gRPC directly, so this project uses <strong>gRPC-Web</strong>.
</p>

Backend enables it in `Program.cs`:

```csharp
app.UseGrpcWeb(new GrpcWebOptions
{
    DefaultEnabled = true
});

app.MapGrpcService<JpkGrpcService>()
   .EnableGrpcWeb();
```

Frontend connects using:

```csharp
var handler = new GrpcWebHandler(
    GrpcWebMode.GrpcWebText,
    new HttpClientHandler()
);

var channel = GrpcChannel.ForAddress(apiBaseUrl,
    new GrpcChannelOptions
    {
        HttpHandler = handler
    });
```

---

<h2>✅ Typical Workflow</h2>

1. Run backend (`JpkVat7.Api`)
2. Run frontend (`JpkVat7.Web.Wasm`)
3. Open browser UI
4. Upload Excel template
5. Backend generates JPK XML
6. Download the final file

---

<h2>🤝 Contributing</h2>

Pull requests are welcome.

1. Fork the repo
2. Create a branch:

```bash
git checkout -b feature/my-change
```

3. Commit + push
4. Open a Pull Request

---

<h2>📜 License</h2>

<p>
This project is licensed under the <strong>GNU General Public License v3.0</strong>.
See <a href="./License.md">License.md</a>.
</p>

---

<h2>⭐ Support</h2>

<p>
If this repo helped you, consider giving it a ⭐ on GitHub.
</p>
