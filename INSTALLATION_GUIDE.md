# راهنمای نصب و استفاده - HearLoveen Complete Package

## 📦 محتویات پکیج

این پکیج شامل 5 بخش کامل است:

```
hearloveen-complete/
├── 01-clinical-validation/          # اعتبارسنجی بالینی
├── 02-business-model/               # مدل کسب‌وکار
├── 03-competitive-analysis/         # تحلیل رقبا
├── 04-explainable-ai/              # هوش مصنوعی قابل توضیح
├── 05-longitudinal-study/          # مطالعه طولی
├── EXECUTIVE_SUMMARY.md            # خلاصه اجرایی
└── README.md                       # فایل راهنما
```

---

## 🚀 شروع سریع

### گام 1: بررسی خلاصه اجرایی
```bash
# ابتدا این فایل را بخوانید
open EXECUTIVE_SUMMARY.md
```

### گام 2: Clinical Validation
```bash
cd 01-clinical-validation

# مطالعه پروتکل RCT
open protocols/rct-protocol.md

# بررسی فرم رضایت‌نامه
open consent-forms/informed-consent.md

# نصب وابستگی‌های Python برای آنالیز
cd statistical-analysis
pip install -r requirements.txt

# اجرای آنالیز (با داده نمونه)
python rct_analysis.py
```

### گام 3: Business Model
```bash
cd 02-business-model

# بررسی Business Model Canvas
open canvas/business-model-canvas.md

# محاسبه Unit Economics
# (Excel/Google Sheets را باز کنید و محاسبات را مرور کنید)
```

### گام 4: Competitive Analysis
```bash
cd 03-competitive-analysis

# مطالعه تحلیل رقبا
open competitor-research/competitive-analysis.md

# این سند شامل:
# - تحلیل 5 رقیب اصلی
# - SWOT analysis
# - Competitive matrix
# - Strategic recommendations
```

### گام 5: Explainable AI
```bash
cd 04-explainable-ai/backend

# نصب وابستگی‌ها
pip install -r requirements.txt

# اجرای API server (در حالت development)
python explainable_api.py

# API در دسترس خواهد بود در:
# http://localhost:8000
# Swagger UI: http://localhost:8000/docs
```

### گام 6: Longitudinal Study
```bash
cd 05-longitudinal-study

# مطالعه طراحی مطالعه طولی
open study-design/longitudinal-protocol.md

# این شامل:
# - طراحی 18 ماهه
# - آنالیز آماری
# - Power analysis
# - Missing data strategy
```

---

## 📋 پیش‌نیازها

### برای Clinical Validation:
- Python 3.8+
- کتابخانه‌های آماری: `scipy`, `statsmodels`, `scikit-learn`
- R (اختیاری، برای آنالیزهای پیشرفته)

### برای Explainable AI:
- Python 3.8+
- PyTorch 2.0+
- SHAP library
- FastAPI

### برای Business Model:
- Microsoft Excel یا Google Sheets
- Adobe Acrobat یا مرورگر (برای PDFها)

---

## 🔧 نصب وابستگی‌ها

### روش 1: استفاده از pip
```bash
# برای Clinical Validation
cd 01-clinical-validation/statistical-analysis
pip install numpy pandas scipy statsmodels scikit-learn matplotlib seaborn

# برای Explainable AI
cd 04-explainable-ai/backend
pip install -r requirements.txt
```

### روش 2: استفاده از conda
```bash
# ایجاد محیط مجازی
conda create -n hearloveen python=3.9
conda activate hearloveen

# نصب وابستگی‌ها
pip install -r requirements.txt
```

### روش 3: استفاده از Docker (توصیه می‌شود)
```bash
cd 04-explainable-ai/backend

# ساخت image
docker build -t hearloveen-explainable-ai .

# اجرا
docker run -p 8000:8000 hearloveen-explainable-ai
```

---

