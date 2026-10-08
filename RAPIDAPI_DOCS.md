# RapidAPI Descriptions (Copy & Paste these into the 'General' tab)

## Short Description

AI-powered OCR API to extract JSON data from Purchase Orders, Delivery Notes, and Invoices. Automate logistics document parsing instantly.

## Long Description

**WareDocs API** is an enterprise-grade AI JSON parser designed specifically for logistics automation. It safely and accurately turns raw PDFs and images into structured JSON data.

Traditional OCR tools require rigid bounding boxes and templates. WareDocs uses advanced AI to adapt to any layout—allowing you to extract line items, prices, SKUs, and supplier data from Purchase Orders, Delivery Notes, and Invoices with zero setup.

**Key Features:**

- **Document Detection:** Automatically identifies whether an uploaded file is a PO, Invoice, or Delivery Note.
- **Intelligent Extraction:** Extracts SKUs, quantities, and pricing using pure AI context.
- **Discrepancy Matching:** Upload a PO, Delivery Note, and Invoice together, and our API will automatically flag missing items, over-shipments, or price mismatches!
- **Barcode & SKU Normalization:** Built-in utility endpoints for warehouse label management.

Stop wasting engineering hours maintaining OCR software. Automate your supply chain document processing today!

---

# RapidAPI Docs Tutorial (Copy & Paste this into the 'Docs' tab)

Welcome to the WareDocs API! Our API takes unstructured warehouse documents and returns perfectly structured JSON.

## 🚀 Quick Start: Extracting a Document

Here is a quick example of how to extract data from an Invoice or Purchase Order.

### Python Example

```python
import requests

url = "https://waredocs-api.p.rapidapi.com/extract"  # (Replace with your actual RapidAPI endpoint)
headers = {
    "X-RapidAPI-Key": "YOUR_RAPIDAPI_KEY_HERE",
    "X-RapidAPI-Host": "YOUR_RAPIDAPI_HOST_HERE"
}

# Open the PDF or image file you want to extract data from
files = {
    "file": ("invoice.pdf", open("path/to/your/invoice.pdf", "rb"), "application/pdf")
}

response = requests.post(url, headers=headers, files=files)

print("Status Code:", response.status_code)
print("Extracted JSON Data:", response.json())
```

### JavaScript (Node.js) / Axios Example

```javascript
const axios = require('axios');
const FormData = require('form-data');
const fs = require('fs');

const form = new FormData();
form.append("file", fs.createReadStream("path/to/your/invoice.pdf"));

const options = {
  method: 'POST',
  url: 'https://waredocs-api.p.rapidapi.com/extract', // (Replace with your actual RapidAPI endpoint)
  headers: {
    'X-RapidAPI-Key': 'YOUR_RAPIDAPI_KEY_HERE',
    'X-RapidAPI-Host': 'YOUR_RAPIDAPI_HOST_HERE',
    ...form.getHeaders()
  },
  data: form
};

axios.request(options).then(response => {
  console.log("Extracted JSON Data:", response.data);
}).catch(error => {
  console.error(error);
});
```

## 🎯 Important Endpoints

1. **`/extract`:** Feed incoming emails directly into the API and sync the JSON response directly to your ERP or QuickBooks.
2. **`/match`:** Use this endpoint when a truck arrives to instantly compare the Purchase Order against the physical Delivery Note and highlight discrepancies automatically.