## 📊 استفاده از بخش‌های مختلف

### 1. اجرای RCT Analysis

```python
# در فایل: 01-clinical-validation/statistical-analysis/rct_analysis.py

from rct_analysis import *

# بارگذاری داده
df = load_data('data/rct_data.csv')

# آنالیز اولیه
descriptive_stats(df)

# بررسی randomization
check_randomization(df)

# آنالیز اصلی
primary_results = primary_analysis(df)

# آنالیزهای ثانویه
secondary_results = secondary_outcomes(df)

# تولید گزارش
generate_report(primary_results, secondary_results)
```

### 2. استفاده از Explainable AI API

```python
import requests

# پیش‌بینی و توضیح نمره تلفظ
response = requests.post(
    "http://localhost:8000/api/explain/pronunciation",
    json={
        "audio_features": [0.23, 0.45, ...],  # 41 features
        "phoneme": "/s/",
        "child_id": "CH123"
    }
)

result = response.json()
print(result['human_explanation'])
print(result['actionable_recommendations'])
```

```bash
# استفاده از curl
curl -X POST "http://localhost:8000/api/explain/pronunciation" \
  -H "Content-Type: application/json" \
  -d '{
    "audio_features": [0.1, 0.2, 0.3, ...],
    "phoneme": "/s/",
    "child_id": "CH001"
  }'
```

### 3. دسترسی به Therapist Dashboard

```python
# در مرورگر
# http://localhost:8000/api/dashboard/therapist/CH001

# یا با Python:
import requests

child_id = "CH001"
response = requests.get(f"http://localhost:8000/api/dashboard/therapist/{child_id}")
dashboard_data = response.json()

print(dashboard_data['summary'])
print(dashboard_data['recommendations'])
```

---

## 📝 نمونه داده

### فرمت داده برای RCT Analysis

```csv
ChildID,Group,Age,Gender,Baseline_SII,Week24_SII,GFTA_Change,Parent_Satisfaction
CH001,Treatment,8,Male,55.2,72.3,15,4.5
CH002,Control,7,Female,58.1,63.4,8,3.8
CH003,Treatment,9,Male,52.7,75.8,22,4.8
...
```

### فرمت audio features برای Explainability

```json
{
  "audio_features": [
    0.234,   // MFCC 1
    -0.123,  // MFCC 2
    0.456,   // ...
    // ... total 41 features
  ],
  "phoneme": "/s/",
  "child_id": "CH001"
}
```

---

## 🧪 تست

### تست Clinical Analysis

```bash
cd 01-clinical-validation/statistical-analysis

# اجرا با داده نمونه
python rct_analysis.py

# خروجی مورد انتظار:
# ✅ Data loaded: 200 participants
# ✅ Descriptive statistics calculated
# ✅ Randomization verified
# ✅ Primary analysis: p = 0.0023 ***
# ✅ Report saved: outputs/analysis_report.md
```

### تست Explainable AI API

```bash
cd 04-explainable-ai/backend

# اجرای تست‌ها
pytest tests/

# اجرای سرور در حالت debug
uvicorn explainable_api:app --reload --log-level debug

# تست endpoint
curl http://localhost:8000/api/explain/pronunciation \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"audio_features": [0.1], "phoneme": "/s/", "child_id": "TEST"}'
```

---

## 🐛 عیب‌یابی

### مشکل: نصب SHAP با خطا مواجه می‌شود

```bash
# راه‌حل 1: نصب از source
pip install shap --no-binary shap

# راه‌حل 2: نصب نسخه قدیمی‌تر
pip install shap==0.41.0

# راه‌حل 3: استفاده از conda
conda install -c conda-forge shap
```

### مشکل: PyTorch نصب نمی‌شود

```bash
# برای CPU:
pip install torch --index-url https://download.pytorch.org/whl/cpu

# برای GPU (CUDA 11.8):
pip install torch --index-url https://download.pytorch.org/whl/cu118
```

### مشکل: داده نمونه وجود ندارد

```python
# تولید داده نمونه
import numpy as np
import pandas as pd

# Generate synthetic RCT data
np.random.seed(42)
n = 200

df = pd.DataFrame({
    'ChildID': [f'CH{i:03d}' for i in range(n)],
    'Group': np.random.choice(['Control', 'Treatment'], n),
    'Age': np.random.randint(5, 13, n),
    'Gender': np.random.choice(['Male', 'Female'], n),
    'Baseline_SII': np.random.normal(60, 10, n),
})

# Treatment effect
treatment_mask = df['Group'] == 'Treatment'
df['Week24_SII'] = df['Baseline_SII'] + \
    np.random.normal(5, 5, n) + \
    treatment_mask * np.random.normal(10, 5, n)  # Extra improvement

df.to_csv('data/rct_data.csv', index=False)
print("✅ Sample data generated!")
```

---

## 📚 منابع اضافی

### Clinical Validation:
- [CONSORT Guidelines](http://www.consort-statement.org/)
- [ClinicalTrials.gov](https://clinicaltrials.gov/)
- [FDA Guidance for Medical Devices](https://www.fda.gov/medical-devices)

### Statistical Analysis:
- [R for Clinical Trial Reporting](https://github.com/openpharma/clinical-reporting)
- [statsmodels Documentation](https://www.statsmodels.org/)
- [Mixed Models Tutorial](https://www.apsc.ubc.ca/~gschumac/)

### Explainable AI:
- [SHAP Documentation](https://shap.readthedocs.io/)
- [Interpretable ML Book](https://christophm.github.io/interpretable-ml-book/)
- [FastAPI Tutorial](https://fastapi.tiangolo.com/)

### Business Model:
- [Business Model Generation](https://www.strategyzer.com/canvas)
- [Unit Economics Guide](https://andrewchen.com/know-your-economics/)

---

## ⚙️ پیکربندی برای Production

### 1. تنظیمات امنیتی

```python
# در .env file
SECRET_KEY=your-secret-key-here
DATABASE_URL=postgresql://user:pass@localhost/hearloveen
AZURE_STORAGE_CONNECTION=your-azure-connection-string
```

### 2. استفاده از PostgreSQL

```sql
-- ایجاد database
CREATE DATABASE hearloveen_clinical;

-- اجرای schema
\i 01-clinical-validation/database/schema.sql
```

### 3. Deploy با Docker Compose

```yaml
version: '3.8'
services:
  api:
    build: ./04-explainable-ai/backend
    ports:
      - "8000:8000"
    environment:
      - DATABASE_URL=postgresql://postgres:password@db:5432/hearloveen
    depends_on:
      - db
  
  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=hearloveen
      - POSTGRES_PASSWORD=password
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

```bash
# اجرا
docker-compose up -d
```

---

## 🤝 مشارکت

برای گزارش مشکلات یا پیشنهادات:
- 📧 Email: yasser.ebrahimifard@hearloveen.com
- 💬 GitHub Issues: [repo-url]

---

## 📄 مجوز

این پروژه تحت مجوز MIT منتشر شده است.

© 2025 HearLoveen. All rights reserved.

---

## 🎯 چک‌لیست راه‌اندازی

- [ ] خواندن EXECUTIVE_SUMMARY.md
- [ ] نصب وابستگی‌های Python
- [ ] مطالعه RCT Protocol
- [ ] بررسی Business Model Canvas
- [ ] مطالعه Competitive Analysis
- [ ] اجرای Explainable AI API (local)
- [ ] مطالعه Longitudinal Study Design
- [ ] شناسایی اولین 5 کلینیک pilot
- [ ] تماس با IRB برای approval
- [ ] شروع!

---

**موفق باشید!** 🚀

برای سوالات: yasser.ebrahimifard@hearloveen.com
